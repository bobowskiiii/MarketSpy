namespace MarketSpy.IAssetService;

public class AssetStorage : IAssetStorage
{
    private readonly MarketSpyDbContext _dbContext;

    public AssetStorage(MarketSpyDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    
    public async Task SaveAssetAsync(string symbol, CoinConfig dto)
    {
        var asset = await _dbContext.Assets.FirstOrDefaultAsync(a => a.Symbol == symbol);

        if (asset == null)
        {
            asset = new Asset()
            {
                Symbol = symbol,
                Name = symbol
            };

            await _dbContext.AddAsync(asset);
            await _dbContext.SaveChangesAsync();
        }

        var price = new AssetPrice()
        {
            AssetId = asset.Id,
            UsdPrice = dto.Usd,
            UsdMarketCap = dto.UsdMarketCap,
            UsdVolume24h = dto.UsdVolume24h,
            UsdChange24h = dto.UsdChange24h,
            LastUpdated = DateTime.UtcNow
        };
        _dbContext.AssetPrices.Add(price);
        await _dbContext.SaveChangesAsync();
    }
}