## Descrição

## Tipo de mudança

- [ ] Nova funcionalidade
- [ ] Correção de bug
- [ ] Refatoração (sem mudança de comportamento)
- [ ] Infraestrutura / CI
- [ ] Documentação

## Checklist

- [ ] Testes unitários/integração cobrem as mudanças (ou não há lógica nova testável)
- [ ] `dotnet format --verify-no-changes --severity error` passa localmente
- [ ] Nenhuma credencial ou secret hardcoded introduzido

## Migrations EF Core

- [ ] Não há migrations neste PR
- [ ] Há migrations **apenas aditivas** (sem `DropTable`, `DropColumn`, `Sql()` bruto no `Up()`)
- [ ] Há migrations **destrutivas** — label `migration-reviewed` adicionada e CODEOWNER notificado

## Breaking changes de API

- [ ] Não há breaking changes no contrato OpenAPI
- [ ] Há breaking changes — label `api-breaking-approved` adicionada e times de frontend notificados
  - Admin Panel: @<!-- mention -->
  - Institucional: @<!-- mention -->

## Notas para o revisor
