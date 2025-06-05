namespace MarketSpy.DtoS.Validators;

public class CoinValidator : AbstractValidator<CoinConfig>
{
    public CoinValidator()
    {
        RuleFor(c => c.Usd)
            .NotEmpty()
            .GreaterThan(0)
            .WithMessage("USD price must be greater than 0.");
        
        RuleFor(c => c.UsdMarketCap)
            .NotEmpty()
            .GreaterThan(0);
        
        RuleFor(c => c.UsdVolume24h)
            .GreaterThan(0);

        RuleFor(c => c.UsdChange24h)
            .InclusiveBetween(-100, 100);
        
        RuleFor(c => c.LastUpdatedAtLong)
            .NotEmpty()
            .GreaterThan(0);
        
        RuleFor(c => c.LastUpdatedAt)
            .LessThanOrEqualTo(DateTime.Now)
            .WithMessage("Date cannot be in the future.");
    }
}