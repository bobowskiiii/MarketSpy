using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;

namespace MarketSpy.Endpoints;

public static class UserEndpoints
{
    public static void MapUserEndpoints(this WebApplication app)
    {
        //Register a new user wth hashed password
        app.MapPost("/register", async (UserDto dto, MarketSpyDbContext db, PasswordService hasher) =>
        {
            if (string.IsNullOrWhiteSpace(dto.Username) || string.IsNullOrWhiteSpace(dto.Password))
                return Results.BadRequest("Username and password are required");

            var exist = await db.Users.AnyAsync(u => u.Username == dto.Username);
            if(exist)
                return Results.BadRequest("User already exists");

            var user = new User
            {
                Username = dto.Username,
            };
            user.PasswordHash = hasher.HashPassword(user, dto.Password);
            
            db.Users.Add(user);
            await db.SaveChangesAsync();
            return Results.Ok("User registered successfully");
        });
        
        //Login user and return JWT token
        app.MapPost("/login", async (UserDto dto, MarketSpyDbContext db, PasswordService hasher, IConfiguration config) =>
        {
            var user = await db.Users.FirstOrDefaultAsync(u => u.Username == dto.Username);
            if (user == null || !hasher.VerifyPassword(user, dto.Password, user.PasswordHash))
                return Results.Unauthorized();

            var claims = new[]
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
            };
            
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            
            var token = new JwtSecurityToken(
                issuer: config["Jwt:Issuer"],
                audience: config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: creds);
            
            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

            return Results.Ok(new
            {
                token = tokenString,
            });
        });
        
        //Get all users
        app.MapGet("/users", async (MarketSpyDbContext db) =>
        {
            var users = await db.Users.ToListAsync();
            return Results.Ok(users);
        });
        
        //Get user by id
        app.MapGet("users/{id}", async (int id, MarketSpyDbContext db) =>
        {
            var user = await db.Users.FindAsync(id);
            return user is not null ? Results.Ok(user) : Results.NotFound();
        });
        
        //Delete user by id
        app.MapDelete("users/{id}", async (int id, MarketSpyDbContext db) =>
        {
            var user = await db.Users.FindAsync(id);
            if (user is null)
                return Results.NotFound();

            db.Users.Remove(user);
            await db.SaveChangesAsync();
            return Results.Ok("User deleted successfully");
        });
    }
}