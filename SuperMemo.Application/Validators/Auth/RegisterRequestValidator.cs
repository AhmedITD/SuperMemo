using FluentValidation;
using SuperMemo.Application.DTOs.requests.Auth;

namespace SuperMemo.Application.Validators.Auth;

public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required.")
            .MaximumLength(200).WithMessage("Full name must not exceed 200 characters.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.");

        RuleFor(x => x.Phone)
            .NotEmpty().WithMessage("Phone is required for registration.")
            .MaximumLength(20).WithMessage("Phone must not exceed 20 characters.");

        RuleFor(x => x.VerificationCode)
            .NotEmpty().WithMessage("Verification code is required.")
            .Length(6).WithMessage("Verification code must be 6 digits.");

        RuleFor(x => x.ImageUrl).MaximumLength(2048).When(x => !string.IsNullOrEmpty(x.ImageUrl));

        When(x => x.IcDocument != null, () =>
        {
            RuleFor(x => x.IcDocument!.IdentityCardNumber).NotEmpty().MaximumLength(50);
            RuleFor(x => x.IcDocument!.FullName).NotEmpty().MaximumLength(200);
            RuleFor(x => x.IcDocument!.MotherFullName).NotEmpty().MaximumLength(200);
            RuleFor(x => x.IcDocument!.BirthLocation).NotEmpty().MaximumLength(200);
            RuleFor(x => x.IcDocument!.ImageUrl).MaximumLength(2048).When(x => !string.IsNullOrEmpty(x.IcDocument?.ImageUrl));
        });
        When(x => x.PassportDocument != null, () =>
        {
            RuleFor(x => x.PassportDocument!.PassportNumber).NotEmpty().MaximumLength(50);
            RuleFor(x => x.PassportDocument!.FullName).NotEmpty().MaximumLength(200);
            RuleFor(x => x.PassportDocument!.Nationality).NotEmpty().MaximumLength(100);
            RuleFor(x => x.PassportDocument!.MotherFullName).NotEmpty().MaximumLength(200);
            RuleFor(x => x.PassportDocument!.ImageUrl).MaximumLength(2048).When(x => !string.IsNullOrEmpty(x.PassportDocument?.ImageUrl));
        });
        When(x => x.LivingIdentityDocument != null, () =>
        {
            RuleFor(x => x.LivingIdentityDocument!.SerialNumber).NotEmpty().MaximumLength(50);
            RuleFor(x => x.LivingIdentityDocument!.FullFamilyName).NotEmpty().MaximumLength(200);
            RuleFor(x => x.LivingIdentityDocument!.LivingLocation).NotEmpty().MaximumLength(200);
            RuleFor(x => x.LivingIdentityDocument!.FormNumber).NotEmpty().MaximumLength(50);
            RuleFor(x => x.LivingIdentityDocument!.ImageUrl).MaximumLength(2048).When(x => !string.IsNullOrEmpty(x.LivingIdentityDocument?.ImageUrl));
        });
    }
}
