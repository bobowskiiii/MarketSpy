namespace MarketSpy.Models;

public class AssetPrice
{
    public int Id { get; set; }
    public decimal UsdPrice { get; set; }
    public decimal UsdMarketCap { get; set; }
    public decimal UsdVolume24h { get; set; }
    public decimal UsdChange24h { get; set; }
    public DateTime LastUpdated { get; set; }
    
    //relacja jeden do wielu od strony Asset
    public int AssetId { get; set; }
    public Asset Asset { get; set; }
}