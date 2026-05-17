using System.Text.Json;
using Core.Entities;
using Core.Entities.OrderAggregate;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data
{
    public class StoreContextSeed
    {
        public static async Task SeedAsync(StoreContext context)
        {
            // UPDATED: Replace old Skinet types with cosmetic marketplace types
            // This runs every startup and ensures correct types exist
            var desiredTypes = new List<(int Id, string Name)>
            {
                (1, "Cosmetics"),
                (2, "Ingredients")
            };

            foreach (var (id, name) in desiredTypes)
            {
                var existing = await context.ProductTypes.FindAsync(id);
                if (existing == null)
                    context.ProductTypes.Add(new ProductType { Id = id, Name = name });
                else if (existing.Name != name)
                    existing.Name = name;
            }

            // Remove old types that don't belong (Boards, Hats, Boots, Gloves)
            var oldTypes = context.ProductTypes.Where(t => t.Id > 2);
            context.ProductTypes.RemoveRange(oldTypes);

            if (!context.DeliveryMethods.Any())
            {
                var deliveryData = File.ReadAllText("../Infrastructure/Data/SeedData/delivery.json");
                var methods = JsonSerializer.Deserialize<List<DeliveryMethod>>(deliveryData);
                context.DeliveryMethods.AddRange(methods);
            }

            if (context.ChangeTracker.HasChanges()) await context.SaveChangesAsync();
        }
    }
}