# Diagrama de Casos de Uso — Módulo Identity

[English](./use-case-diagram.md) · **Português**

Este documento extrai a seção específica do módulo **Identity**. Cobre os casos de uso de autenticação e gestão de usuários/perfis/
permissões, agrupados em 5 capacidades de alto nível: autenticação pública (login/logout),
recuperação/redefinição de senha, gestão de usuários (CRUD, ativar/desativar), gestão de
roles e permissões, e atualização da própria conta. Interagem com este módulo os atores
**Admin** (gestão completa de Identity) e **Usuário / Visitante** (acesso à área pública
de autenticação).

```mermaid
flowchart LR
 Admin(["Admin"])
 Visitante(["Usuário / Visitante"])

 subgraph Auth["Área Pública (Sem Autenticação)"]
 direction TB
 UC_Login(["Realizar Login / Logout"])
 UC_Recuperar(["Recuperar / Redefinir Senha"])
 end

 subgraph Gestao["Gestão de Identidade (Requer Autenticação)"]
 direction TB
 UC_GerirUsuarios(["Gerenciar Usuários (CRUD, Ativar/Desativar)"])
 UC_GerirPerfis(["Gerenciar Roles e Permissões"])
 UC_GerirPropriaConta(["Atualizar Próprio Perfil e Senha"])
 end

 Visitante --> Auth
 Admin --> Auth
 Admin --> Gestao
 
 classDef actor fill:#fff3cd,stroke:#a67c00,stroke-width:1px,color:#000
 classDef publicUc fill:#e2f0d9,stroke:#548235,color:#000
 classDef adminUc fill:#d6e8ff,stroke:#1f5fa8,color:#000

 class Admin,Visitante actor
 class UC_Login,UC_Recuperar publicUc
 class UC_GerirUsuarios,UC_GerirPerfis,UC_GerirPropriaConta adminUc
```

**Relações cross-módulo originadas em outros módulos que dependem de Identity** (não
desenhadas aqui por pertencerem ao diagrama de origem, listadas para referência):
`Inventory.Gerenciar Catálogo e Kits`, `Assets.Gerenciar Equipamentos`,
`Research.Administrar Projetos` e `Scheduling.Analisar Fila de Solicitações` dependem da
autenticação (`Identity.Realizar Login / Logout`) — ver as notas nas seções desses
módulos.
