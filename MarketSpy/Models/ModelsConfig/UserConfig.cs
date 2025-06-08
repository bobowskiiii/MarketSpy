using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MarketSpy.Models.ModelsDTOs;

public class UserConfig : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder
            .HasKey(u => u.Id);
        
        builder
            .HasIndex(u => u.Username)
            .IsUnique();

        builder
            .Property(u => u.Username)
            .IsRequired()
            .HasMaxLength(100);

        builder
            .Property(u => u.PasswordHash)
            .IsRequired();
        
    }
}