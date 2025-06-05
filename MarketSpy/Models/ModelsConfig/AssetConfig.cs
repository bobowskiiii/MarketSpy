using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MarketSpy.Models.ModelsDTOs;

public class AssetConfig : IEntityTypeConfiguration<Asset>
{
    public void Configure(EntityTypeBuilder<Asset> builder)
    {
        builder.HasKey(a => a.Id);
        
        builder
            .HasIndex(a => a.Symbol)
            .IsUnique();

        builder
            .Property(a => a.Symbol)
            .IsRequired()
            .HasMaxLength(50);
        
        builder
            .Property(a => a.Name)
            .HasMaxLength(50);

        builder
            .HasMany(a => a.AssetPrices)
            .WithOne(ap => ap.Asset)
            .HasForeignKey(p => p.AssetId)
            .OnDelete(DeleteBehavior.Cascade); //Kaskadowe usuwanie AssetPrice przy usuwaniu Asset
    }
}