namespace MarketSpy.Endpoints;

public static class CoinEndpoints
{
    public static void MapCoinEndpoints(this WebApplication app)
    {
        app.MapGet("/validated-coins", async (CoinGeckoClient coinClient, IValidator<CoinConfig> validator) =>
        {
            var assetNames = new List<string> { "bitcoin", "ethereum", "xrp", "dogecoin", "solana" };
            var coins = await coinClient.GetCoinsAsync(assetNames);

            var validationErrors = new Dictionary<string, List<string>>();

            foreach (var (symbol, config) in coins)
            {
                var result = await validator.ValidateAsync(config);
                if (!result.IsValid)
                    validationErrors[symbol] = result.Errors.Select(e => e.ErrorMessage).ToList();
            }

            return validationErrors.Any()
                ? Results.BadRequest(validationErrors)
                : Results.Ok(coins);
        });
    }
}