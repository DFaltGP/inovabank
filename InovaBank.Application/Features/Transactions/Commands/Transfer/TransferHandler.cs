using InovaBank.Domain.Events.Transactions;
using InovaBank.Domain.Interfaces;
using InovaBank.Domain.Primitives;
using MassTransit;
using MediatR;

namespace InovaBank.Application.Features.Transactions.Commands.Transfer;

public sealed class TransferHandler(IAccountRepository _repository, IUnitOfWork _unityOfWork, ICacheService _cache, IPublishEndpoint _publishEndpoint) : IRequestHandler<TransferCommand, Result<Unit>>
{
    public async Task<Result<Unit>> Handle(TransferCommand request, CancellationToken ct)
    {
        var idempotencyKey = $"idempotency:transfer:{request.IdempotencyKey}";

        var acquired = await _cache.SetIfNotExistsAsync(idempotencyKey, true, TimeSpan.FromMinutes(2), ct);

        if (!acquired)
            return Result<Unit>.Failure("Transação já processada ou em andamento", 409);

        var sourceGuidId = Guid.Parse(request.SourceAccountId);
        var destGuidId = Guid.Parse(request.DestinationAccountId);

        var ids = new[] { sourceGuidId, destGuidId }.OrderBy(id => id).ToArray();

        var firstAccount = await _repository.GetByIdForUpdateAsync(sourceGuidId, ct);
        var secondAccount = await _repository.GetByIdForUpdateAsync(destGuidId, ct);
        if (firstAccount is null || secondAccount is null)
            return Result<Unit>.Failure("Uma ou ambas as contas não foram encontradas.", 404);

        var source = sourceGuidId == firstAccount.Id ? firstAccount : secondAccount;
        var destination = destGuidId == firstAccount.Id ? firstAccount : secondAccount;

        if (!source.CanPerformTransactions || !destination.CanPerformTransactions)
            return Result<Unit>.Failure("Ambas as contas devem estar ativas para realizar transferências.", 422);

        var debitResult = source.Debit(request.Valor, request.Moeda, $"Transf. Enviada: {request.Descricao}");
        if (!debitResult.IsSuccess)
            return Result<Unit>.Failure(debitResult.Error!, debitResult.StatusCode);

        var creditResult = destination.Credit(request.Valor, request.Moeda, $"Transf. Recebida: {request.Descricao}");
        if (!creditResult.IsSuccess)
            return Result<Unit>.Failure(creditResult.Error!, creditResult.StatusCode);

        var transaction = source.Transactions.Last();

        await _publishEndpoint.Publish(new TransferCreatedEvent(
            transaction.Id,
            source.Id,
            destination.Id,
            request.Valor,
            request.Moeda,
            request.Descricao,
            transaction.CreatedAt
        ), ct);

        await _unityOfWork.SaveChangesAsync(ct);

        return Result<Unit>.Success(Unit.Value);
    }
}
