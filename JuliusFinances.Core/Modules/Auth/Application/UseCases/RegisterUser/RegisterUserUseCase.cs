using JuliusFinances.Core.Modules.Auth.Application.Interfaces;
using JuliusFinances.Core.Modules.Auth.Domain.Entities;
using JuliusFinances.Core.Modules.Auth.Domain.ValueObjects;
using JuliusFinances.Core.Modules.Auth.Domain.Exceptions;
using JuliusFinances.Core.Common.Security;

namespace JuliusFinances.Core.Modules.Auth.Application.UseCases.RegisterUser;

/// <summary>
/// Contrato de entrada para registrar um novo usuário.
/// </summary>
public record RegisterUserRequest(string Name, string Email, string Password);

/// <summary>
/// Contrato de saída após registrar um novo usuário.
/// </summary>
public record RegisterUserResponse(Guid Id, string Name, string Email);

/// <summary>
/// Caso de Uso responsável por registrar um novo usuário no sistema.
/// </summary>
public class RegisterUserUseCase
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;

    public RegisterUserUseCase(IUserRepository userRepository, IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task<RegisterUserResponse> ExecuteAsync(RegisterUserRequest request, CancellationToken cancellationToken = default)
    {
        // 1. Valida as regras de força da senha em texto limpo. Caso inválida, dispara uma DomainException.
        Password.ValidateStrength(request.Password);

        // 2. Instancia os Value Objects Name e Email. Qualquer erro de validação disparará DomainException.
        var name = new Name(request.Name);
        var email = new Email(request.Email);

        // 3. Verifica se o e-mail informado já está cadastrado no banco de dados.
        var emailExists = await _userRepository.ExistsByEmailAsync(email, cancellationToken);
        if (emailExists)
        {
            throw new EmailAlreadyExistsException(email.Value);
        }

        // 4. Criptografa a senha usando o IPasswordHasher e instancia o Value Object Password.
        var password = Password.Create(request.Password, _passwordHasher);

        // 5. Instancia a entidade de domínio User.
        var user = new User(UserId.Unique(), name, email, password);

        // 6. Salva a entidade User no banco de dados.
        await _userRepository.AddAsync(user, cancellationToken);

        return new RegisterUserResponse(user.Id.Value, user.Name.Value, user.Email.Value);
    }
}
