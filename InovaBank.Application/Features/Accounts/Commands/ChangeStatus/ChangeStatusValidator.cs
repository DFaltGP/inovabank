using FluentValidation;
using InovaBank.Domain.Enums;

namespace InovaBank.Application.Features.Accounts.Commands.ChangeStatus;

public sealed class ChangeStatusValidator : AbstractValidator<ChangeStatusCommand>
{
    public ChangeStatusValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("O ID é obrigatório como parâmetro.")
            .Must(BeAValidGuid).WithMessage("O formato do ID fornecido é inválido.");

        RuleFor(x => x.Status)
            .IsEnumName(typeof(AccountStatus), caseSensitive: false).NotEqual(AccountStatus.Encerrada.ToString())
            .WithMessage("Status inválido. Use 'Ativa' ou 'Bloqueada'.");
    }

    private bool BeAValidGuid(string id) => Guid.TryParse(id, out _);
}
