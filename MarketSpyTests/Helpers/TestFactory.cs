using Microsoft.Extensions.Configuration;
using MarketSpy.CoinAPI;

namespace TestProject1.Helpers;

public class TestFactory
{
    public static IConfigurationRoot CreateConfig(string apiKey = "123456")
    {
        var inMemorySettings = new Dictionary<string, string>
        {
            { "CoinGeckoApiKey", apiKey }
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();
    }

    public static CoinGeckoClient CreateCoinGeckoClient(string apiKey = "123456")
    {
        var config = CreateConfig(apiKey);
        return new CoinGeckoClient(config);
    }
}