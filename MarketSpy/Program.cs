


using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
});


//Swagger with JWT Authentication
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "MyBoards API",
        Version = "v1"
    });
    
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Wpisz: Bearer {token}"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
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

//JWT Authentication
builder.Services.AddAuthorization();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwtConfig = builder.Configuration.GetSection("Jwt");
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtConfig["Issuer"],
            ValidAudience = jwtConfig["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding
                .UTF8.GetBytes(jwtConfig["Key"]!))
        };
    });

builder.Services.AddScoped<AiService>();
builder.Services.AddScoped<IAssetStorage, AssetStorage>();
builder.Services.AddScoped<CoinGeckoClient>();
builder.Services.AddHttpClient<AiService>();
builder.Services.AddScoped<PasswordService>();

//FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<CoinValidator>();


var app = builder.Build();


app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "MarketSpy API v1");
    c.RoutePrefix = string.Empty;
});


if (app.Environment.IsDevelopment()) app.MapOpenApi();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

using (var scope = app.Services.CreateScope())
{
    var coinClient = scope.ServiceProvider.GetRequiredService<CoinGeckoClient>();
    var assetStorage = scope.ServiceProvider.GetRequiredService<IAssetStorage>();
    var assetsToFetch = new List<string> { "bitcoin", "ethereum", "xrp", "dogecoin", "solana" };
    var coins = await coinClient.GetCoinsAsync(assetsToFetch);

    foreach (var coin in coins) 
        await assetStorage.SaveAssetAsync(coin.Key, coin.Value);
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
app.MapCoinEndpoints();
app.MapUserEndpoints();

app.Run();