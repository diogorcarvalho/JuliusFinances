using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
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

// Configuração de CORS para permitir acessos de outros computadores da rede local
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// 5. Injeção de Dependências (Casos de Uso e Serviços)
// Core
builder.Services.AddScoped<RegisterUserUseCase>();
builder.Services.AddScoped<LoginUseCase>();

// Infraestrutura / Serviços
builder.Services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();
builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
builder.Services.AddScoped<IUserRepository, UserRepository>();

// Registrar suporte ao Swagger / API Explorer
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "JuliusFinances API",
        Version = "v1",
        Description = "API de controle financeiro pessoal JuliusFinances.",
        Contact = new OpenApiContact
        {
            Name = "Time de Desenvolvimento JuliusFinances",
            Email = "suporte@juliusfinances.com"
        }
    });

    // Configurar o suporte para inserção do Token JWT no Swagger UI usando o esquema HTTP Bearer nativo
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Insira o token JWT de acesso."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

    // Carregar os comentários XML para enriquecer a documentação dos Schemas e Endpoints
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

var app = builder.Build();

// Executar migrações automaticamente ao inicializar a aplicação
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<JuliusDbContext>();
    dbContext.Database.Migrate();
}

// 6. Configuração do Pipeline HTTP
app.UseExceptionHandler(); // Ativa o pipeline de IExceptionHandler global

if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("EnableSwagger"))
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "JuliusFinances API v1");
        // Opcional: Define a rota onde o Swagger UI estará acessível.
        // Ex: "swagger" deixará acessível em http://localhost:5000/swagger
        options.RoutePrefix = "swagger";
    });
}

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

// Rota padrão de teste
app.MapGet("/", () => "Hello World!");

// 7. Mapeamento de Rotas Modulares (Minimal APIs)
app.MapAuthEndpoints();

app.Run();
