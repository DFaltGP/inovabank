using InovaBank.Domain.Common;
using InovaBank.Domain.Primitives;
using MediatR;

namespace InovaBank.Application.Features.Accounts.Queries.GetAccountByCnpj;

public sealed record GetAccountByCnpjQuery(string Cnpj) : IRequest<Result<AccountResponse>>;
