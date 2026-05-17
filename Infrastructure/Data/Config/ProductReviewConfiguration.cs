using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Config
{
    public class ProductReviewConfiguration : IEntityTypeConfiguration<ProductReview>
    {
        public void Configure(EntityTypeBuilder<ProductReview> builder)
        {
            builder.Property(r => r.BuyerEmail).IsRequired();
            builder.Property(r => r.BuyerName).IsRequired();
            builder.Property(r => r.Rating).IsRequired();
            builder.Property(r => r.Comment).HasMaxLength(1000);
            // Un user poate lasa un singur review per produs per comanda
            builder.HasIndex(r => new { r.ProductId, r.OrderId, r.BuyerEmail }).IsUnique();
        }
    }
}