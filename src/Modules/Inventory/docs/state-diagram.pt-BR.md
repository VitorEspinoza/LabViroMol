# Diagrama de Estado — Order (Inventory)

[English](./state-diagram.md) · **Português**

Este documento extrai a seção específica do agregado `Order`. Mostra o ciclo de vida completo do `OrderStatus`, controlado por um enum com
regras de transição explícitas no domínio: todos os estados, todas as transições
válidas, o método de domínio que dispara cada transição e a(s) guarda(s)/precondição(ões)
que bloqueiam transições inválidas.

Fontes: `src/Modules/Inventory/Domain/Orders/Order.cs`, `src/Modules/Inventory/Domain/Orders/OrderStatus.cs`, Handlers em `src/Modules/Inventory/Application/Orders/Commands/{Process,Receive,Cancel,FixDetails}/`.

`OrderStatus` tem 4 estados: `Pending`, `Processing`, `Completed`, `Canceled`. `Completed` e `Canceled` são estados terminais — nenhuma transição parte deles. `FixDetails(...)` é permitido apenas em `Status == Pending`, mas não altera o status (por isso aparece como nota, não como transição formal).

```mermaid
stateDiagram-v2
 [*] --> Pending: Order.Create()

 Pending --> Processing: Process()
 note right of Processing
 Guarda: Status == Pending, senão
 "Apenas pedidos pendentes podem ser processados."
 end note

 Processing --> Completed: Receive()
 note right of Completed
 Guarda: Status == Processing, senão
 "Apenas pedidos em processamento podem ser recebidos."
 Dispara OrderReceivedDomainEvent
 end note

 Pending --> Canceled: Cancel()
 note right of Canceled
 Guarda: Status == Pending, senão
 "Apenas pedidos pendentes podem ser cancelados."
 Processing e Completed não têm saída para Canceled.
 end note

 note left of Pending
 FixDetails() permite editar projeto,
 quantidade e descrição somente em
 Status == Pending. Não altera o status
 (self-transition implícita, sem
 transição formal no diagrama).
 end note

 Completed --> [*]
 Canceled --> [*]
```

**Guia de leitura**: o pedido nasce sempre `Pending`. A partir daí segue dois caminhos possíveis e mutuamente exclusivos: avançar para `Processing` (e depois `Completed`, estado terminal de sucesso) ou ser `Canceled` diretamente (estado terminal de desistência). Uma vez em `Processing` ou `Completed`, o cancelamento não é mais possível.
