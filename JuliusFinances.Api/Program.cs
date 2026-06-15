using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using JuliusFinances.Core.Common.Security;
using JuliusFinances.Core.Modules.Auth.Application.Interfaces;
using JuliusFinances.Core.Modules.Auth.Application.UseCases.Login;
using JuliusFinances.Core.Modules.Auth.Application.UseCases.RegisterUser;
using JuliusFinances.Api.Common.Database;
using JuliusFinances.Api.Common.Errors;
using JuliusFinances.Api.Common.Security;
using JuliusFinances.Api.Modules.Auth.Infrastructure.Persistence;
using JuliusFinances.Api.Modules.Auth.Infrastructure.Services;
using JuliusFinances.Api.Modules.Auth.Presentation;

var builder = WebApplication.CreateBuilder(args);

// 1. Configuração e Validação de Opções (Fail-Fast com ValidateOnStart)
builder.Services.AddOptions<JwtSettings>()
    .Bind(builder.Configuration.GetSection(JwtSettings.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

// 2. Configuração do EF Core com PostgreSQL + Convenções snake_case
builder.Services.AddDbContext<JuliusDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
           .UseSnakeCaseNamingConvention();
});

// 3. Tratamento de Exceções Global com IExceptionHandler (RFC 7807)
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// 4. Configuração de Autenticação e Autorização JWT
var jwtSecret = builder.Configuration["JwtSettings:Secret"];
if (string.IsNullOrEmpty(jwtSecret))
{
    throw new InvalidOperationException("A chave secreta JwtSettings:Secret não foi configurada.");
}
var key = Encoding.UTF8.GetBytes(jwtSecret);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// 5. Injeção de Dependências (Casos de Uso e Serviços)
// Core
builder.Services.AddScoped<RegisterUserUseCase>();
builder.Services.AddScoped<LoginUseCase>();

// Infraestrutura / Serviços
builder.Services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();
builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
builder.Services.AddScoped<IUserRepository, UserRepository>();

var app = builder.Build();

// 6. Configuração do Pipeline HTTP
app.UseExceptionHandler(); // Ativa o pipeline de IExceptionHandler global

app.UseAuthentication();
app.UseAuthorization();

// Rota padrão de teste
app.MapGet("/", () => "Hello World!");

// 7. Mapeamento de Rotas Modulares (Minimal APIs)
app.MapAuthEndpoints();

app.Run();
