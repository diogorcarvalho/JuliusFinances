using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using JuliusFinances.Core.Common.Domain;
using JuliusFinances.Core.Modules.Auth.Domain.Exceptions;
using JuliusFinances.Core.Modules.FinancesSetup.Domain.Exceptions;

namespace JuliusFinances.Api.Common.Errors;

/// <summary>
/// Handler global de exceções para capturar erros e retornar respostas no formato RFC 7807 (Problem Details).
/// </summary>
public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, title, detail) = exception switch
        {
            EmailAlreadyExistsException emailEx => (
                StatusCodes.Status409Conflict,
                "E-mail já cadastrado",
                emailEx.Message),

            CategoryNameAlreadyExistsException catNameEx => (
                StatusCodes.Status409Conflict,
                "Categoria já cadastrada",
                catNameEx.Message),

            CategoryForbiddenAccessException catForbidEx => (
                StatusCodes.Status403Forbidden,
                "Acesso proibido",
                catForbidEx.Message),

            AccountNameAlreadyExistsException accNameEx => (
                StatusCodes.Status409Conflict,
                "Conta já cadastrada",
                accNameEx.Message),

            AccountForbiddenAccessException accForbidEx => (
                StatusCodes.Status403Forbidden,
                "Acesso proibido",
                accForbidEx.Message),

            TransactionForbiddenAccessException txForbidEx => (
                StatusCodes.Status403Forbidden,
                "Acesso proibido",
                txForbidEx.Message),

            DomainException domainEx => (
                StatusCodes.Status400BadRequest,
                "Erro de Regra de Negócio",
                domainEx.Message),

            _ => (
                StatusCodes.Status500InternalServerError,
                "Erro Interno do Servidor",
                "Ocorreu um erro inesperado no servidor. Por favor, tente novamente mais tarde.")
        };

        if (statusCode == StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(exception, "Ocorreu uma exceção não tratada: {Message}", exception.Message);
        }
        else
        {
            _logger.LogWarning("Ocorreu um erro de negócio/validação: {Message}", exception.Message);
        }

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = httpContext.Request.Path
        };

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}
