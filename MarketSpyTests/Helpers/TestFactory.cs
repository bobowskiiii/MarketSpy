namespace TestProject1.Helpers;

public class TestFactory
{
    public static IConfigurationRoot CreateConfig(string apiKey = "123456")
    {
        var inMemorySettings = new Dictionary<string, string>
        {
            { "CoinGeckoApiKey", apiKey },
            { "OpenAiKey", apiKey }
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

    public static AiService CreateAiService(HttpClient client,string apiKey = "123456")
    {
        var config = CreateConfig(apiKey);
        return new AiService(client, config);
    }

    public static DbContextOptions<MarketSpyDbContext> CreateInMemoryDbOptions(string dbName = "TestDb")
    {
        return new DbContextOptionsBuilder<MarketSpyDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
    }
}