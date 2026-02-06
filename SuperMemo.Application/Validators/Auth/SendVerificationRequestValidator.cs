using FluentValidation;
using SuperMemo.Application.DTOs.requests.Auth;

namespace SuperMemo.Application.Validators.Auth;

public class SendVerificationRequestValidator : AbstractValidator<SendVerificationRequest>
{
    public SendVerificationRequestValidator()
    {
        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("Phone number is required.")
            .MaximumLength(20).WithMessage("Phone number must not exceed 20 characters.");
    }
}
