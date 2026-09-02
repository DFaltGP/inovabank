using InovaBank.Domain.Common;
using InovaBank.Domain.Interfaces;
using InovaBank.Domain.Primitives;
using InovaBank.Domain.ValueObjects;
using MediatR;

namespace InovaBank.Application.Features.Accounts.Queries.GetAccountByCnpj;

public sealed class GetAccountByCnpjHandler(IAccountReadRepository _readRepository) : IRequestHandler<GetAccountByCnpjQuery, Result<AccountResponse>>
{
    public async Task<Result<AccountResponse>> Handle(GetAccountByCnpjQuery request, CancellationToken ct)
    {
        if (!Cnpj.IsValid(request.Cnpj))
            return Result<AccountResponse>.Failure("CNPJ inválido.", 422);

        var account = await _readRepository.GetByCnpjAsync(new Cnpj(request.Cnpj), ct);

        if (account is null)
            return Result<AccountResponse>.Failure("Conta não encontrada.", 404);

        return Result<AccountResponse>.Success(account);
    }
}
