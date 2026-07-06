# Diagrama de Casos de Uso — Módulo Assets

[English](./use-case-diagram.md) · **Português**

Este documento apresenta o diagrama de casos de uso específico do módulo **Assets**. Cobre a gestão de equipamentos e manutenção, agrupada em 2 capacidades internas
(gestão de equipamentos e ciclo de manutenção) mais a consulta pública ao catálogo de
equipamentos consumida pelo site institucional. Interagem com este módulo os atores
**Admin** e **Estudante Externo / Visitante**.

```mermaid
flowchart LR
 Admin(["Admin"])
 Visitante(["Estudante Externo / Visitante"])

 subgraph Assets["Módulo de Ativos e Equipamentos (LabViroMol)"]
 direction TB
 
 subgraph Publico["Área Institucional (Acesso Público)"]
 UC_CatalogoPublico(["Consultar Catálogo de Equipamentos\n(Listagem, Detalhes e Itens Agendáveis)"])
 end

 subgraph Interno["Gestão Interna (Acesso Restrito)"]
 UC_GerirEquipamentos(["Gerenciar Equipamentos\n(CRUD e Upload de Imagens)"])
 UC_CicloManutencao(["Gerenciar Ciclo de Manutenção\n(Solicitar, Iniciar, Finalizar e Cancelar)"])
 end
 end

 Visitante --> UC_CatalogoPublico
 Admin --> Interno

 classDef actor fill:#fff3cd,stroke:#a67c00,stroke-width:1px,color:#000
 classDef adminUc fill:#d6e8ff,stroke:#1f5fa8,color:#000
 classDef publicUc fill:#d9f7d9,stroke:#2e7d32,color:#000

 class Admin,Visitante actor
 class UC_GerirEquipamentos,UC_CicloManutencao adminUc
 class UC_CatalogoPublico publicUc
```

**Relações cross-módulo:**
- `Gerenciar Equipamentos` depende de `Identity.Realizar Login / Logout` (autenticação) —
 ver Mapa de Contexto (`context-map.md`) para o mecanismo de integração.
