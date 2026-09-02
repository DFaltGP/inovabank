using InovaBank.Domain.Common;
using InovaBank.Domain.Primitives;
using MediatR;

namespace InovaBank.Application.Features.Accounts.Queries.GetAccountById;

public sealed record GetAccountByIdQuery(string Id) : IRequest<Result<AccountResponse>>;
