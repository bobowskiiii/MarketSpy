using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MarketSpy.Models.ModelsDTOs;

public class AssetPriceConfig : IEntityTypeConfiguration<AssetPrice>
{
    public void Configure(EntityTypeBuilder<AssetPrice> builder)
    {
        builder.HasKey(ap => ap.Id);

        builder.Property(ap => ap.UsdPrice)
            .HasColumnType("decimal(18, 4)");
        
        builder.Property(ap => ap.UsdMarketCap)
            .HasColumnType("decimal(18, 4)");
        
        builder.Property(ap => ap.UsdVolume24h)
            .HasColumnType("decimal(18, 4)");
        
        builder.Property(ap => ap.UsdChange24h)
            .HasColumnType("decimal(18, 4)");

        builder.Property(ap => ap.LastUpdated)
            .IsRequired();

        builder.HasOne(ap => ap.Asset)
            .WithMany(a => a.AssetPrices)
            .HasForeignKey(ap => ap.AssetId);
    }
}