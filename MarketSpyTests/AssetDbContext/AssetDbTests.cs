using TestProject1.Helpers;

namespace TestProject1;

public class AssetDbTests
{
    [Fact]
    public async Task AddAsset_SavesAssetToDatabase()
    {
        //Arrange
        var options = TestFactory.CreateInMemoryDbOptions();

        //Act
        using (var context = new MarketSpyDbContext(options))
        {
            var asset = new Asset {Name = "bitcoin", Symbol = "bitcoin"};
            context.Assets.Add(asset);
            await context.SaveChangesAsync();
        }
        
        //Assert
        using (var context = new MarketSpyDbContext(options))
        {
            var asset = await context.Assets.FirstOrDefaultAsync(a => a.Symbol == "bitcoin");
            Assert.NotNull(asset);
            Assert.Equal("bitcoin", asset.Name);
        }
    }
}