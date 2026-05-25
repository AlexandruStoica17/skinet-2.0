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
    }
}
