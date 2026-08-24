using InovaBank.Domain.Interfaces;
using InovaBank.Domain.Primitives;
using MediatR;
using MassTransit;
using InovaBank.Domain.Events.Transactions;

namespace InovaBank.Application.Features.Transactions.Commands.Withdraw;

public sealed class WithdrawHandler(IAccountRepository _repository, IUnitOfWork _unitOfWork, ICacheService _cache, IPublishEndpoint _publishEndpoint) : IRequestHandler<WithdrawCommand, Result<Unit>>
{
    public async Task<Result<Unit>> Handle(WithdrawCommand request, CancellationToken ct)
    {
        var idempotencyKey = $"idempotency:withdraw:{request.IdempotencyKey}";
        
        var acquired = await _cache.SetIfNotExistsAsync(idempotencyKey, true, TimeSpan.FromMinutes(2), ct);

        if (!acquired)
            return Result<Unit>.Failure("Transação já processada ou em andamento", 409);

        var guidId = Guid.Parse(request.AccountId);

        var account = await _repository.GetByIdAsync(guidId, ct);
        if (account is null)
        {
            await _cache.RemoveAsync(idempotencyKey, ct);
            return Result<Unit>.Failure("Conta não encontrada.", 404);
        }

        var result = account.Debit(request.Valor, request.Moeda, request.Descricao);

        if (result.IsFailure)
        {
            await _cache.RemoveAsync(idempotencyKey, ct);
            return Result<Unit>.Failure(result.Error!, result.StatusCode);
        }    

        var transaction = account.Transactions.Last();

        await _publishEndpoint.Publish(new TransactionCreatedEvent(
            transaction.Id,
            account.Id,
            -transaction.Amount,
            transaction.Currency,
            transaction.Type.ToString(),
            transaction.Description,
            transaction.CreatedAt
        ), ct);

        await _unitOfWork.SaveChangesAsync(ct);

        await _cache.SetAsync(idempotencyKey, true, TimeSpan.FromHours(24), ct);

        return Result<Unit>.Success(Unit.Value);
    }
}
