using SuperMemo.Application.DTOs.requests.Cards;
using FluentValidation;

namespace SuperMemo.Application.Validators.Cards;

public class IssueCardRequestValidator : AbstractValidator<IssueCardRequest>
{
    public IssueCardRequestValidator()
    {
        RuleFor(x => x.AccountId).GreaterThan(0);
        RuleFor(x => x.Number).NotEmpty().Length(12, 19);
        RuleFor(x => x.ExpiryDate).GreaterThan(DateTime.UtcNow.Date);
        RuleFor(x => x.SecurityCode).NotEmpty().Length(3, 4);
    }
}
