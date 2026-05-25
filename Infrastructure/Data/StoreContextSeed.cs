using System.Text.Json;
using Core.Entities;
using Core.Entities.Identity;
using Core.Entities.OrderAggregate;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

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

            if (!context.Products.Any())
            {
                var productsData = File.ReadAllText("../Infrastructure/Data/SeedData/products.json");

                var seedProducts = JsonSerializer.Deserialize<List<SeedProductDto>>(
                    productsData,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }
                );

                if (seedProducts != null)
                {
                    foreach (var seedProduct in seedProducts)
                    {
                        var producer = await userManager.FindByEmailAsync(seedProduct.ProducerEmail);

                        if (producer == null)
                        {
                            continue;
                        }

                        var product = new Product
                        {
                            Name = seedProduct.Name,
                            Description = seedProduct.Description,
                            Price = seedProduct.Price,
                            PictureUrl = seedProduct.PictureUrl,
                            ProductTypeId = seedProduct.ProductTypeId,
                            ProductBrandId = seedProduct.ProductBrandId,
                            ProducerId = producer.Id,
                            ProducerName = producer.DisplayName,
                            SkinType = seedProduct.SkinType,
                            Usage = seedProduct.Usage,
                            Benefits = seedProduct.Benefits,
                            Formula = seedProduct.Formula
                        };

                        context.Products.Add(product);
                    }

                    await context.SaveChangesAsync();
                }
            }
        }

        private class SeedProductDto
        {
            public string Name { get; set; }
            public string Description { get; set; }
            public decimal Price { get; set; }
            public string PictureUrl { get; set; }

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