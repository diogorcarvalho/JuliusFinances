using JuliusFinances.Core.Modules.Auth.Application.Interfaces;
using JuliusFinances.Core.Modules.Auth.Domain.ValueObjects;
using JuliusFinances.Core.Common.Domain;
using JuliusFinances.Core.Common.Security;

namespace JuliusFinances.Core.Modules.Auth.Application.UseCases.Login;

/// <summary>
/// Contrato de entrada para realizar o login do usuário.
/// </summary>
public record LoginRequest(string Email, string Password);

/// <summary>
/// DTO de representação limpa do usuário logado.
/// </summary>
public record UserDto(Guid Id, string Name, string Email);

/// <summary>
/// Contrato de saída após realizar login com sucesso, contendo o token de acesso.
/// </summary>
public record LoginResponse(string AccessToken, int ExpiresInMinutes, UserDto User);

/// <summary>
/// Caso de Uso responsável por autenticar um usuário e emitir o token JWT correspondente.
/// </summary>
public class LoginUseCase
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public LoginUseCase(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<LoginResponse> ExecuteAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        // 1. Instancia o Value Object Email para normalização automática e validação de formato.
        var email = new Email(request.Email);

        // 2. Busca o usuário no banco com o e-mail correspondente.
        var user = await _userRepository.GetByEmailAsync(email, cancellationToken);

        // 3. Se o usuário não existir ou se a verificação da senha falhar, lança DomainException genérica com mensagem segura.
        if (user == null || !user.Password.Verify(request.Password, _passwordHasher))
        {
            throw new DomainException("E-mail ou senha incorretos.");
        }

        // 4. Gera o token JWT seguro com as claims do usuário e a validade parametrizada obtida pela infraestrutura.
        var generatedToken = _jwtTokenGenerator.GenerateToken(user);

        var userDto = new UserDto(user.Id.Value, user.Name.Value, user.Email.Value);
        return new LoginResponse(generatedToken.Token, generatedToken.ExpiresInMinutes, userDto);
    }
}
