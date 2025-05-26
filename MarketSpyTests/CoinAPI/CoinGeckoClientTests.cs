using Flurl.Http;
using Flurl.Http.Testing;
using MarketSpy.CoinAPI;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using TestProject1.Helpers;
namespace TestProject1.CoinAPI;

public class CoinGeckoClientTests
{
    [Fact]
    public async Task GetCoinsAsync_ReturnsDataFromMockedResult()
    {
        using var httpTest = new HttpTest();
        var config = TestFactory.CreateConfig();
        
        //Arrange
        var fakeResponse = new Dictionary<string, CoinConfig>
        {
            ["bitcoin"] = new CoinConfig
            {
                Usd = 6000, 
                UsdMarketCap = 123456789,
                UsdChange24h = 0.5m,
                UsdVolume24h = 100000,
                LastUpdatedAtLong = (long)DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            }
        };

        httpTest.RespondWithJson(fakeResponse);
        var client = TestFactory.CreateCoinGeckoClient();
        
        //Act
        var result = await client.GetCoinsAsync(new List<string>{"bitcoin"});
        
        //Assert
        Assert.NotNull(result);
        Assert.True(result.ContainsKey("bitcoin"));
        Assert.Equal(6000, result["bitcoin"].Usd);
    }
    
    
    [Fact]
    public async Task GetCoinsAsync_ReturnEmptyDictionary_WhenNoAssetsProvided()
    {
        using var httpTest = new HttpTest();
        var config = TestFactory.CreateConfig();
        
        //Arrange
        var response = new Dictionary<string, CoinConfig>
        {
            
        };
        
        httpTest.RespondWithJson(response);
        var client = TestFactory.CreateCoinGeckoClient();

        //Act
        var result = await client.GetCoinsAsync(new List<string>());
        
        //Assert
        Assert.Empty(result);
    }

    
    [Fact]
    public async Task GetCoinsAsync_ThrowsException_WhenApiReturnsError()
    {
        using var httpTest = new HttpTest();
        var config = TestFactory.CreateConfig();
        
        //Arrange 
        httpTest.RespondWith(status: 500);
        var client = TestFactory.CreateCoinGeckoClient();
        
        //Act & Assert
        await Assert.ThrowsAsync<FlurlHttpException>(() => client
            .GetCoinsAsync(new List<string> { "bitcoin" }));

    }

    
    [Fact]
    public async Task GetCoinsAsync_Constructor_ThrowsArgumentNullException_WhenConfigIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new CoinGeckoClient(null));
    }

    
    [Fact]
    public async Task GetCoinsAsync_ThrowsException_WhenApiResponseIsInvalid()
    {
        using var httpTest = new HttpTest();
        var config = TestFactory.CreateConfig();
        
        //Arrange
        httpTest.RespondWithJson(new { invalid = "some data" }, status: 200);
        var client = TestFactory.CreateCoinGeckoClient();
        
        //Act & Assert
        await Assert.ThrowsAsync<FlurlParsingException>(() => client.GetCoinsAsync(new List<string> { "bitcoin" }));
    }

    
    [Fact]
    public async Task GetCoinsAsync_ReturnDataForMultipleAssets()
    {
        using var httpTest = new HttpTest();
        var config = TestFactory.CreateConfig();
        
        //Arrange
        var response = new Dictionary<string, CoinConfig>
        {
            ["bitcoin"] = new CoinConfig
            {
                Usd = 6000,
                UsdMarketCap = 123456789,
                UsdChange24h = 0.5m,
                UsdVolume24h = 100000,
                LastUpdatedAtLong = (long)DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            },
            ["ethereum"] = new CoinConfig
            {
                Usd = 4000,
                UsdMarketCap = 987654321,
                UsdChange24h = -0.2m,
                UsdVolume24h = 200000,
                LastUpdatedAtLong = (long)DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            }
        };
        
        httpTest.RespondWithJson(response);
        var client = TestFactory.CreateCoinGeckoClient();
        
        //Act
        var result = await client.GetCoinsAsync(new List<string> {"bitcoin", "ethereum"});
        
        //Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.True(result.ContainsKey("bitcoin"));
        Assert.True(result.ContainsKey("ethereum"));
    }

    [Fact]
    public async Task GetCoinsAsync_ReturnsEmptyDictionary_WhenAssetListIsEmpty()
    {
        using var httpTest = new HttpTest();
        var config = TestFactory.CreateConfig();
        //Arrange
        httpTest.RespondWithJson(new Dictionary<string, CoinConfig>());
        var client = TestFactory.CreateCoinGeckoClient();
        
        //Act
        var result = await client.GetCoinsAsync(new List<string>());
        
        //Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }
}