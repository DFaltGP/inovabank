using FluentValidation;
using InovaBank.Domain.Enums;

namespace InovaBank.Application.Features.Accounts.Queries.GetStatement;

public sealed class GetStatementValidator : AbstractValidator<GetStatementQuery>
{
    public GetStatementValidator()
    {
        RuleFor(x => x.Id)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("O ID é obrigatório como parâmetro.")
            .Must(BeAValidGuid).WithMessage("O formato do ID fornecido é inválido.");

        RuleFor(x => x.Pagina)
            .GreaterThanOrEqualTo(1).WithMessage("A página deve ser maior ou igual a 1.");

        RuleFor(x => x.TamanhoPagina)
            .InclusiveBetween(1, 100).WithMessage("O tamanho da página deve ser entre 1 e 100.");

        RuleFor(x => x.Tipo)
            .Must(t => string.IsNullOrEmpty(t) || Enum.TryParse<StatementType>(t, true, out _))
            .WithMessage("O tipo de transação deve ser 'Deposito', 'Saque' ou 'Transferencia'.");

        RuleFor(x => x.DataInicio)
            .LessThanOrEqualTo(DateTime.UtcNow).WithMessage("A data inicial não pode estar no futuro.")
            .When(x => x.DataInicio.HasValue);

        RuleFor(x => x.DataFim)
            .GreaterThanOrEqualTo(x => x.DataInicio)
            .WithMessage("A data final deve ser maior ou igual à data inicial.")
            .When(x => x.DataInicio.HasValue && x.DataFim.HasValue);

        RuleFor(x => x)
            .Must(x => (x.DataFim!.Value - x.DataInicio!.Value).TotalDays <= 90)
            .WithMessage("O período da consulta não pode ser superior a 90 dias.")
            .When(x => x.DataInicio.HasValue && x.DataFim.HasValue);
    }

    private bool BeAValidGuid(string id) => Guid.TryParse(id, out _);
}
