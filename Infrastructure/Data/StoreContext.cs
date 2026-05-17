using System.Reflection;
using Core.Entities;
using Core.Entities.Identity; // Adăugat pentru AppUser
using Core.Entities.OrderAggregate;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore; // Adăugat pentru IdentityDbContext
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data
{
    // AICI E SCHIMBAREA MAJORĂ: Moștenim din IdentityDbContext<AppUser> în loc de DbContext
    public class StoreContext : IdentityDbContext<AppUser>
    {
        public StoreContext(DbContextOptions<StoreContext> options) : base(options)
        {
        }

        // --- Tabelele E-commerce (Originale) ---
        public DbSet<Product> Products { get; set; }
        public DbSet<ProductBrand> ProductBrands { get; set; }
        public DbSet<ProductType> ProductTypes { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<DeliveryMethod> DeliveryMethods { get; set; }

        // --- Tabelele Noi (Blog, Favorite, Poze) ---
        public DbSet<Post> Posts { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<FavoriteProduct> FavoriteProducts { get; set; }
        public DbSet<Photo> Photos { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<BlogPost> BlogPosts { get; set; }
        // NOU: reviews
        public DbSet<Review> Reviews { get; set; }
        // NOU: reviews pentru produse individuale
public DbSet<ProductReview> ProductReviews { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // CRITIC pentru Identity: Trebuie lăsat aici sus!
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

            // Partea ta originală pentru SQLite rămâne intactă
            if (Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite")
            {
                foreach (var entityType in modelBuilder.Model.GetEntityTypes())
                {

                    var decimalProperties = entityType.ClrType.GetProperties()
                        .Where(p => p.PropertyType == typeof(decimal));

                    foreach (var property in decimalProperties)
                    {
                        modelBuilder.Entity(entityType.Name).Property(property.Name).HasConversion<double>();
                    }


                    var dateTimeProperties = entityType.ClrType.GetProperties()
                        .Where(p => p.PropertyType == typeof(DateTimeOffset));

                    foreach (var property in dateTimeProperties)
                    {
                        modelBuilder.Entity(entityType.Name).Property(property.Name)
                            .HasConversion(new Microsoft.EntityFrameworkCore.Storage.ValueConversion.DateTimeOffsetToBinaryConverter());
                    }
                }
            }
            // --- CONFIGURARE NOUĂ PENTRU MESAJE ---
            modelBuilder.Entity<Message>()
                .HasOne(u => u.Recipient)
                .WithMany(m => m.MessagesReceived) // Va fi roșu momentan, rezolvăm la Pasul 3
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Message>()
                .HasOne(u => u.Sender)
                .WithMany(m => m.MessagesSent) // Va fi roșu momentan
                .OnDelete(DeleteBehavior.Restrict);


        }
    }
}