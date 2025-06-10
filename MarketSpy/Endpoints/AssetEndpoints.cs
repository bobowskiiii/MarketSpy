namespace MarketSpy.Endpoints;

public static class AssetEndpoints
{
    public static void MapAssetEndpoints(this WebApplication app)
    {
        app.MapGet("/test", () => { throw new Exception("Test exception"); });

        //List of all assets
        app.MapGet("/assets", async (MarketSpyDbContext db) =>
        {
            var assets = await db.Assets
                .ToListAsync();
            return Results.Ok(assets);
        });

        //List of all assets with prices
        app.MapGet("/assetsPrices", async (MarketSpyDbContext db) =>
        {
            var assets = await db.Assets
                .Include(a => a.AssetPrices)
                .OrderByDescending(a => a.Id)
                .ToListAsync();

            return Results.Ok(assets);
        });


        //Assets wth prices by symbol
        app.MapGet("/assets/symbol={symbol}", async (string symbol, MarketSpyDbContext db) =>
        {
            var asset = await db.Assets
                .Include(a => a.AssetPrices
                    .OrderByDescending(ap => ap.LastUpdated))
                .FirstOrDefaultAsync(a => a.Symbol == symbol);

            if (asset is null)
                return Results.NotFound("There is no such asset");
            return Results.Ok(asset);
        });

        //Asset wth prices if true
        app.MapGet("/assets/istrue/{symbol}", async (string symbol, MarketSpyDbContext db) =>
        {
            var wthHistData = true;

            var asset = await db.Assets
                .Include(a => a.AssetPrices)
                .FirstOrDefaultAsync(a => a.Symbol == symbol);

            if (asset is null)
                return Results.NotFound("There is no such asset");


            if (wthHistData)
                return Results.Ok(new
                {
                    Name = asset?.Name,
                    UsdPrice = asset?.AssetPrices.OrderByDescending(ap => ap.LastUpdated).Select(ap => new
                    {
                        Price = ap.UsdPrice,
                        MarketCap = ap.UsdMarketCap,
                        Volume24h = ap.UsdVolume24h,
                        Change24h = ap.UsdChange24h,
                        LastUpdated = ap.LastUpdated
                    })
                    .FirstOrDefault()
                });

            return Results.Ok(new
            {
                Name = asset?.Name,
                UsdPrice = asset?.AssetPrices
                    .OrderByDescending(ap => ap.LastUpdated)
                    .Select(ap => ap.UsdPrice)
                    .FirstOrDefault()
            });
        });


        //Assets wth prices by id
        app.MapGet("/assets/{id}", async (int id, MarketSpyDbContext db) =>
        {
            var asset = await db.Assets
                .Include(a => a.AssetPrices)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (asset == null)
                return Results.NotFound();

            return Results.Ok(asset);
        });

        //Assets that have price >= 2000 wth their last price
        app.MapGet("/test/data", async (MarketSpyDbContext db) =>
        {
            var grouped = await db.AssetPrices
                .Where(ap => ap.UsdPrice >= 2000)
                .Include(ap => ap.Asset)
                .ToListAsync();

            var asset = grouped
                .GroupBy(ap => ap.Asset.Id)
                .Select(g => g.OrderByDescending(ap => ap.LastUpdated).First())
                .Select(ap => new
                {
                    Name = ap.Asset.Name,
                    Price = ap.UsdPrice,
                    MarketCap = ap.UsdMarketCap,
                    Volume24h = ap.UsdVolume24h,
                    Change24h = ap.UsdChange24h,
                    LastUpdated = ap.LastUpdated
                })
                .ToList();

            if (asset.Count == 0)
                return Results.NotFound();
            return Results.Ok(asset);
        });


        //New Asset wth price info
        app.MapPost("/newasset/", async (MarketSpyDbContext db) =>
        {
            var asset = new Asset()
            {
                Symbol = "Bitcoin",
                Name = "Bitcoin",
                AssetPrices = new List<AssetPrice>()
                {
                    new AssetPrice()
                    {
                        UsdPrice = 1000,
                        UsdMarketCap = 1000,
                        UsdVolume24h = 1000,
                        UsdChange24h = 1000,
                        LastUpdated = DateTime.UtcNow
                    }
                }
            };
            await db.Assets.AddAsync(asset);
            await db.SaveChangesAsync();
        });

        //Put Asset by id
        app.MapPut("/assets/{id}", async (int id, Asset updatedAsset, MarketSpyDbContext db) =>
        {
            var asset = await db.Assets
                .FindAsync(id);
            if (asset == null)
                return Results.NotFound();
            asset.Name = updatedAsset.Name;
            asset.Symbol = updatedAsset.Symbol;

            await db.SaveChangesAsync();
            return Results.Ok(asset);
        });


        //Delete Asset by id
        app.MapDelete("/assets/{id}", async (int id, MarketSpyDbContext db) =>
        {
            var asset = await db.Assets
                .FindAsync(id);
            if (asset == null)
                return Results.NotFound();
            db.Assets.Remove(asset);
            await db.SaveChangesAsync();

            return Results.Ok(asset);
        });


        //Paginacja
        app.MapGet("/assetsPaged", async (MarketSpyDbContext db) =>
        {
            var page = 1;
            var pageSize = 5;
            var assets = await db.Assets
                .Include(a => a.AssetPrices)
                .OrderByDescending(a => a.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(a => new
                {
                    a.Id,
                    a.Symbol,
                    a.Name,
                    PricesCount = a.AssetPrices.Count(),
                    AvgPrice = a.AssetPrices.Any() ? a.AssetPrices.Average(a => a.UsdPrice) : 0,
                    MinPrice = a.AssetPrices.Any() ? a.AssetPrices.Min(a => a.UsdPrice) : 0,
                    MaxPrice = a.AssetPrices.Any() ? a.AssetPrices.Max(a => a.UsdPrice) : 0,
                    TotalVolume24h = a.AssetPrices.Any() ? a.AssetPrices.Sum(p => p.UsdVolume24h) : (decimal?)null,
                })
                .ToListAsync();

            return Results.Ok(assets);
        });


        //OpenAI Test
        app.MapPost("/openai", async (AiService aiService, AiDto dto) =>
        {
            if (string.IsNullOrEmpty(dto.Prompt))
                return Results.BadRequest("Prompt is empty");

            var result = await aiService.GetSummaryAsync(dto.Prompt);
            return Results.Ok(result);
        });


        //Crypto Analisis wth OpenAI
        app.MapGet("/crypto-analysis/{symbol}", async (string symbol, MarketSpyDbContext db, AiService service) =>
        {
            var asset = await db.Assets
                .Include(a => a.AssetPrices)
                .FirstOrDefaultAsync(a => a.Symbol.ToLower() == symbol.ToLower());

            if (asset == null)
                return Results.NotFound();

            var latestPrice = asset.AssetPrices
                .OrderByDescending(ap => ap.LastUpdated)
                .FirstOrDefault();
            if (latestPrice == null)
                return Results.NotFound("No price data for this coin");

            var prompt = $@"
            Przeanalizuj dane kryptowaluty, dane i wiadomości ze świata i odpowiedz na pytania:
            - Czy warto inwestować w ten aktyw teraz?
            - Jaka może być przyczyna ostatniej zmiany ceny?
            - Czy lepiej zshortować czy trzymać dłużej?
            Odpowiedź co ty byś zrobił na moim miejscu.
            Odpowiedź powinna być krótka i zwięzła, maksymalnie 6 zdań.
            Zacznij od słów 'Analiza kryptowaluty {asset.Name}:'.
            Oraz kolejne zdanie Tak, warto inwestować, Nie, nie warto inwestować, Shortuj, Trzymaj dłużej.
            Weź pod uwagę ostatnie wydarzenia i wiadomości ze świata kryptowalut/i ogolnie swiata
            , do swojej analizy bo dobrze wiemy że może to mieć duży wpływ.
            Data:
            Name: {asset.Name}
            Symbol: {asset.Symbol}
            Price: {latestPrice.UsdPrice} USD
            Market Cap: {latestPrice.UsdMarketCap} USD
            Volume (24h): {latestPrice.UsdVolume24h} USD
            Change (24h): {latestPrice.UsdChange24h}%
            Last Updated: {latestPrice.LastUpdated}
            ";

            var analysis = await service.GetSummaryAsync(prompt);
            return Results.Ok(analysis);
        })
        .RequireAuthorization();
        

        //Crypto Analisis wth OpenAI, 500USD to invest
        app.MapGet("/crypto-analysis500", async (MarketSpyDbContext db, AiService service) =>
        {
            var assets = await db.Assets
                .Include(a => a.AssetPrices
                    .OrderByDescending(ap => ap.LastUpdated))
                .Select(a => new
                {
                    Name = a.Symbol,
                    Price = a.AssetPrices.Any()
                        ? a.AssetPrices.OrderByDescending(ap => ap.LastUpdated).Select(ap => ap.UsdPrice)
                            .FirstOrDefault()
                        : 0,
                    MarketCap = a.AssetPrices.Any() ? a.AssetPrices.Average(a => a.UsdPrice) : 0,
                    Volume24h = a.AssetPrices.Any() ? a.AssetPrices.Average(a => a.UsdVolume24h) : 0,
                    Change24h = a.AssetPrices.Any()
                        ? a.AssetPrices.OrderByDescending(a => a.LastUpdated).Select(a => a.UsdChange24h)
                            .FirstOrDefault()
                        : 0,
                    LastUpdated = a.AssetPrices.Any()
                        ? a.AssetPrices.OrderByDescending(ap => ap.LastUpdated).Select(ap => ap.LastUpdated)
                            .FirstOrDefault()
                        : DateTime.Now
                })
                .ToListAsync();

            var promptBuilder = new StringBuilder();
            promptBuilder.AppendLine(
                "Załóżmy że masz 500 USD do zainwestowania w kryptowaluty," +
                " jesteś swiadomy ryzyka  i wszelkich z nim związanych rzeczy " +
                "więc nie musisz nic o nim mówić. Przeanalizuj dane kryptowaluty, " +
                "dane i wiadomości ze świata  i przygotuj gotowy plan inwestycyjny " +
                "stworzony pod maxymalizację zysków: " +
                "- W co z podanych aktywów zainwestowałbyś te 500 USD? " +
                "- Jak byś to podzielił? " +
                "- Czy zszortowałbyś czy trzymał długo, jesli tak to które dokładnie?" +
                "- Odpowiedz w miarę krótko i zwięźle, maksymalnie 8 zdań. " +
                "- Ani JEDNEGO słowa o ryzyku, ani o tym że to nie jest porada inwestycyjna. " +
                "- Masz prawo wybrać tylko z podanych aktywów, nie możesz dodać innych. " +
                "Podaj w skrócie(ale serio dużym skrócie wydarzenia z ostatnich dni/tygodni ze świata które uwierzytelniają twoją tezę które powinny wzrosnąć itd ");

            foreach (var asset in assets)
            {
                promptBuilder.AppendLine($@"
                Name: {asset.Name}
                Price: {asset.Price} USD
                Market Cap: {asset.MarketCap} USD
                Volume (24h): {asset.Volume24h} USD
                Change (24h): {asset.Change24h}%
                Last Updated: {asset.LastUpdated}
                ");
            }

            var prompt = promptBuilder.ToString();
            var analysis = await service.GetSummaryAsync(prompt);

            if (analysis.Length == 0)
                return Results.NotFound("Brak danych");
            return Results.Ok(analysis);
        })
        .RequireAuthorization();
    }
}