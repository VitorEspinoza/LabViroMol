# Documentação de Arquitetura — Módulo Inventory

[English](./README.md) · **Português**

Índice dos diagramas de arquitetura específicos do módulo Inventory.

- [`use-case-diagram.md`](./use-case-diagram.pt-BR.md) — casos de uso de gestão de catálogo/kits, pedidos de compra e movimentação de estoque.
- [`class-diagram.md`](./class-diagram.pt-BR.md) — diagrama de classes dos agregados `MaterialType`, `Material`, `Kit` e `Order`, e da entidade `StockTransaction`.
- [`sequence-diagrams.md`](./sequence-diagrams.pt-BR.md) — diagramas de sequência dos 7 fluxos centrais: Consumir Material para Projeto, Criar/Processar/Receber/Cancelar Pedido, Adicionar/Remover Estoque por Exceção.
- [`state-diagram.md`](./state-diagram.pt-BR.md) — diagrama de estado do ciclo de vida de `Order` (`OrderStatus`).
- [`er-diagram.md`](./er-diagram.pt-BR.md) — diagrama entidade-relacionamento do schema `inventory` (tabelas físicas do Postgres).
