using InovaBank.Domain.Entities;
using InovaBank.Domain.Events.Accounts;
using InovaBank.Domain.Interfaces;
using InovaBank.Domain.Primitives;
using InovaBank.Domain.ValueObjects;
using MassTransit;
using MediatR;

namespace InovaBank.Application.Features.Accounts.Commands.OpenAccount;

public sealed class OpenAccountHandler(
    IAccountRepository _repository,
    IReceitaWsService _receitaService,
    IFileStorageService _fileStorageService,
    IPublishEndpoint _publishEndpoint,
    IUnitOfWork _unitOfWork) : IRequestHandler<OpenAccountCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(OpenAccountCommand request, CancellationToken ct)
    {
        var cnpj = new Cnpj(request.Cnpj);
        if (await _repository.ExistsCnpjAsync(cnpj, ct))
            return Result<Guid>.Failure("Já existe uma conta aberta para este CNPJ.", 409);

        var receitaResult = await _receitaService.GetCompanyByCnpjAsync(cnpj, ct);
        if (receitaResult.IsFailure)
            return Result<Guid>.Failure(receitaResult.Error!, receitaResult.StatusCode);

        var fileName = $"{Guid.NewGuid()}.jpg";
        var imagePath = await _fileStorageService.UploadAsync(request.ImagemDocumento, fileName, ct);

        var account = new Account(
            cnpj,
            receitaResult.Value!.RazaoSocial,
            request.Agencia,
            imagePath);

        await _repository.AddAsync(account, ct);

        await _publishEndpoint.Publish(new AccountCreatedEvent(
            account.Id,
            account.Cnpj.Number,
            account.Agencia,
            account.RazaoSocial,
            imagePath,
            DateTime.UtcNow
        ), ct);

        try
        {
            await _unitOfWork.SaveChangesAsync(ct);
        }
        catch
        {
            await _fileStorageService.DeleteAsync(imagePath, ct);
            throw;
        }

        return Result<Guid>.Created(account.Id);
    }
}
