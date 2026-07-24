# Diagrama de Casos de Uso — Módulo Research

[English](./use-case-diagram.md) · **Português**

Este documento apresenta o diagrama de casos de uso do módulo **Research**. Cobre
a gestão de parceiros, posições/cargos, projetos de pesquisa, membros de projeto e
publicações, agrupados em 4 capacidades: consulta pública ao acervo institucional,
condução de projetos pelos pesquisadores, administração de projetos e gestão de
publicações/cadastros base pelo Admin. Interagem com este módulo os atores **Admin**,
**Pesquisador** e **Estudante Externo / Visitante**.

```mermaid
flowchart LR
 Admin(["Admin"])
 Pesq(["Pesquisador"])
 Visitante(["Estudante Externo / Visitante"])

 subgraph Pesquisa["Módulo de Pesquisa e Extensão (LabViroMol)"]
 direction TB
 
 subgraph Publico["Portal Institucional (Acesso Público)"]
 UC_AcervoPublico(["Consultar Acervo de Pesquisa\n(Projetos, Publicações, Parceiros e Equipe)"])
 end

 subgraph Operacao["Execução Científica (Pesquisadores)"]
 UC_ConduzirProjeto(["Conduzir Projetos de Pesquisa\n(Alterar Status, Gerir Membros e Liderança)"])
 end

 subgraph Governanca["Governança e Administração (Restrito)"]
 UC_CriarProjeto(["Administrar Projetos\n(Criar, Editar, Excluir)"])
 UC_Publicacoes(["Gerenciar Publicações Científicas\n(Cadastro, Autoria, Reordenação e DOI)"])
 UC_CadastrosBase(["Gerenciar Cadastros Base\n(Parceiros e Cargos)"])
 end
 end

 Visitante --> UC_AcervoPublico
 
 Pesq --> UC_ConduzirProjeto
 
 Admin --> UC_ConduzirProjeto
 Admin --> UC_CriarProjeto
 Admin --> UC_Publicacoes
 Admin --> UC_CadastrosBase

 classDef actor fill:#fff3cd,stroke:#a67c00,stroke-width:1px,color:#000
 classDef publicUc fill:#d9f7d9,stroke:#2e7d32,color:#000
 classDef researcherUc fill:#f3e0ff,stroke:#6a1fa8,color:#000
 classDef adminUc fill:#d6e8ff,stroke:#1f5fa8,color:#000

 class Admin,Pesq,Visitante actor
 class UC_AcervoPublico publicUc
 class UC_ConduzirProjeto researcherUc
 class UC_CriarProjeto,UC_Publicacoes,UC_CadastrosBase adminUc
```

**Relações cross-módulo:**
- `Administrar Projetos` depende de `Identity.Realizar Login / Logout` (autenticação) —
 ver Mapa de Contexto (`context-map.md`) para o mecanismo de integração.
