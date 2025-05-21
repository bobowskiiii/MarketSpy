namespace MarketSpy.Models;

public class Asset
{
    public int Id { get; set; }
    public string Symbol { get; set; }
    public string? Name { get; set; }
    
    public ICollection<AssetPrice> AssetPrices { get; set; }
    
}