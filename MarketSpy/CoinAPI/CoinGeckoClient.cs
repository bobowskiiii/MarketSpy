namespace MarketSpy.CoinAPI;

public class CoinGeckoClient
{
     private readonly string? _apiKey;
     public CoinGeckoClient(IConfiguration config)
     {
          if (config == null) throw new ArgumentNullException("Missing key in configuration");
          _apiKey = config["CoinGeckoApiKey"];
     }
     
     public async Task<Dictionary<string, CoinConfig>> GetCoinsAsync(List<string> assetNames)
     {
          string url = "https://api.coingecko.com/api/v3/simple/price";
          var idsParam = string.Join(",", assetNames);

          var results = await url
               .SetQueryParams(new
               {
                    ids = idsParam,
                    vs_currencies = "usd",
                    include_market_cap = "true",
                    include_24hr_vol = "true",
                    include_24hr_change = "true",
                    include_last_updated_at = "true"
               })
               .WithHeader("x-cg-demo-api-key", _apiKey)
               .GetJsonAsync<Dictionary<string, CoinConfig>>();

          foreach (var coin in results)
          {
               Console.WriteLine(coin.Key);
               Console.WriteLine($"Price: {coin.Value.Usd}");
               Console.WriteLine($"Market Cap: {coin.Value.UsdMarketCap}");
               Console.WriteLine($"Volume 24h: {coin.Value.UsdVolume24h}");
               Console.WriteLine($"Change 24h: {coin.Value.UsdChange24h}");
               Console.WriteLine($"Last Updated: {coin.Value.LastUpdatedAt}");
          }
          return results;
     }
     
}