# Documentação de Arquitetura — Módulo Assets

[English](./README.md) · **Português**

Este diretório reúne os diagramas de arquitetura específicos do módulo **Assets**.

- [`use-case-diagram.md`](./use-case-diagram.pt-BR.md) — casos de uso de gestão de equipamentos e manutenção (CRUD interno e catálogo público).
- [`class-diagram.md`](./class-diagram.pt-BR.md) — diagrama de classes de domínio dos agregados `Equipment` e `MaintenanceRequest`.
- [`state-diagram.md`](./state-diagram.pt-BR.md) — diagrama de estado do agregado `MaintenanceRequest`.
- [`er-diagram.md`](./er-diagram.pt-BR.md) — modelo entidade-relacionamento do schema `assets` (tabelas `Equipments`, `MaintenanceRequests`).

Assets não tem um `sequence-diagrams.md` próprio: nenhum dos fluxos de sequência documentados pertence exclusivamente a este módulo.
