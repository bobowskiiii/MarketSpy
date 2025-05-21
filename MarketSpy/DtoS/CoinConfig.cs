
public class CoinConfig
{
    [JsonPropertyName("usd")]
    public decimal Usd { get; set; }
    
    [JsonPropertyName("usd_market_cap")]
    public decimal UsdMarketCap { get; set; }
    
    [JsonPropertyName("usd_24h_vol")]
    public decimal UsdVolume24h { get; set; }
    
    [JsonPropertyName("usd_24h_change")]
    public decimal UsdChange24h { get; set; }
    
    [JsonPropertyName("last_updated_at")]
    public long LastUpdatedAtLong { get; set; }
    
    public DateTime LastUpdatedAt => DateTimeOffset
        .FromUnixTimeSeconds(LastUpdatedAtLong)
        .UtcDateTime;
}