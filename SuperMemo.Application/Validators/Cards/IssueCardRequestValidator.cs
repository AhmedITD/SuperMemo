using System.Text.RegularExpressions;
using FluentValidation;
using SuperMemo.Application.DTOs.requests.Cards;
using SuperMemo.Domain.Enums;

namespace SuperMemo.Application.Validators.Cards;

public class IssueCardRequestValidator : AbstractValidator<IssueCardRequest>
{
    private static readonly Regex DigitsOnly = new(@"^\d+$", RegexOptions.Compiled);

    public IssueCardRequestValidator()
    {
        RuleFor(x => x.AccountId)
            .GreaterThan(0)
            .WithMessage("Account ID must be a positive number.");

        RuleFor(x => x.Number)
            .NotEmpty()
            .WithMessage("Card number is required.")
            .Length(12, 19)
            .WithMessage("Card number must be between 12 and 19 digits.")
            .Must(n => n != null && DigitsOnly.IsMatch(n))
            .WithMessage("Card number must contain only digits (0-9).");

        RuleFor(x => x.Type)
            .IsInEnum()
            .WithMessage("Card type must be Virtual (0), MasterCard (1), or VisaCard (2).");

        RuleFor(x => x.ExpiryDate)
            .NotEmpty()
            .WithMessage("Expiry date is required.")
            .GreaterThan(DateTime.UtcNow.Date)
            .WithMessage("Expiry date must be in the future.")
            .LessThanOrEqualTo(DateTime.UtcNow.Date.AddYears(10))
            .WithMessage("Expiry date cannot be more than 10 years from today.");

        RuleFor(x => x.SecurityCode)
            .NotEmpty()
            .WithMessage("Security code is required.")
            .Length(3, 4)
            .WithMessage("Security code must be 3 or 4 digits.")
            .Must(c => c != null && DigitsOnly.IsMatch(c))
            .WithMessage("Security code must contain only digits (0-9).");
    }
}
