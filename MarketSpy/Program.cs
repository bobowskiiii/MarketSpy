
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
});


//Swagger
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "MyBoards API",
        Version = "v1"
    });
});

//Konfigruacja DbContextu
builder.Services.AddDbContext<MarketSpyDbContext>(options =>
{
    options.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("DefaultConnection"))
        // mySqlOptions => mySqlOptions.EnableStringComparisonTranslations() 
    );
});

builder.Services.AddScoped<AiService>();
builder.Services.AddScoped<IAssetStorage, AssetStorage>();
builder.Services.AddScoped<CoinGeckoClient>();
builder.Services.AddHttpClient<AiService>();    

var app = builder.Build();


app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "MyBoards API v1");
    c.RoutePrefix = string.Empty;
});


if (app.Environment.IsDevelopment()) app.MapOpenApi();

app.UseHttpsRedirection();


using (var scope = app.Services.CreateScope())
{
    var coinClient = scope.ServiceProvider.GetRequiredService<CoinGeckoClient>();
    var assetStorage = scope.ServiceProvider.GetRequiredService<IAssetStorage>();
    var assetsToFetch = new List<string> { "bitcoin", "ethereum", "xrp", "dogecoin", "solana" };
    var coins = await coinClient.GetCoinsAsync(assetsToFetch);

    foreach (var coin in coins) await assetStorage.SaveAssetAsync(coin.Key, coin.Value);
}

//Obsługa błędów
app.UseExceptionHandler(appError =>
{
    appError.Run(async context =>
    {
        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";
        
        var contextFeature = context.Features.Get<IExceptionHandlerFeature>();
        if (contextFeature is not null)
        {
            Console.WriteLine($"Error: {contextFeature.Error}");

            await context.Response.WriteAsJsonAsync(new
            {
                StatusCode = context.Response.StatusCode,
                Message = "Internal server error",
            });
        }
    });
});


//Endpointy

app.MapGet("/test", () =>
{
    throw new Exception("Test exception");
});

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
    var filter = "a";
    var sortBy = "UsdChange24h";
    var sortByDesc = true;
    var page = 1;
    var pageSize = 5;
    var assets = await db.Assets
        .Include(a => a.AssetPrices)
        .OrderByDescending(a => a.Id)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
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
        Analyze the following cryptocurrency data and answer:
        - Is it worth investing in this asset right now?
        - What could be the reason for the recent price change?
        - What are the risks and opportunities?
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

});



app.Run();