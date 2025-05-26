namespace MarketSpy.IAssetService;

public interface IAssetStorage
{
    Task SaveAssetAsync(string symbol, CoinConfig dto);
}