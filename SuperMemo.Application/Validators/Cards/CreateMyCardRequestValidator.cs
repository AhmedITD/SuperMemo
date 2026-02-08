using FluentValidation;
using SuperMemo.Application.DTOs.requests.Cards;

namespace SuperMemo.Application.Validators.Cards;

public class CreateMyCardRequestValidator : AbstractValidator<CreateMyCardRequest>
{
    public CreateMyCardRequestValidator()
    {
        RuleFor(x => x.Type)
            .IsInEnum()
            .WithMessage("Card type must be a valid value: Virtual (0), MasterCard (1), or VisaCard (2).");
    }
}
