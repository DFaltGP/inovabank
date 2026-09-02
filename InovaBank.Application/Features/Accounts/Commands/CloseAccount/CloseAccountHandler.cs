using InovaBank.Domain.Events.Accounts;
using InovaBank.Domain.Interfaces;
using InovaBank.Domain.Primitives;
using MassTransit;
using MediatR;

namespace InovaBank.Application.Features.Accounts.Commands.CloseAccount;

public sealed class CloseAccountHandler(IAccountRepository _repository, IPublishEndpoint _publishEndpoint, IUnitOfWork _unityOfWork) : IRequestHandler<CloseAccountCommand, Result<Unit>>
{
    public async Task<Result<Unit>> Handle(CloseAccountCommand request, CancellationToken ct)
    {
        var guidId = Guid.Parse(request.Id);

        var account = await _repository.GetByIdAsync(guidId, ct);
        if (account is null) return Result<Unit>.Failure("Conta não encontrada.", 404);

        var domainResult = account.Close();

        if (domainResult.IsFailure)
            return Result<Unit>.Failure(domainResult.Error!, 422);

        await _publishEndpoint.Publish(new AccountClosedEvent(account.Id, DateTime.UtcNow), ct);

        await _unityOfWork.SaveChangesAsync(ct);

        return Result<Unit>.Success(Unit.Value);
    }
}
