using InovaBank.Domain.Enums;
using InovaBank.Domain.Events.Accounts;
using InovaBank.Domain.Interfaces;
using InovaBank.Domain.Primitives;
using MassTransit;
using MediatR;

namespace InovaBank.Application.Features.Accounts.Commands.ChangeStatus;

public sealed class ChangeStatusHandler(IAccountRepository _repository, IPublishEndpoint _publishEndpoint, IUnitOfWork _unityOfWork) : IRequestHandler<ChangeStatusCommand, Result<Unit>>
{
    public async Task<Result<Unit>> Handle(ChangeStatusCommand request, CancellationToken ct)
    {
        var guidId = Guid.Parse(request.Id);

        var account = await _repository.GetByIdAsync(guidId, ct);
        if (account is null) return Result<Unit>.Failure("Conta não encontrada.", 404);

        var newStatus = Enum.Parse<AccountStatus>(request.Status, ignoreCase: true);

        var domainResult = account.ChangeStatus(newStatus);

        if (domainResult.IsFailure)
            return Result<Unit>.Failure(domainResult.Error!, domainResult.StatusCode);

        await _publishEndpoint.Publish(new AccountStatusChangedEvent(account.Id, newStatus.ToString(), DateTime.UtcNow), ct);

        await _unityOfWork.SaveChangesAsync(ct);

        return Result<Unit>.Success(Unit.Value);
    }
}
