using Microsoft.EntityFrameworkCore;

namespace MarketSpy.Data;

public class MarketSpyDbContext : DbContext
{
    public MarketSpyDbContext(DbContextOptions<MarketSpyDbContext> options) : base(options)
    {
    }
    
    public DbSet<Asset> Assets { get; set; }
    public DbSet<AssetPrice> AssetPrices { get; set; }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(GetType().Assembly);
    }
}