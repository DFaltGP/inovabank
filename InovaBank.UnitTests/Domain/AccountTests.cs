using FluentAssertions;
using InovaBank.Domain.Entities;
using InovaBank.Domain.Enums;
using InovaBank.Domain.ValueObjects;
using Xunit;

namespace InovaBank.UnitTests.Domain;

public class AccountTests
{
    [Fact]
    public void Constructor_WhenValidParamaters_ShouldInitializeAccountCorrectly()
    {
        var cnpj = new Cnpj("42832915000103");
        var razaoSocial = "LOJA DO COMPUTADOR LTDA";
        var agencia = "0001";
        var imagemDocumentoPath = "docs/comprovante.png";

        var account = new Account(cnpj, razaoSocial, agencia, imagemDocumentoPath);

        account.Balance.Should().Be(0m);
        account.Status.Should().Be(AccountStatus.Ativa);
        account.CanPerformTransactions.Should().BeTrue();

        account.Id.Should().NotBeEmpty();
        account.Agencia.Should().Be(agencia);
        account.Cnpj.Should().Be(cnpj);
        account.ImagemDocumentoPath.Should().Be(imagemDocumentoPath);
        account.RazaoSocial.Should().Be(razaoSocial);
        account.Transactions.Should().BeEmpty();
    }

    [Fact]
    public void Deposit_WhenAmountIsValid_ShouldIncreaseBalanceAndReturnSuccess()
    {
        var account = CreateValidAccount();
        var depositAmount = 150m;

        var result = account.Deposit(depositAmount);

        result.IsSuccess.Should().BeTrue();

        account.Balance.Should().Be(depositAmount);
    }

    [Fact]
    public void Deposit_WhenAmountIsInvalid_ShouldNotChangeBalanceAndReturnFailure()
    {
        var account = CreateValidAccount();
        var depositAmount = -20m;

        var result = account.Deposit(depositAmount);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Valor do depósito deve ser maior que zero");
        result.StatusCode.Should().Be(422);

        account.Balance.Should().Be(0m);
    }

    [Fact]
    public void Deposit_WhenCannotPerformTransactions_ShouldNotChangeBalanceAndReturnFailure()
    {
        var account = CreateValidAccount();
        var depositAmount = 150m;

        account.ChangeStatus(AccountStatus.Bloqueada);

        var result = account.Deposit(depositAmount);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Conta não permite depósitos no status atual.");
        result.StatusCode.Should().Be(422);

        account.Balance.Should().Be(0m);
    }

    [Fact]
    public void Withdraw_WhenAmountIsValid_ShouldDecreaseBalanceAndReturnSuccess()
    {
        var account = CreateValidAccount();
        var withdrawAmount = 150m;

        account.Deposit(withdrawAmount);

        var result = account.Withdraw(withdrawAmount);

        result.IsSuccess.Should().BeTrue();

        account.Balance.Should().Be(0m);
    }

    [Fact]
    public void Withdraw_WhenAmountIsInvalid_ShouldNotDecreaseBalanceAndReturnFailure()
    {
        var account = CreateValidAccount();
        var withdrawAmount = -20m;

        var result = account.Withdraw(withdrawAmount);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Valor do saque deve ser maior que zero");
        result.StatusCode.Should().Be(422);

        account.Balance.Should().Be(0m);
    }

    [Fact]
    public void Withdraw_WhenCannotPerformTransactions_ShouldNotChangeBalanceAndReturnFailure()
    {
        var account = CreateValidAccount();
        var withdrawAmount = 150m;

        account.ChangeStatus(AccountStatus.Bloqueada);

        var result = account.Withdraw(withdrawAmount);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Conta não permite saques no status atual.");
        result.StatusCode.Should().Be(422);

        account.Balance.Should().Be(0m);
    }

    [Fact]
    public void Withdraw_WhenAmountIsGreaterThanBalance_ShouldNotChangeBalanceAndReturnFailure()
    {
        var account = CreateValidAccount();
        var depositAmount = 150m;

        account.Deposit(depositAmount);

        var withdrawAmount = 151m;

        var result = account.Withdraw(withdrawAmount);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Saldo insuficiente.");
        result.StatusCode.Should().Be(422);

        account.Balance.Should().Be(150m);
    }

    [Fact]
    public void Credit_WhenAmountIsValid_ShouldIncreaseBalanceThenAddTransactionAndReturnSuccess()
    {
        var account = CreateValidAccount();
        var creditAmount = 20m;

        var result = account.Credit(creditAmount, "BRL", "Teste unitário");

        result.IsSuccess.Should().BeTrue();

        account.Balance.Should().Be(20m);
        account.Transactions.Should().HaveCount(1);

        var transaction = account.Transactions.Should().ContainSingle().Which;

        transaction.Amount.Should().Be(20m);
        transaction.Type.Should().Be(TransactionType.Deposito);
    }

    [Fact]
    public void Credit_WhenAmountIsValidAndCannotPerformTransactions_ShouldNotIncreaseBalanceAndNotAddTransactionAndReturnFailure()
    {
        var account = CreateValidAccount();
        var creditAmount = 20m;
        account.ChangeStatus(AccountStatus.Bloqueada);

        var result = account.Credit(creditAmount, "BRL", "Teste unitário");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Conta bloqueada ou encerrada.");
        result.StatusCode.Should().Be(422);

        account.Balance.Should().Be(0);
        account.Transactions.Should().BeEmpty();
    }

    [Fact]
    public void Debit_WhenAmountIsValid_ShouldDecreaseBalanceThenAddTransactionAndReturnSuccess()
    {
        var account = CreateValidAccount();
        var depositAmount = 50m;
        var debitAmount = 20m;

        account.Deposit(depositAmount);

        var result = account.Debit(debitAmount, "BRL", "Teste unitário");

        result.IsSuccess.Should().BeTrue();

        account.Balance.Should().Be(30m);
        account.Transactions.Should().HaveCount(1);

        var transaction = account.Transactions.Should().ContainSingle().Which;

        transaction.Amount.Should().Be(20m);
        transaction.Type.Should().Be(TransactionType.Saque);
    }

    [Fact]
    public void Debit_WhenAmountIsValidAndCannotPerformTransactions_ShouldNotDecreaseBalanceAndNotAddTransactionAndReturnFailure()
    {
        var account = CreateValidAccount();
        var debitAmount = 20m;
        account.ChangeStatus(AccountStatus.Bloqueada);

        var result = account.Debit(debitAmount, "BRL", "Teste unitário");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Conta bloqueada ou encerrada.");
        result.StatusCode.Should().Be(422);

        account.Balance.Should().Be(0);
        account.Transactions.Should().BeEmpty();
    }

    [Fact]
    public void Debit_WhenInsufficientBalance_ShouldNotDecreaseBalanceAndNotAddTransactionAndReturnFailure()
    {
        var account = CreateValidAccount();
        var debitAmount = 20m;

        var result = account.Debit(debitAmount, "BRL", "Teste unitário");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Saldo insuficiente.");
        result.StatusCode.Should().Be(422);

        account.Balance.Should().Be(0);
        account.Transactions.Should().BeEmpty();
    }

    [Fact]
    public void ChangeStatus_WhenStatusIsValid_ShouldChangeStatusAndReturnSuccess()
    {
        var account = CreateValidAccount();

        var result = account.ChangeStatus(AccountStatus.Bloqueada);

        account.Status.Should().Be(AccountStatus.Bloqueada);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void ChangeStatus_WhenStatusIsInvalid_ShouldNotChangeStatusAndReturnFailure()
    {
        var account = CreateValidAccount();

        account.ChangeStatus(AccountStatus.Encerrada);
        var result = account.ChangeStatus(AccountStatus.Bloqueada);

        account.Status.Should().Be(AccountStatus.Encerrada);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Não é possível alterar o status de uma conta encerrada.");
        result.StatusCode.Should().Be(422);
    }

    [Fact]
    public void Close_WhenAccountCanBeClosed_ShouldCloseAndReturnSuccess()
    {
        var account = CreateValidAccount();

        var result = account.Close();

        account.Status.Should().Be(AccountStatus.Encerrada);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Close_WhenAlreadyClosed_ShouldNotCloseAndReturnFailure()
    {
        var account = CreateValidAccount();
        account.Close();
        var result = account.Close();

        account.Status.Should().Be(AccountStatus.Encerrada);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Conta já encerrada.");
        result.StatusCode.Should().Be(422);
    }

    [Fact]
    public void Close_WhenAmountIsGreaterThanZero_ShouldNotCloseAndReturnFailure()
    {
        var account = CreateValidAccount();
        account.Deposit(20m);

        var result = account.Close();

        account.Status.Should().NotBe(AccountStatus.Encerrada);
        account.Balance.Should().Be(20m);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Só é possível encerrar contas com saldo zero.");
        result.StatusCode.Should().Be(422);
    }

    private static Account CreateValidAccount()
    {
        var cnpj = new Cnpj("42832915000103");
        var razaoSocial = "LOJA DO COMPUTADOR LTDA";
        var agencia = "0001";
        var imagemDocumentoPath = "docs/comprovante.png";

        return new Account(cnpj, razaoSocial, agencia, imagemDocumentoPath);
    }
}
