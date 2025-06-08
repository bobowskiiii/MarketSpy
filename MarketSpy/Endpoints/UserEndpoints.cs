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