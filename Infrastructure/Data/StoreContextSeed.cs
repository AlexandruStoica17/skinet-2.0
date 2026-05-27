using System.Text.Json;
using Core.Entities;
using Core.Entities.Identity;
using Core.Entities.OrderAggregate;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data
{
    public class StoreContextSeed
    {
        public static async Task SeedAsync(
            StoreContext context,
            UserManager<AppUser> userManager)
        {
            if (!context.ProductTypes.Any())
            {
                var typesData = File.ReadAllText("../Infrastructure/Data/SeedData/types.json");
                var types = JsonSerializer.Deserialize<List<ProductType>>(typesData);

                if (types != null)
                {
                    context.ProductTypes.AddRange(types);
                }
            }

            if (!context.ProductBrands.Any())
            {
                var brandsData = File.ReadAllText("../Infrastructure/Data/SeedData/brands.json");
                var brands = JsonSerializer.Deserialize<List<ProductBrand>>(brandsData);

                if (brands != null)
                {
                    context.ProductBrands.AddRange(brands);
                }
            }

            if (!context.DeliveryMethods.Any())
            {
                var deliveryData = File.ReadAllText("../Infrastructure/Data/SeedData/delivery.json");
                var methods = JsonSerializer.Deserialize<List<DeliveryMethod>>(deliveryData);

                if (methods != null)
                {
                    context.DeliveryMethods.AddRange(methods);
                }
            }

            if (context.ChangeTracker.HasChanges())
            {
                await context.SaveChangesAsync();
            }

            var productsData = File.ReadAllText("../Infrastructure/Data/SeedData/products.json");

            var seedProducts = JsonSerializer.Deserialize<List<SeedProductDto>>(
                productsData,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }
            );

            if (seedProducts == null) return;

            foreach (var seedProduct in seedProducts)
            {
                var producer = await userManager.FindByEmailAsync(seedProduct.ProducerEmail);

                if (producer == null)
                {
                    continue;
                }

                var product = await context.Products
                    .Include(p => p.Photos)
                    .FirstOrDefaultAsync(p =>
                        p.Name == seedProduct.Name ||
                        p.PictureUrl == seedProduct.PictureUrl);

                if (product == null)
                {
                    product = new Product();
                    context.Products.Add(product);
                }

                product.Name = seedProduct.Name;
                product.Description = seedProduct.Description;
                product.Price = seedProduct.Price;
                product.PictureUrl = seedProduct.PictureUrl;
                product.ProductTypeId = seedProduct.ProductTypeId;
                product.ProductBrandId = seedProduct.ProductBrandId;
                product.ProducerId = producer.Id;
                product.ProducerName = producer.DisplayName;
                product.SkinType = seedProduct.SkinType;
                product.Usage = seedProduct.Usage;
                product.Benefits = seedProduct.Benefits;
                product.Formula = seedProduct.Formula;

                EnsureProductPhotos(product, seedProduct);
            }

            if (context.ChangeTracker.HasChanges())
            {
                await context.SaveChangesAsync();
            }

            await SeedBlogPostsAsync(context, userManager);

            if (context.ChangeTracker.HasChanges())
            {
                await context.SaveChangesAsync();
            }
        }

        private static void EnsureProductPhotos(Product product, SeedProductDto seedProduct)
        {
            var photoUrls = (seedProduct.PhotoUrls == null || seedProduct.PhotoUrls.Count == 0)
                ? new List<string> { seedProduct.PictureUrl }
                : seedProduct.PhotoUrls;

            photoUrls = photoUrls
                .Where(url => !string.IsNullOrWhiteSpace(url))
                .Distinct()
                .ToList();

            for (var i = 0; i < photoUrls.Count; i++)
            {
                var url = photoUrls[i];
                var existingPhoto = product.Photos.FirstOrDefault(p => p.Url == url);

                if (existingPhoto == null)
                {
                    product.Photos.Add(new ProductPhoto
                    {
                        Url = url,
                        DisplayOrder = i + 1,
                        IsMain = url == seedProduct.PictureUrl
                    });

                    continue;
                }

                existingPhoto.DisplayOrder = i + 1;
                existingPhoto.IsMain = url == seedProduct.PictureUrl;
            }

            var stalePhotos = product.Photos
                .Where(p => !photoUrls.Contains(p.Url))
                .ToList();

            foreach (var stalePhoto in stalePhotos)
            {
                product.Photos.Remove(stalePhoto);
            }

            foreach (var photo in product.Photos.Where(p => p.Url != seedProduct.PictureUrl))
            {
                photo.IsMain = false;
            }

            var mainPhoto = product.Photos.FirstOrDefault(p => p.Url == seedProduct.PictureUrl);

            if (mainPhoto != null)
            {
                mainPhoto.IsMain = true;
            }
        }

        private static async Task SeedBlogPostsAsync(
            StoreContext context,
            UserManager<AppUser> userManager)
        {
            var bloggers = await userManager.GetUsersInRoleAsync("Blogger");

            if (bloggers.Count == 0)
            {
                return;
            }

            var articles = new List<SeedBlogArticleDto>
            {
                new()
                {
                    BloggerEmail = "skincarediary@greenbeauty.com",
                    Title = "How to Build a Gentle Morning Skincare Routine",
                    ImageUrl = "images/products/cosmetic-1A.jpg",
                    Content = "A professional morning routine should cleanse gently, hydrate properly and protect the skin barrier without overwhelming it.",
                    Sections = new List<SeedBlogSectionDto>
                    {
                        new()
                        {
                            Heading = "Start with a mild cleanse",
                            Text = "Use a cleanser that removes overnight residue without leaving the skin tight. Cream and milk textures are useful for sensitive or dry skin.",
                            ImageUrl = "images/products/cosmetic-2A.jpg",
                            Caption = "Gentle cleansing keeps the barrier comfortable."
                        },
                        new()
                        {
                            Heading = "Layer hydration before richer textures",
                            Text = "Apply lightweight hydrating products before creams or balms. This helps the routine feel elegant and avoids a heavy finish.",
                            ImageUrl = "images/products/cosmetic-1C.jpg",
                            Caption = "Hydrating layers prepare the skin for the day."
                        }
                    }
                },
                new()
                {
                    BloggerEmail = "skincarediary@greenbeauty.com",
                    Title = "Choosing Botanical Ingredients for Sensitive Skin",
                    ImageUrl = "images/products/ingredient-1A.jpg",
                    Content = "Sensitive skin benefits from simple botanical ingredients, clear usage guidance and formulas that avoid unnecessary complexity.",
                    Sections = new List<SeedBlogSectionDto>
                    {
                        new()
                        {
                            Heading = "Look for calming ingredients",
                            Text = "Aloe vera, lavender and honey are common choices for soothing routines. The final formula still matters, so introduce new products slowly.",
                            ImageUrl = "images/products/ingredient-2A.jpg",
                            Caption = "Botanical ingredients should be matched with skin needs."
                        },
                        new()
                        {
                            Heading = "Avoid mixing too many actives",
                            Text = "A minimal routine is easier to evaluate. Add one product at a time and monitor texture, comfort and visible changes.",
                            ImageUrl = "images/products/ingredient-3A.jpg",
                            Caption = "A slower routine is often more reliable."
                        }
                    }
                },
                new()
                {
                    BloggerEmail = "greenbeautyblog@greenbeauty.com",
                    Title = "Ingredient Stories: Why Honey Is Used in Natural Cosmetics",
                    ImageUrl = "images/products/ingredient-1M.jpg",
                    Content = "Honey is valued in cosmetic formulas for its comforting texture, humectant feel and compatibility with masks, balms and gentle cleansing products.",
                    Sections = new List<SeedBlogSectionDto>
                    {
                        new()
                        {
                            Heading = "Honey as a humectant-inspired ingredient",
                            Text = "In handmade cosmetic recipes, honey is often used to support a softer feel and a more comforting product experience.",
                            ImageUrl = "images/products/ingredient-2M.jpg",
                            Caption = "Bio honey fits soothing and nourishing formulas."
                        },
                        new()
                        {
                            Heading = "Where it works best",
                            Text = "Honey appears often in masks, body products and rinse-off blends. It should be used with clear instructions and suitable preservation logic.",
                            ImageUrl = "images/products/ingredient-3M.jpg",
                            Caption = "Usage area and formulation context are essential."
                        }
                    }
                },
                new()
                {
                    BloggerEmail = "naturalglow@greenbeauty.com",
                    Title = "From Farm to Formula: Reading a Seller Page Like a Professional",
                    ImageUrl = "images/products/ingredient-1S.jpg",
                    Content = "A trustworthy natural beauty marketplace should make seller origin, story, documents and product availability easy to understand.",
                    Sections = new List<SeedBlogSectionDto>
                    {
                        new()
                        {
                            Heading = "Check the seller story and location",
                            Text = "A clear seller profile helps customers understand whether a producer is a cosmetic studio, an ingredient farm or a raw-material supplier.",
                            ImageUrl = "images/products/ingredient-2S.jpg",
                            Caption = "Transparency builds trust before purchase."
                        },
                        new()
                        {
                            Heading = "Review available products together",
                            Text = "The seller page should show the full product list so customers can compare textures, benefits and formulation purposes in one place.",
                            ImageUrl = "images/products/ingredient-3S.jpg",
                            Caption = "Available products complete the seller profile."
                        }
                    }
                }
            };

            foreach (var blogger in bloggers)
            {
                var hasSeedArticle = articles.Any(article =>
                    string.Equals(article.BloggerEmail, blogger.Email, StringComparison.OrdinalIgnoreCase));

                if (hasSeedArticle)
                {
                    continue;
                }

                articles.Add(new SeedBlogArticleDto
                {
                    BloggerEmail = blogger.Email,
                    Title = $"{blogger.DisplayName}'s Guide to Natural Beauty",
                    ImageUrl = "images/products/cosmetic-1C.jpg",
                    Content = "A practical guide for choosing natural cosmetic products with clear ingredients, realistic expectations and a consistent routine.",
                    Sections = new List<SeedBlogSectionDto>
                    {
                        new()
                        {
                            Heading = "Choose products with a clear purpose",
                            Text = "A focused routine is easier to follow. Select products by skin type, usage area and main benefit instead of adding unnecessary steps.",
                            ImageUrl = "images/products/cosmetic-2C.jpg",
                            Caption = "Clarity makes the routine easier to maintain."
                        },
                        new()
                        {
                            Heading = "Check the seller and product details",
                            Text = "Seller pages, product photos and ingredient notes help customers evaluate quality before placing an order.",
                            ImageUrl = "images/products/cosmetic-3C.jpg",
                            Caption = "Trust grows when product information is complete."
                        }
                    }
                });
            }

            foreach (var article in articles)
            {
                var blogger = bloggers.FirstOrDefault(x =>
                    string.Equals(x.Email, article.BloggerEmail, StringComparison.OrdinalIgnoreCase));

                if (blogger == null)
                {
                    continue;
                }

                var alreadyExists = await context.Posts.AnyAsync(p =>
                    p.AppUserId == blogger.Id &&
                    p.Title == article.Title);

                if (alreadyExists)
                {
                    continue;
                }

                var post = new Post
                {
                    Title = article.Title,
                    Content = article.Content,
                    ImageUrl = article.ImageUrl,
                    CreatedAt = DateTime.UtcNow.AddDays(-articles.IndexOf(article) - 1),
                    AppUserId = blogger.Id
                };

                for (var i = 0; i < article.Sections.Count; i++)
                {
                    var section = article.Sections[i];

                    post.Sections.Add(new PostSection
                    {
                        Heading = section.Heading,
                        Text = section.Text,
                        ImageUrl = section.ImageUrl,
                        Caption = section.Caption,
                        DisplayOrder = i + 1
                    });
                }

                context.Posts.Add(post);
            }
        }

        private class SeedProductDto
        {
            public string Name { get; set; }
            public string Description { get; set; }
            public decimal Price { get; set; }
            public string PictureUrl { get; set; }
            public List<string> PhotoUrls { get; set; } = new();

            public int ProductTypeId { get; set; }
            public int ProductBrandId { get; set; }

            public string ProducerEmail { get; set; }

            public string SkinType { get; set; }
            public string Usage { get; set; }
            public string Benefits { get; set; }
            public string Formula { get; set; }
        }

        private class SeedBlogArticleDto
        {
            public string BloggerEmail { get; set; }
            public string Title { get; set; }
            public string Content { get; set; }
            public string ImageUrl { get; set; }
            public List<SeedBlogSectionDto> Sections { get; set; } = new();
        }

        private class SeedBlogSectionDto
        {
            public string Heading { get; set; }
            public string Text { get; set; }
            public string ImageUrl { get; set; }
            public string Caption { get; set; }
        }
    }
}
