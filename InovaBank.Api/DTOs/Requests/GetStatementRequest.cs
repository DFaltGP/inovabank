namespace InovaBank.Api.DTOs.Requests;

public sealed record GetStatementRequest(
    DateTime? DataInicio,
    DateTime? DataFim,
    string? Tipo,
    int Pagina = 1,
    int TamanhoPagina = 20);
