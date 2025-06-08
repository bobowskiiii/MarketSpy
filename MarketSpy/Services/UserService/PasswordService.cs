using Microsoft.AspNetCore.Identity;

namespace MarketSpy.Services.UserService;

public class PasswordService
{
    private readonly PasswordHasher<User> _hasher = new();
    
    public string HashPassword(User user, string password)
        => _hasher.HashPassword(user, password);

    public bool VerifyPassword(User user, string password, string hash)
        => _hasher.VerifyHashedPassword(user, hash, password) == PasswordVerificationResult.Success;
}