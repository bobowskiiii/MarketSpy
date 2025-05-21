using MarketSpy.IAssetService;

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


builder.Services.AddScoped<IAssetStorage, AssetStorage>();
builder.Services.AddScoped<CoinGeckoClient>();

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

//Endpointy

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


app.Run();