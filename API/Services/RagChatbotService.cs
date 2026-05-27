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
        private static readonly Regex TokenRegex = new("[a-zA-Z0-9]+", RegexOptions.Compiled);
        private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
        {
            "the", "and", "for", "with", "that", "this", "from", "you", "your", "are", "can",
            "how", "what", "when", "where", "why", "who", "a", "an", "to", "of", "in", "on",
            "is", "it", "as", "or", "be", "my", "i", "me", "sa", "si", "de", "la", "cu",
            "pe", "un", "o", "ce", "cum", "este", "sunt", "pentru"
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

            if (IsProductLinkRequest(userMessage) && products.Count > 0)
            {
                return BuildProductLinkResponse(userMessage, chunks, products);
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
                    Answer = ExtractOllamaAnswer(responseBody),
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
                    Answer = ExtractAnswer(responseBody),
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
                    $"- {product.Name} | {product.ProductType} | {product.Brand} | {product.ProducerName} | ${product.Price:0.00} | /shop/{product.Id}");
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

            return string.Join(" ", new[] { userMessage }.Concat(recentContext));
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
                $"[{product.Name}](/shop/{product.Id}) ({product.ProductType}) by {product.ProducerName}, ${product.Price:0.00}")));
            builder.Append(".");

            return builder.ToString();
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
                   normalized.Contains("page");
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
                   normalized.Contains("spre") ||
                   normalized.Contains("imi");
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
