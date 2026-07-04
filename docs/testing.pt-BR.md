# Testes

[English](./testing.md) · **Português**

## Projetos de teste

```
tests/
├── UnitTests/
│   └── Modules/
│       └── <Module>/
│           └── Domain/       # Testes de entidades e lógica de agregados
└── IntegrationTests/
    └── Modules/
        └── <Module>/         # Testes completos de endpoints HTTP
```

## Testes unitários

Os testes unitários cobrem a lógica de domínio isoladamente — métodos de agregado, comportamento de value objects e regras de negócio. Sem banco de dados ou stack HTTP envolvidos.

**Bibliotecas**:
- xUnit como test runner e para asserções
- Bogus para geração de dados fake realistas
- NSubstitute para mockar dependências

**Exemplo de padrão**:

```csharp
public class MaterialTests
{
    [Fact]
    public void UpdateStock_BelowMinimum_RaisesLowStockEvent()
    {
        var material = MaterialFaker.Generate();
        material.UpdateStock(material.MinStock - 1);
        material.Events.Should().ContainSingle(e => e is LowStockEvent);
    }
}
```

## Testes de integração

Os testes de integração sobem o pipeline completo do ASP.NET Core em memória usando `WebApplicationFactory`. Eles acionam endpoints reais via HTTP com um banco EF em memória.

**Bibliotecas**:
- Microsoft.AspNetCore.Mvc.Testing
- xUnit
- Bogus
- NSubstitute (para mocks de serviços externos, ex.: e-mail)

**Helpers principais**:
- `IdentityIntegrationTestWebAppFactory` — factory customizada que configura o banco em memória e semeia dados de teste
- Helper de token JWT — gera tokens válidos para requisições autenticadas

**Exemplo de padrão**:

```csharp
public class CreateMaterialTests : IClassFixture<InventoryIntegrationTestWebAppFactory>
{
    [Fact]
    public async Task Post_ValidRequest_Returns201()
    {
        var client = _factory.CreateAuthenticatedClient();
        var response = await client.PostAsJsonAsync("/api/inventory/materials", new { ... });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }
}
```

## Rodando os testes

```bash
# Todos os testes
dotnet test

# Só testes unitários
dotnet test tests/UnitTests

# Só testes de integração
dotnet test tests/IntegrationTests

# Com relatório de cobertura
dotnet test --collect:"XPlat Code Coverage"
```

## Cobertura

A cobertura é coletada via Coverlet. A configuração está em cada `.csproj` de teste através do pacote `coverlet.collector`.

## Testes de arquitetura

O repositório agora aplica verificações de arquitetura e convenção em três camadas complementares:

- `tests/ArchitectureTests/LabViroMol.ArchitectureTests/` usa ArchUnitNET para validar dependências de camada, isolamento de módulo, convenções de CQRS, modelagem DDD, fronteiras de persistência e regras de presentation.
- `Directory.Build.props`, `.editorconfig` e `BannedSymbols.txt` habilitam analisadores Roslyn para `src/**`, com APIs banidas que quebram o build, como `DateTime.Now`, `DateTime.UtcNow` e `Console`.
- `tests/IntegrationTests/Shared/EndpointAuthorizationTests.cs` valida os metadados de endpoint em runtime para garantir que toda rota esteja protegida por autorização ou explicitamente marcada como pública numa allowlist.

### Como executar

```bash
# Compilar os projetos de produção com os analisadores
dotnet build LabViroMol.sln

# Testes de arquitetura
dotnet test tests/ArchitectureTests/LabViroMol.ArchitectureTests/LabViroMol.ArchitectureTests.csproj

# Guarda de autorização de integração
dotnet test tests/IntegrationTests/Shared/LabViroMol.IntegrationTests.Shared.csproj --filter EndpointAuthorizationTests
```

### Regras opt-in

Algumas regras de arquitetura estão presentes intencionalmente, mas puladas até que a base de código esteja preparada para elas:

- handlers devem ser `sealed`
- implementações de repository/query devem ser `internal`
- agregados não devem expor setters públicos
- fatias de feature intra-módulo devem ser livres de ciclos

Essas regras só devem ser ativadas junto com o refactor correspondente que faz o código atual se conformar a elas.

### Intencionalmente não aplicado

- `ConfigureAwait(false)` não é aplicado porque não é útil no tratamento de requisições do ASP.NET Core.
- Limiares de cobertura ainda são uma preocupação de QA, não uma regra arquitetural.
