
using MarketSpy.Endpoints;

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
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "MarketSpy API v1");
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
app.MapAssetEndpoints();

app.Run();