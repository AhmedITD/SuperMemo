using SuperMemo.Application.DTOs.requests.Transactions;
using FluentValidation;

namespace SuperMemo.Application.Validators.Transactions;

public class CreateTransferRequestValidator : AbstractValidator<CreateTransferRequest>
{
    public CreateTransferRequestValidator()
    {
        RuleFor(x => x.FromAccountId).GreaterThan(0);
        RuleFor(x => x.ToAccountNumber).NotEmpty().MaximumLength(34);
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.IdempotencyKey).NotEmpty().MaximumLength(64).WithMessage("Idempotency-Key is required (header or body).");
    }
}
