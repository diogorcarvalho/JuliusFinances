using Microsoft.AspNetCore.Mvc;
using JuliusFinances.Core.Modules.Auth.Application.UseCases.RegisterUser;
using JuliusFinances.Core.Modules.Auth.Application.UseCases.Login;

namespace JuliusFinances.Api.Modules.Auth.Presentation;

/// <summary>
/// Define os endpoints de rotas do módulo de autenticação agrupados em /auth.
/// </summary>
public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/auth")
                       .WithTags("Auth");

        group.MapPost("/register", RegisterAsync);
        group.MapPost("/login", LoginAsync);

        return app;
    }

    private static async Task<IResult> RegisterAsync(
        [FromBody] RegisterUserRequest request,
        [FromServices] RegisterUserUseCase useCase,
        CancellationToken cancellationToken)
    {
        var response = await useCase.ExecuteAsync(request, cancellationToken);
        return Results.Created($"/auth/users/{response.Id}", response);
    }

    private static async Task<IResult> LoginAsync(
        [FromBody] LoginRequest request,
        [FromServices] LoginUseCase useCase,
        CancellationToken cancellationToken)
    {
        var response = await useCase.ExecuteAsync(request, cancellationToken);
        return Results.Ok(response);
    }
}
