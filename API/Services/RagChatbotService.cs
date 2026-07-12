using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using API.Dtos;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace API.Services
{
    public class RagChatbotService : IChatbotService
    {
        private static readonly Regex TokenRegex = new(@"[\p{L}\p{N}]+", RegexOptions.Compiled);
        private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
        {
            "the", "and", "for", "with", "that", "this", "from", "you", "your", "are", "can",
            "how", "what", "when", "where", "why", "who", "a", "an", "to", "of", "in", "on",
            "is", "it", "as", "or", "be", "my", "i", "me", "sa", "si", "de", "la", "cu",
            "pe", "un", "o", "ce", "cum", "este", "sunt", "pentru", "vindeti", "vindeți",
            "aveti", "aveți", "vand", "vând", "sell", "sold", "stock", "have", "product",
            "products", "produs", "produse"
        };

        private static readonly Dictionary<string, string[]> ProductSynonyms = new(StringComparer.OrdinalIgnoreCase)
        {
            ["miere"] = new[] { "honey", "bio honey" },
            ["honey"] = new[] { "miere", "bio honey" },
            ["lamaie"] = new[] { "lemon", "citrus" },
            ["lămâie"] = new[] { "lemon", "citrus" },
            ["lemon"] = new[] { "lamaie", "citrus" },
            ["lavanda"] = new[] { "lavender" },
            ["lavandă"] = new[] { "lavender" },
            ["aloe"] = new[] { "aloe vera" },
            ["menta"] = new[] { "mint" },
            ["mentă"] = new[] { "mint" },
            ["trandafir"] = new[] { "rose" },
            ["trandafiri"] = new[] { "rose" },
            ["cocos"] = new[] { "coconut" },
            ["nuca"] = new[] { "coconut" },
            ["nucă"] = new[] { "coconut" },
            ["shea"] = new[] { "shea butter" },
            ["unt"] = new[] { "butter" },
            ["hibiscus"] = new[] { "hibiscus flower" }
        };

        private readonly HttpClient _httpClient;
        private readonly ChatbotOptions _options;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<RagChatbotService> _logger;
        private readonly StoreContext _context;
        private IReadOnlyList<KnowledgeChunk> _knowledgeCache;

        public RagChatbotService(
            HttpClient httpClient,
            IOptions<ChatbotOptions> options,
            IWebHostEnvironment environment,
            ILogger<RagChatbotService> logger,
            StoreContext context)
        {
            _httpClient = httpClient;
            _options = options.Value;
            _environment = environment;
            _logger = logger;
            _context = context;
        }

        public async Task<ChatbotResponseDto> AskAsync(
            ChatbotRequestDto request,
            string userEmail,
            CancellationToken cancellationToken)
        {
            var userMessage = request.Message?.Trim();
            var searchQuery = BuildSearchQuery(userMessage, request.History);
            var chunks = GetRelevantChunks(searchQuery)
                .Take(Math.Max(1, _options.MaxKnowledgeChunks))
                .ToList();
            var products = await GetRelevantProductsAsync(searchQuery, cancellationToken);

            if (IsOrderTrackingQuestion(userMessage))
            {
                return BuildOrderTrackingResponse(userMessage, chunks);
            }

            if (IsSellerContactQuestion(userMessage))
            {
                return BuildSellerContactResponse(userMessage, chunks);
            }

            if (IsRecommendationQuestion(userMessage))
            {
                return BuildRecommendationResponse(userMessage, chunks);
            }

            if (IsFavoritesQuestion(userMessage))
            {
                return BuildFavoritesResponse(userMessage, chunks);
            }

            if (IsProductLinkRequest(userMessage) && products.Count > 0)
            {
                return BuildProductLinkResponse(userMessage, chunks, products);
            }

            if ((IsCatalogQuestion(userMessage) || IsLanguageRequest(userMessage)) && products.Count > 0)
            {
                return BuildProductAvailabilityResponse(userMessage, chunks, products);
            }

            if (UseOllamaProvider())
            {
                return await AskOllamaAsync(
                    userMessage,
                    request.History,
                    chunks,
                    products,
                    userEmail,
                    cancellationToken);
            }

            if (string.IsNullOrWhiteSpace(_options.ApiKey))
            {
                return BuildLocalFallback(chunks, products);
            }

            return await AskOpenAiAsync(
                userMessage,
                request.History,
                chunks,
                products,
                userEmail,
                cancellationToken);
        }

        private async Task<ChatbotResponseDto> AskOllamaAsync(
            string userMessage,
            IReadOnlyList<ChatbotHistoryMessageDto> history,
            IReadOnlyList<KnowledgeChunk> chunks,
            IReadOnlyList<ProductSearchResult> products,
            string userEmail,
            CancellationToken cancellationToken)
        {
            var modelOptions = new Dictionary<string, object>
            {
                ["temperature"] = _options.Temperature
            };

            if (_options.MaxOutputTokens > 0)
            {
                modelOptions["num_predict"] = _options.MaxOutputTokens;
            }

            var payload = new Dictionary<string, object>
            {
                ["model"] = string.IsNullOrWhiteSpace(_options.Model) ? "gemma3:4b" : _options.Model,
                ["stream"] = false,
                ["messages"] = new[]
                {
                    new
                    {
                        role = "system",
                        content = BuildInstructions()
                    },
                    new
                    {
                        role = "user",
                        content = BuildInput(userMessage, history, chunks, products, userEmail)
                    }
                },
                ["options"] = modelOptions
            };

            try
            {
                using var response = await _httpClient.PostAsJsonAsync(
                    _options.OllamaEndpoint,
                    payload,
                    cancellationToken);
                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "Ollama chatbot request failed with status {StatusCode}: {Body}",
                        response.StatusCode,
                        responseBody);

                    return BuildLocalFallback(chunks, products);
                }

                return new ChatbotResponseDto
                {
                    Answer = NormalizeNavigationAnswer(ExtractOllamaAnswer(responseBody), userMessage),
                    Sources = BuildSources(chunks, products),
                    Mode = "ollama-rag",
                    IsAiConfigured = true
                };
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ollama chatbot request failed. Falling back to local retrieval.");
                return BuildLocalFallback(chunks, products);
            }
        }

        private async Task<ChatbotResponseDto> AskOpenAiAsync(
            string userMessage,
            IReadOnlyList<ChatbotHistoryMessageDto> history,
            IReadOnlyList<KnowledgeChunk> chunks,
            IReadOnlyList<ProductSearchResult> products,
            string userEmail,
            CancellationToken cancellationToken)
        {
            var payload = new Dictionary<string, object>
            {
                ["model"] = string.IsNullOrWhiteSpace(_options.Model) ? "gpt-5-mini" : _options.Model,
                ["instructions"] = BuildInstructions(),
                ["input"] = BuildInput(userMessage, history, chunks, products, userEmail)
            };

            if (_options.MaxOutputTokens > 0)
            {
                payload["max_output_tokens"] = _options.MaxOutputTokens;
            }

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, _options.Endpoint);
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
            httpRequest.Content = JsonContent.Create(payload);

            try
            {
                using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "OpenAI chatbot request failed with status {StatusCode}: {Body}",
                        response.StatusCode,
                        responseBody);

                    return BuildLocalFallback(chunks, products);
                }

                return new ChatbotResponseDto
                {
                    Answer = NormalizeNavigationAnswer(ExtractAnswer(responseBody), userMessage),
                    Sources = BuildSources(chunks, products),
                    Mode = "openai-rag",
                    IsAiConfigured = true
                };
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OpenAI chatbot request failed.");
                return BuildLocalFallback(chunks, products);
            }
        }

        private bool UseOllamaProvider()
        {
            return !string.Equals(_options.Provider, "OpenAI", StringComparison.OrdinalIgnoreCase);
        }

        private static string BuildInstructions()
        {
            return """
                You are GreenBeauty Assistant, a professional support chatbot for a natural beauty marketplace.
                Answer in the same language as the user.
                Use only the provided knowledge base excerpts and platform context.
                If relevant product catalog matches are available, answer with those concrete products instead of generic marketplace text.
                When a product route is available, format it as a Markdown link like [Product Name](/shop/16).
                Use only real GreenBeauty routes. Favorites is /favorites. Orders is /orders. Cart is /basket. Checkout is /checkout. Messages is /chat. Never invent routes
                like /my-orders, /account/orders, /account/orders, /order-history, /help or external help-center URLs.
                If the user asks how to track orders, answer with [My Orders](/orders).
                If the user asks how to contact a seller, say they can use Contact seller on product pages or open [Messages](/chat).
                If the user asks where favorites or saved products are, answer with [Favorites](/favorites).
                If the user asks how recommendations work, explain that suggestions compare product name, category, benefits, usage area, formula and skin type.
                If the user asks for a direct product link, answer with the direct Markdown link and no extra explanation.
                If the knowledge base does not contain enough information, say that clearly and suggest checking the relevant page or contacting support.
                Do not provide medical diagnosis, treatment plans, or allergy guarantees. For health-sensitive questions, recommend a patch test and a qualified dermatologist.
                Do not request or reveal passwords, payment data, private order data, or personal information.
                Keep answers concise, polite, and actionable.
                """;
        }

        private string BuildInput(
            string userMessage,
            IReadOnlyList<ChatbotHistoryMessageDto> history,
            IReadOnlyList<KnowledgeChunk> chunks,
            IReadOnlyList<ProductSearchResult> products,
            string userEmail)
        {
            var builder = new StringBuilder();

            builder.AppendLine("Knowledge base excerpts:");
            if (chunks.Count == 0)
            {
                builder.AppendLine("No directly relevant knowledge base excerpt was found.");
            }

            foreach (var chunk in chunks)
            {
                builder.AppendLine($"Source: {chunk.Source}");
                builder.AppendLine(chunk.Content);
                builder.AppendLine();
            }

            builder.AppendLine("Relevant product catalog matches:");
            if (products.Count == 0)
            {
                builder.AppendLine("No relevant product match was found in the current catalog.");
            }

            foreach (var product in products)
            {
                builder.AppendLine(
                    $"- {product.Name} | {product.ProductType} | {product.Brand} | {product.ProducerName} | {FormatPrice(product.Price)} | /shop/{product.Id}");
                builder.AppendLine(
                    $"  Markdown link: [{product.Name}](/shop/{product.Id})");
                builder.AppendLine(
                    $"  Details: {product.Description} Skin: {product.SkinType}. Usage: {product.Usage}. Benefits: {product.Benefits}. Formula: {product.Formula}.");
            }

            builder.AppendLine();
            builder.AppendLine("Conversation history:");
            foreach (var message in history
                .Where(m => !string.IsNullOrWhiteSpace(m.Content))
                .TakeLast(Math.Max(0, _options.MaxHistoryMessages)))
            {
                builder.AppendLine($"{NormalizeRole(message.Role)}: {TrimForPrompt(message.Content, 900)}");
            }

            builder.AppendLine();
            builder.AppendLine($"Authenticated user: {(string.IsNullOrWhiteSpace(userEmail) ? "anonymous visitor" : userEmail)}");
            builder.AppendLine("Current user question:");
            builder.AppendLine(userMessage);

            return builder.ToString();
        }

        private static string BuildSearchQuery(
            string userMessage,
            IReadOnlyList<ChatbotHistoryMessageDto> history)
        {
            var recentContext = history
                .Where(message => !string.IsNullOrWhiteSpace(message.Content))
                .TakeLast(4)
                .Select(message => message.Content);

            return ExpandProductSynonyms(string.Join(" ", new[] { userMessage }.Concat(recentContext)));
        }

        private ChatbotResponseDto BuildLocalFallback(
            IReadOnlyList<KnowledgeChunk> chunks,
            IReadOnlyList<ProductSearchResult> products)
        {
            if (products.Count > 0)
            {
                return new ChatbotResponseDto
                {
                    Answer = BuildProductFallbackAnswer(products),
                    Sources = BuildSources(chunks, products),
                    Mode = "catalog-retrieval",
                    IsAiConfigured = false
                };
            }

            if (chunks.Count == 0)
            {
                return new ChatbotResponseDto
                {
                    Answer = "I can help with GreenBeauty orders, products, sellers, ingredients and account questions. I could not find a reliable answer in the local knowledge base for this question, so please check the relevant page or contact support.",
                    Sources = Array.Empty<string>(),
                    Mode = "local-retrieval",
                    IsAiConfigured = false
                };
            }

            var first = chunks[0];
            return new ChatbotResponseDto
            {
                Answer = $"Based on the GreenBeauty knowledge base: {BuildPreview(first.Content)}",
                Sources = BuildSources(chunks, products),
                Mode = "local-retrieval",
                IsAiConfigured = false
            };
        }

        private async Task<IReadOnlyList<ProductSearchResult>> GetRelevantProductsAsync(
            string query,
            CancellationToken cancellationToken)
        {
            var queryTokens = Tokenize(query)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            if (queryTokens.Count == 0)
            {
                return Array.Empty<ProductSearchResult>();
            }

            var products = await _context.Products
                .AsNoTracking()
                .Include(p => p.ProductType)
                .Include(p => p.ProductBrand)
                .Select(p => new ProductSearchResult(
                    p.Id,
                    p.Name,
                    p.ProductType.Name,
                    p.ProductBrand.Name,
                    p.ProducerName,
                    p.Price,
                    p.Description,
                    p.SkinType,
                    p.Usage,
                    p.Benefits,
                    p.Formula))
                .ToListAsync(cancellationToken);

            return products
                .Select(product => new
                {
                    Product = product,
                    Score = ScoreProduct(product, queryTokens)
                })
                .Where(item => item.Score > 0)
                .OrderByDescending(item => item.Score)
                .ThenBy(item => item.Product.Name)
                .Take(5)
                .Select(item => item.Product)
                .ToList();
        }

        private static int ScoreProduct(ProductSearchResult product, HashSet<string> queryTokens)
        {
            var score = 0;
            score += Tokenize(product.Name).Count(queryTokens.Contains) * 8;
            score += Tokenize(product.ProductType).Count(queryTokens.Contains) * 4;
            score += Tokenize(product.Brand).Count(queryTokens.Contains) * 3;
            score += Tokenize(product.ProducerName).Count(queryTokens.Contains) * 3;
            score += Tokenize(product.Description).Count(queryTokens.Contains) * 2;
            score += Tokenize(product.Benefits).Count(queryTokens.Contains) * 2;
            score += Tokenize(product.Usage).Count(queryTokens.Contains);
            score += Tokenize(product.SkinType).Count(queryTokens.Contains);
            score += Tokenize(product.Formula).Count(queryTokens.Contains);

            return score;
        }

        private static string BuildProductFallbackAnswer(IReadOnlyList<ProductSearchResult> products)
        {
            var builder = new StringBuilder();
            builder.Append("Yes. I found ");
            builder.Append(products.Count == 1 ? "this relevant product" : "these relevant products");
            builder.Append(" in the current GreenBeauty catalog: ");
            builder.Append(string.Join("; ", products.Select(product =>
                $"[{product.Name}](/shop/{product.Id}) ({product.ProductType}) by {product.ProducerName}, {FormatPrice(product.Price)}")));
            builder.Append(".");

            return builder.ToString();
        }

        private static ChatbotResponseDto BuildProductAvailabilityResponse(
            string query,
            IReadOnlyList<KnowledgeChunk> chunks,
            IReadOnlyList<ProductSearchResult> products)
        {
            var isRomanian = LooksRomanian(query) || IsLanguageRequest(query);
            var answer = isRomanian
                ? BuildRomanianProductAnswer(products)
                : BuildEnglishProductAnswer(products);

            return new ChatbotResponseDto
            {
                Answer = answer,
                Sources = BuildSources(chunks, products),
                Mode = "catalog-answer",
                IsAiConfigured = true
            };
        }

        private static string BuildEnglishProductAnswer(IReadOnlyList<ProductSearchResult> products)
        {
            var prefix = products.Count == 1
                ? "Yes, we sell this relevant product: "
                : "Yes, we sell these relevant products: ";

            return prefix + string.Join("; ", products.Select(product =>
                $"[{product.Name}](/shop/{product.Id}) by {product.ProducerName}, {FormatPrice(product.Price)}")) + ".";
        }

        private static string BuildRomanianProductAnswer(IReadOnlyList<ProductSearchResult> products)
        {
            var prefix = products.Count == 1
                ? "Da, vindem acest produs: "
                : "Da, vindem aceste produse: ";

            return prefix + string.Join("; ", products.Select(product =>
                $"[{product.Name}](/shop/{product.Id}) de la {product.ProducerName}, {FormatPrice(product.Price)}")) + ".";
        }

        private static string FormatPrice(decimal price)
        {
            return $"{price:0.00} RON";
        }

        private static ChatbotResponseDto BuildProductLinkResponse(
            string query,
            IReadOnlyList<KnowledgeChunk> chunks,
            IReadOnlyList<ProductSearchResult> products)
        {
            var isRomanian = LooksRomanian(query);
            var answer = products.Count == 1
                ? isRomanian
                    ? $"Iata linkul direct: [{products[0].Name}](/shop/{products[0].Id})."
                    : $"Here is the direct product link: [{products[0].Name}](/shop/{products[0].Id})."
                : isRomanian
                    ? "Iata linkurile directe: " + string.Join(", ", products.Select(product => $"[{product.Name}](/shop/{product.Id})")) + "."
                    : "Here are the direct product links: " + string.Join(", ", products.Select(product => $"[{product.Name}](/shop/{product.Id})")) + ".";

            return new ChatbotResponseDto
            {
                Answer = answer,
                Sources = BuildSources(chunks, products),
                Mode = "catalog-link",
                IsAiConfigured = true
            };
        }

        private static ChatbotResponseDto BuildFavoritesResponse(
            string query,
            IReadOnlyList<KnowledgeChunk> chunks)
        {
            return new ChatbotResponseDto
            {
                Answer = BuildFavoritesAnswerText(query),
                Sources = BuildSources(chunks, Array.Empty<ProductSearchResult>()),
                Mode = "navigation-answer",
                IsAiConfigured = true
            };
        }

        private static ChatbotResponseDto BuildOrderTrackingResponse(
            string query,
            IReadOnlyList<KnowledgeChunk> chunks)
        {
            return new ChatbotResponseDto
            {
                Answer = BuildOrderTrackingAnswerText(query),
                Sources = BuildSources(chunks, Array.Empty<ProductSearchResult>()),
                Mode = "navigation-answer",
                IsAiConfigured = true
            };
        }

        private static ChatbotResponseDto BuildSellerContactResponse(
            string query,
            IReadOnlyList<KnowledgeChunk> chunks)
        {
            return new ChatbotResponseDto
            {
                Answer = BuildSellerContactAnswerText(query),
                Sources = BuildSources(chunks, Array.Empty<ProductSearchResult>()),
                Mode = "navigation-answer",
                IsAiConfigured = true
            };
        }

        private static ChatbotResponseDto BuildRecommendationResponse(
            string query,
            IReadOnlyList<KnowledgeChunk> chunks)
        {
            return new ChatbotResponseDto
            {
                Answer = BuildRecommendationAnswerText(query),
                Sources = BuildSources(chunks, Array.Empty<ProductSearchResult>()),
                Mode = "platform-answer",
                IsAiConfigured = true
            };
        }

        private static bool IsFavoritesQuestion(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return false;
            }

            var normalized = query.ToLowerInvariant();
            return normalized.Contains("favorite") ||
                   normalized.Contains("favourite") ||
                   normalized.Contains("wishlist") ||
                   normalized.Contains("saved product") ||
                   normalized.Contains("saved products") ||
                   normalized.Contains("preferate") ||
                   normalized.Contains("preferat") ||
                   normalized.Contains("salvate") ||
                   normalized.Contains("salvat");
        }

        private static bool IsOrderTrackingQuestion(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return false;
            }

            var normalized = query.ToLowerInvariant();
            return (normalized.Contains("track") && normalized.Contains("order")) ||
                   normalized.Contains("order status") ||
                   normalized.Contains("delivery information") ||
                   normalized.Contains("my orders") ||
                   normalized.Contains("comanda mea") ||
                   normalized.Contains("comenzile mele") ||
                   normalized.Contains("urmaresc comanda") ||
                   normalized.Contains("urmăresc comanda") ||
                   normalized.Contains("status comanda") ||
                   normalized.Contains("status comandă");
        }

        private static bool IsSellerContactQuestion(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return false;
            }

            var normalized = query.ToLowerInvariant();
            return (normalized.Contains("contact") && normalized.Contains("seller")) ||
                   normalized.Contains("message seller") ||
                   normalized.Contains("write to seller") ||
                   normalized.Contains("contactez vanzator") ||
                   normalized.Contains("contactez vânzător") ||
                   normalized.Contains("mesaj vanzator") ||
                   normalized.Contains("mesaj vânzător");
        }

        private static bool IsRecommendationQuestion(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return false;
            }

            var normalized = query.ToLowerInvariant();
            return normalized.Contains("recommendation") ||
                   normalized.Contains("recommendations") ||
                   normalized.Contains("suggestion") ||
                   normalized.Contains("suggestions") ||
                   normalized.Contains("you might also like") ||
                   normalized.Contains("recomandari") ||
                   normalized.Contains("recomandări") ||
                   normalized.Contains("sugestii");
        }

        private static string BuildFavoritesAnswerText(string query)
        {
            return LooksRomanian(query)
                ? "Poti vedea produsele favorite in pagina [Favorites](/favorites), disponibila si in meniul din header."
                : "You can view your favorite products on the [Favorites](/favorites) page, which is also available in the header navigation.";
        }

        private static string BuildOrderTrackingAnswerText(string query)
        {
            return LooksRomanian(query)
                ? "Poti urmari comenzile si statusul livrarii in pagina [My Orders](/orders)."
                : "You can track your orders and delivery status from the [My Orders](/orders) page.";
        }

        private static string BuildSellerContactAnswerText(string query)
        {
            return LooksRomanian(query)
                ? "Poti contacta vanzatorul din pagina produsului, folosind butonul Contact seller. Conversatiile tale sunt disponibile si in [Messages](/chat). Pentru probleme legate de o comanda, intra in [My Orders](/orders)."
                : "You can contact a seller from a product page using the Contact seller button. Your conversations are also available in [Messages](/chat). For an order-specific issue, open [My Orders](/orders).";
        }

        private static string BuildRecommendationAnswerText(string query)
        {
            return LooksRomanian(query)
                ? "Recomandarile de produse compara produsul curent cu restul catalogului dupa nume, categorie, beneficii, zona de utilizare, formula si tip de piele. Produsele cu cel mai bun scor de relevanta apar in sectiunea You might also like."
                : "Product recommendations compare the current product with the catalog by product name, category, benefits, usage area, formula and skin type. The highest-scoring matches are shown in the You might also like section.";
        }

        private static string NormalizeNavigationAnswer(string answer, string userMessage)
        {
            if (IsOrderTrackingQuestion(userMessage))
            {
                return BuildOrderTrackingAnswerText(userMessage);
            }

            if (IsSellerContactQuestion(userMessage))
            {
                return BuildSellerContactAnswerText(userMessage);
            }

            if (IsRecommendationQuestion(userMessage))
            {
                return BuildRecommendationAnswerText(userMessage);
            }

            if (IsFavoritesQuestion(userMessage))
            {
                return BuildFavoritesAnswerText(userMessage);
            }

            if (string.IsNullOrWhiteSpace(answer))
            {
                return answer;
            }

            return answer
                .Replace("[Order History](/account/orders)", "[My Orders](/orders)", StringComparison.OrdinalIgnoreCase)
                .Replace("[Order History](/my-orders)", "[My Orders](/orders)", StringComparison.OrdinalIgnoreCase)
                .Replace("(/account/orders)", "(/orders)", StringComparison.OrdinalIgnoreCase)
                .Replace("(/my-orders)", "(/orders)", StringComparison.OrdinalIgnoreCase)
                .Replace("(/order-history)", "(/orders)", StringComparison.OrdinalIgnoreCase)
                .Replace("/account/orders", "/orders", StringComparison.OrdinalIgnoreCase)
                .Replace("/my-orders", "/orders", StringComparison.OrdinalIgnoreCase)
                .Replace("/order-history", "/orders", StringComparison.OrdinalIgnoreCase)
                .Replace("[Help Center](https://www.greenbeauty.com/help)", "support", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsProductLinkRequest(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return false;
            }

            var normalized = query.ToLowerInvariant();
            return normalized.Contains("link") ||
                   normalized.Contains("url") ||
                   normalized.Contains("direct") ||
                   normalized.Contains("pagina") ||
                   normalized.Contains("pagină") ||
                   normalized.Contains("page");
        }

        private static bool IsCatalogQuestion(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return false;
            }

            var normalized = query.ToLowerInvariant();
            return normalized.Contains("sell") ||
                   normalized.Contains("carry") ||
                   normalized.Contains("stock") ||
                   normalized.Contains("have") ||
                   normalized.Contains("vindeti") ||
                   normalized.Contains("vindeți") ||
                   normalized.Contains("vindem") ||
                   normalized.Contains("aveti") ||
                   normalized.Contains("aveți") ||
                   normalized.Contains("exista") ||
                   normalized.Contains("există") ||
                   normalized.Contains("gasesti") ||
                   normalized.Contains("găsești");
        }

        private static bool IsLanguageRequest(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return false;
            }

            var normalized = query.ToLowerInvariant();
            return normalized.Contains("romana") ||
                   normalized.Contains("română") ||
                   normalized.Contains("limba romana") ||
                   normalized.Contains("romanian");
        }

        private static bool LooksRomanian(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return false;
            }

            var normalized = query.ToLowerInvariant();
            return normalized.Contains(" da ") ||
                   normalized.StartsWith("da ") ||
                   normalized.Contains("produs") ||
                   normalized.Contains("pagina") ||
                   normalized.Contains("pagină") ||
                   normalized.Contains("spre") ||
                   normalized.Contains("imi") ||
                   normalized.Contains("vindeti") ||
                   normalized.Contains("vindeți") ||
                   normalized.Contains("aveti") ||
                   normalized.Contains("aveți") ||
                   normalized.Contains("miere");
        }

        private static string ExpandProductSynonyms(string query)
        {
            var terms = new List<string> { query ?? string.Empty };

            foreach (var token in Tokenize(query))
            {
                if (ProductSynonyms.TryGetValue(token, out var synonyms))
                {
                    terms.AddRange(synonyms);
                }
            }

            return string.Join(" ", terms);
        }

        private static IReadOnlyList<string> BuildSources(
            IReadOnlyList<KnowledgeChunk> chunks,
            IReadOnlyList<ProductSearchResult> products)
        {
            return chunks
                .Select(c => c.Source)
                .Concat(products.Select(p => $"catalog: {p.Name}"))
                .Distinct()
                .ToList();
        }

        private IReadOnlyList<KnowledgeChunk> GetRelevantChunks(string query)
        {
            var queryTokens = Tokenize(query).Distinct(StringComparer.OrdinalIgnoreCase).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var chunks = LoadKnowledgeChunks();

            if (queryTokens.Count == 0)
            {
                return chunks.Take(Math.Max(1, _options.MaxKnowledgeChunks)).ToList();
            }

            return chunks
                .Select(chunk => new
                {
                    Chunk = chunk,
                    Score = ScoreChunk(chunk, queryTokens)
                })
                .Where(item => item.Score > 0)
                .OrderByDescending(item => item.Score)
                .ThenBy(item => item.Chunk.Source)
                .Select(item => item.Chunk)
                .DefaultIfEmpty(chunks.FirstOrDefault())
                .Where(chunk => chunk != null)
                .ToList();
        }

        private IReadOnlyList<KnowledgeChunk> LoadKnowledgeChunks()
        {
            if (_knowledgeCache != null)
            {
                return _knowledgeCache;
            }

            var knowledgeRoot = Path.Combine(_environment.ContentRootPath, _options.KnowledgePath);
            if (!Directory.Exists(knowledgeRoot))
            {
                _logger.LogWarning("Chatbot knowledge directory was not found: {KnowledgeRoot}", knowledgeRoot);
                _knowledgeCache = Array.Empty<KnowledgeChunk>();
                return _knowledgeCache;
            }

            _knowledgeCache = Directory
                .EnumerateFiles(knowledgeRoot, "*.md", SearchOption.TopDirectoryOnly)
                .SelectMany(BuildChunksFromFile)
                .ToList();

            return _knowledgeCache;
        }

        private static IEnumerable<KnowledgeChunk> BuildChunksFromFile(string filePath)
        {
            var fileName = Path.GetFileNameWithoutExtension(filePath);
            var lines = File.ReadAllLines(filePath);
            var currentTitle = fileName;
            var currentContent = new StringBuilder();

            foreach (var line in lines)
            {
                if (line.StartsWith("## ", StringComparison.Ordinal))
                {
                    foreach (var chunk in FlushChunk(fileName, currentTitle, currentContent))
                    {
                        yield return chunk;
                    }

                    currentTitle = line.TrimStart('#').Trim();
                    currentContent.Clear();
                    continue;
                }

                currentContent.AppendLine(line);
            }

            foreach (var chunk in FlushChunk(fileName, currentTitle, currentContent))
            {
                yield return chunk;
            }
        }

        private static IEnumerable<KnowledgeChunk> FlushChunk(string fileName, string title, StringBuilder content)
        {
            var text = content.ToString().Trim();
            if (string.IsNullOrWhiteSpace(text))
            {
                yield break;
            }

            yield return new KnowledgeChunk($"{fileName}: {title}", title, text);
        }

        private static int ScoreChunk(KnowledgeChunk chunk, HashSet<string> queryTokens)
        {
            var titleTokens = Tokenize(chunk.Title).ToList();
            var contentTokens = Tokenize(chunk.Content).ToList();

            var titleScore = titleTokens.Count(token => queryTokens.Contains(token)) * 4;
            var contentScore = contentTokens.Count(token => queryTokens.Contains(token));

            return titleScore + contentScore;
        }

        private static IEnumerable<string> Tokenize(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                yield break;
            }

            foreach (Match match in TokenRegex.Matches(value.ToLowerInvariant()))
            {
                var token = match.Value;
                if (token.Length > 2 && !StopWords.Contains(token))
                {
                    yield return token;
                }
            }
        }

        private static string ExtractAnswer(string responseBody)
        {
            using var json = JsonDocument.Parse(responseBody);
            var root = json.RootElement;

            if (root.TryGetProperty("output_text", out var outputText) &&
                outputText.ValueKind == JsonValueKind.String)
            {
                return outputText.GetString();
            }

            if (root.TryGetProperty("output", out var output) &&
                output.ValueKind == JsonValueKind.Array)
            {
                foreach (var outputItem in output.EnumerateArray())
                {
                    if (!outputItem.TryGetProperty("content", out var content) ||
                        content.ValueKind != JsonValueKind.Array)
                    {
                        continue;
                    }

                    foreach (var contentItem in content.EnumerateArray())
                    {
                        if (contentItem.TryGetProperty("text", out var text) &&
                            text.ValueKind == JsonValueKind.String)
                        {
                            return text.GetString();
                        }
                    }
                }
            }

            return "I could not generate a reliable answer right now. Please try again or contact support.";
        }

        private static string ExtractOllamaAnswer(string responseBody)
        {
            using var json = JsonDocument.Parse(responseBody);
            var root = json.RootElement;

            if (root.TryGetProperty("message", out var message) &&
                message.TryGetProperty("content", out var content) &&
                content.ValueKind == JsonValueKind.String)
            {
                return content.GetString();
            }

            if (root.TryGetProperty("response", out var response) &&
                response.ValueKind == JsonValueKind.String)
            {
                return response.GetString();
            }

            return "I could not generate a reliable answer right now. Please try again or contact support.";
        }

        private static string NormalizeRole(string role)
        {
            return string.Equals(role, "assistant", StringComparison.OrdinalIgnoreCase) ? "Assistant" : "User";
        }

        private static string TrimForPrompt(string value, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length <= maxLength)
            {
                return value;
            }

            return value.Substring(0, maxLength) + "...";
        }

        private static string BuildPreview(string content)
        {
            var preview = Regex.Replace(content, @"\s+", " ").Trim();
            return preview.Length <= 420 ? preview : preview.Substring(0, 420) + "...";
        }

        private sealed record KnowledgeChunk(string Source, string Title, string Content);

        private sealed record ProductSearchResult(
            int Id,
            string Name,
            string ProductType,
            string Brand,
            string ProducerName,
            decimal Price,
            string Description,
            string SkinType,
            string Usage,
            string Benefits,
            string Formula);
    }
}
