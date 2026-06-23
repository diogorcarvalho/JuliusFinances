# Especificação: Instalação e Documentação com Swagger no JuliusFinances.Api

Este documento especifica as diretrizes de instalação, configuração técnica e padronização para a documentação de endpoints utilizando OpenAPI/Swagger no projeto **JuliusFinances.Api**. Isso garante que desenvolvedores possam compreender os contratos, testar rotas manualmente e simular a autenticação JWT diretamente pelo navegador.

---

## 1. Pacotes NuGet e Configuração do Projeto

No ecossistema .NET 10, o pacote **`Swashbuckle.AspNetCore`** continua sendo a escolha mais comum para habilitar a geração de documentação OpenAPI integrada com a clássica interface visual **Swagger UI**.

### 1.1. Comando para Instalação:
Execute o comando a partir da raiz do repositório:
```bash
dotnet add JuliusFinances.Api/JuliusFinances.Api.csproj package Swashbuckle.AspNetCore --version 6.6.2
```

*(Ou adicione diretamente à seção `<ItemGroup>` no arquivo `JuliusFinances.Api.csproj`)*
```xml
<PackageReference Include="Swashbuckle.AspNetCore" Version="6.6.2" />
```

### 1.2. Habilitar Geração de Arquivo de Documentação XML:
Para que o Swagger possa extrair e exibir os comentários de documentação triplos (`/// <summary>`) nos endpoints e schemas dos DTOs, adicione as configurações abaixo no arquivo `JuliusFinances.Api.csproj` dentro de `<PropertyGroup>`:
```xml
<GenerateDocumentationFile>true</GenerateDocumentationFile>
<NoWarn>$(NoWarn);1591</NoWarn>
```

---

## 2. Configuração de Serviços e Middleware (`Program.cs`)

Para ativar o Swagger e integrá-lo adequadamente com o fluxo de autenticação JWT, as configurações abaixo devem ser adicionadas em `Program.cs`.

### 2.1. Importações Necessárias
Adicione as seguintes namespaces no topo de `Program.cs`:
```csharp
using System.Reflection;
using Microsoft.OpenApi.Models;
```

### 2.2. Registro de Serviços no Container de DI
Adicione o suporte ao SwaggerGen, configure o suporte nativo a tokens de autenticação Bearer JWT e inclua o carregamento de comentários XML antes da chamada de `builder.Build()`:

```csharp
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
```

### 2.3. Configuração do Pipeline HTTP (Middleware)
O pipeline do Swagger é exposto no ambiente de desenvolvimento (`Development`) ou em outros ambientes (como `Production`) mediante a chave de configuração dinâmica `"EnableSwagger": true`. Isso permite que o painel seja acessado de forma segura e flexível em múltiplos ambientes.

Adicione o middleware logo após a instanciação do `app`:

```csharp
if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("EnableSwagger"))
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "JuliusFinances API v1");
        // Opcional: Define a rota onde o Swagger UI estará acessível.
        // Ex: "swagger" deixará acessível em http://localhost:5290/swagger (ou pelo IP da rede local, ex: http://192.168.15.25:5290/swagger)
        options.RoutePrefix = "swagger";
    });
}
```

---

## 3. Padronização de Documentação de Endpoints (Minimal APIs)

Para manter a API rica em detalhes e auto-explicativa, as Minimal APIs devem usar as extensões nativas do ASP.NET Core para declarar metadados das rotas de forma explícita.

### Diretrizes de Decoração de Rotas:
1. **Tags (`WithTags`):** Agrupar endpoints de forma coesa (ex: `"Auth"`, `"Transactions"`).
2. **Nome da Rota (`WithName`):** Definir um identificador único para o endpoint.
3. **Resumos e Descrições (`WithSummary` e `WithDescription`):** Indicar de forma breve e clara a responsabilidade do endpoint.
4. **Respostas Esperadas (`Produces` e `ProducesProblem`):** Mapear os tipos de retorno, códigos de status HTTP e possíveis respostas de erro.

### Exemplo Prático de Documentação (`AuthEndpoints.cs`):

```csharp
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

        group.MapPost("/register", RegisterAsync)
             .WithName("RegisterUser")
             .WithSummary("Registrar um novo usuário")
             .WithDescription("Cria uma nova conta de usuário no sistema JuliusFinances validando os dados fornecidos de acordo com as regras de negócio.")
             .Produces<RegisterUserResponse>(StatusCodes.Status201Created)
             .ProducesProblem(StatusCodes.Status400BadRequest)
             .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/login", LoginAsync)
             .WithName("LoginUser")
             .WithSummary("Autenticar usuário")
             .WithDescription("Realiza o login de um usuário existente e retorna as credenciais junto com o token JWT de acesso.")
             .Produces<LoginResponse>(StatusCodes.Status200OK)
             .ProducesProblem(StatusCodes.Status400BadRequest);

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
```

---

## 4. Fluxo de Teste e Validação

Após aplicar as configurações especificadas:

1. **Suba o Banco de Dados (Docker):**
   ```bash
   docker compose up -d
   ```
2. **Inicie a API:**
   ```bash
   dotnet run --project JuliusFinances.Api/JuliusFinances.Api.csproj
   ```
3. **Acesse o Painel:**
   Abra o navegador no endereço: `http://localhost:5290/swagger` (localmente) ou pelo IP do servidor na rede doméstica se estiver acessando de outro computador (ex: `http://192.168.15.25:5290/swagger`).
4. **Fluxo de Teste da Autenticação:**
   * Envie uma requisição válida para `/auth/register` no Swagger UI para criar um usuário.
   * Envie uma requisição de login em `/auth/login` para receber o token de acesso.
   * Copie o valor do token de resposta (`accessToken`).
   * Clique em **Authorize** no topo superior direito da página do Swagger UI.
   * No campo de texto, cole apenas o token copiado (o Swagger UI incluirá o prefixo `Bearer` automaticamente por usar o tipo de esquema HTTP).
   * Clique em **Authorize** e feche a janela.
   * Realize chamadas em rotas protegidas e certifique-se de obter as respostas desejadas.
