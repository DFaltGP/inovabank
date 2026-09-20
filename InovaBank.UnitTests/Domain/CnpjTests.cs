using FluentAssertions;
using InovaBank.Domain.Entities;
using InovaBank.Domain.Enums;
using InovaBank.Domain.ValueObjects;
using Xunit;

namespace InovaBank.UnitTests.Domain;

public class CnpjTests()
{
    [Fact]
    public void Constructor_WhenValidParamaters_ShouldCreateCnpj()
    {
        var cnpj = new Cnpj("42832915000103");

        cnpj.Number.Should().Be("42832915000103");
        Cnpj.IsValid(cnpj).Should().BeTrue();
    }

    [Fact]
    public void Constructor_WhenCnpjIsEmpty_ShouldThrowArgumentException()
    {
        var emptyCnpj = "";

        Action act = () => new Cnpj(emptyCnpj);

        act.Should().Throw<ArgumentException>()
           .WithMessage("CNPJ não pode ser vazio.");
    }

    [Theory]
    [InlineData("11111111111111")]
    [InlineData("123")]
    [InlineData("123456789012345")]
    [InlineData("42832915000199")]
    public void Constructor_WhenCnpjIsInvalid_ShouldThrowArgumentException(string invalidCnpj)
    {
        Action act = () => new Cnpj(invalidCnpj);

        act.Should().Throw<ArgumentException>()
            .WithMessage("CNPJ inválido.");
    }
}
