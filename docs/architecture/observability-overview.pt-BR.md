# Visão Geral de Observabilidade — LabViroMol

[English](./observability-overview.md) · **Português**

Este documento mostra o fluxo real de telemetria implementado na iniciativa de observabilidade:
como uma requisição HTTP, um ciclo do Outbox e um job de tradução em background geram logs
estruturados (`ILogger<T>` + provider OpenTelemetry), métricas e spans (OpenTelemetry SDK), e como
tudo converge para o mesmo exportador OTLP rumo à New Relic. A configuração de todo o pipeline
está em `src/Modules/Shared/Infrastructure/Observability/ObservabilityExtensions.cs`.

Fontes usadas para este diagrama: `ObservabilityExtensions.cs`,
`src/Modules/Shared/Infrastructure/Behaviors/{ValidationBehavior,LoggingBehavior}.cs`,
`src/Modules/Shared/Infrastructure/Observability/{LabViroMolDiagnostics,SlowQueryInterceptor}.cs`,
`src/Modules/Shared/Infrastructure/Persistence/Outbox/OutboxProcessor.cs`,
`src/Modules/Shared/Infrastructure/Translation/{TranslationBackgroundWorker,LibreTranslator}.cs`,
`src/Modules/Notify/Infrastructure/Emails/BrevoEmailSender.cs`.

## Diagrama

```mermaid
flowchart TB
    subgraph HTTP["Requisição HTTP (ASP.NET Core Minimal API)"]
        Req(["Cliente HTTP"]) --> Endpoint["Endpoint (Minimal API)"]
        Endpoint --> Mediator["IMediator.Send"]
        Mediator --> Validation["ValidationBehavior&lt;TMessage,TResponse&gt;"]
        Validation --> Logging["LoggingBehavior&lt;TMessage,TResponse&gt;"]
        Logging --> Handler["Command/Query Handler"]
        Handler --> EfCore["EF Core / Npgsql"]
        EfCore --> SlowQuery["SlowQueryInterceptor\n(Warning se duração >= Observability:SlowQueryMs)"]
    end

    subgraph Background["Processos em background"]
        Outbox["OutboxProcessor&lt;TContext&gt;.ProcessAsync\nActivity 'outbox.process_cycle' / 'outbox.publish'"]
        Translation["TranslationBackgroundWorker.ExecuteAsync\nActivity 'translation.run_job' por ITranslationJob"]
        Translation --> LibreHttp["LibreTranslator.TranslateAsync\n(HttpClient -> LibreTranslate, tags translate.*)"]
    end

    subgraph External["Integrações externas instrumentadas manualmente"]
        Brevo["BrevoEmailSender.SendEmail\nCounter email.failures + Histogram email.latency"]
    end

    Logging -- "ILogger.LogWarning/LogInformation\n+ LabViroMolMetrics.RecordSuccess/RecordFailure/RecordDuration\n(cqrs.requests, cqrs.duration)" --> MelLogging["Microsoft.Extensions.Logging (ILogger&lt;T&gt;)\n→ OpenTelemetry Logging Provider"]
    SlowQuery -- "ILogger.LogWarning" --> MelLogging
    Outbox -- "ILogger.LogError/LogInformation\n+ OutboxMetrics (outbox.messages.processed/failed, outbox.batch.duration, outbox.pending)" --> MelLogging
    Translation -- "ILogger.LogInformation/LogWarning\n+ TranslationMetrics (translation.records, translation.failures, translation.duration)" --> MelLogging
    Brevo -- "ILogger.LogWarning" --> MelLogging

    Logging -. "Activity.Current (AspNetCore instrumentation)" .-> OtelSdk
    Outbox -. "LabViroMolDiagnostics.ActivitySource.StartActivity" .-> OtelSdk
    Translation -. "LabViroMolDiagnostics.ActivitySource.StartActivity" .-> OtelSdk
    LibreHttp -. "Activity.Current?.SetTag(translate.*)\n(HttpClient instrumentation)" .-> OtelSdk
    Brevo -. "AddHttpClientInstrumentation\n(span automático via HttpClient tipado, sem código manual)" .-> OtelSdk

    MelLogging -->|"AddOpenTelemetry(logging => ...)\n(PiiRedactionLogProcessor, TraceId/SpanId via Activity.Current)"| OtlpExporterLogs["OTLP Exporter (logs)"]

    subgraph OtelSdk["OpenTelemetry SDK (AddObservabilityTelemetry)"]
        Tracing["WithTracing\nAddAspNetCoreInstrumentation + AddHttpClientInstrumentation + AddNpgsql\n+ AddSource(LabViroMolDiagnostics.Name)\nTraceIdRatioBasedSampler"]
        Metrics["WithMetrics\nAddAspNetCoreInstrumentation + AddHttpClientInstrumentation + AddRuntimeInstrumentation\n+ AddMeter(LabViroMolDiagnostics.Name) + AddMeter(Npgsql)"]
    end

    Tracing -->|"AddOtlpExporter"| OtlpExporterTraces["OTLP Exporter (traces)"]
    Metrics -->|"AddOtlpExporter (Delta temporality)"| OtlpExporterMetrics["OTLP Exporter (metrics)"]

    OtlpExporterLogs --> NewRelic[("New Relic\notlp.nr-data.net")]
    OtlpExporterTraces --> NewRelic
    OtlpExporterMetrics --> NewRelic

    classDef pipeline fill:#e8f0fe,stroke:#1a73e8;
    classDef background fill:#fef7e0,stroke:#f9ab00;
    classDef external fill:#fce8e6,stroke:#d93025;
    classDef otel fill:#e6f4ea,stroke:#188038;
    classDef sink fill:#f3e8fd,stroke:#9334e6;
    classDef backend fill:#202124,stroke:#202124,color:#fff;

    class Req,Endpoint,Mediator,Validation,Logging,Handler,EfCore,SlowQuery pipeline;
    class Outbox,Translation,LibreHttp background;
    class Brevo external;
    class OtelSdk,Tracing,Metrics otel;
    class MelLogging,OtlpExporterLogs,OtlpExporterTraces,OtlpExporterMetrics sink;
    class NewRelic backend;
```

## Legenda

- **Pipeline HTTP** (azul): toda requisição passa pelo Mediator antes do handler — `ValidationBehavior`
  roda primeiro (falha de validação encurta o pipeline via `Result.Validation`), `LoggingBehavior`
  envolve a execução do handler com `Stopwatch`, registrando `cqrs.duration` (Histogram) sempre, e
  `cqrs.requests` como sucesso/falha (`outcome=success|failure`, `error_type` no caso de falha) —
  ver `LoggingBehavior.cs`. O `SlowQueryInterceptor` (registrado por módulo via
  `AddSlowQueryLogging`) loga em `Warning` quando uma query EF Core/Npgsql excede
  `Observability:SlowQueryMs` (default 500ms).
- **Background** (amarelo): `OutboxProcessor<TContext>` abre uma `Activity` por ciclo
  (`outbox.process_cycle`) e uma por mensagem publicada (`outbox.publish`), reportando
  `outbox.messages.processed`/`outbox.messages.failed` (Counter), `outbox.batch.duration`
  (Histogram) e `outbox.pending` (Gauge). `TranslationBackgroundWorker` abre uma `Activity`
  (`translation.run_job`) por `ITranslationJob` executado e reporta `translation.records`,
  `translation.failures`, `translation.duration`; a chamada HTTP real ao LibreTranslate
  (`LibreTranslator.TranslateAsync`) adiciona tags (`translate.source_lang`, `translate.target_lang`,
  `translate.text_length`) na `Activity` corrente, sem abrir uma própria.
  - Pequena nota de fidelidade: `LibreTranslator` usa `Activity.Current` (a `Activity` aberta pelo
    worker), não `LabViroMolDiagnostics.ActivitySource` diretamente — por isso a seta pontilhada
    de `LibreHttp` para o SDK representa a continuação do mesmo span, não um span novo.
- **Integração externa instrumentada manualmente** (vermelho): `BrevoEmailSender.SendEmail` chama
  `EmailMetrics` (`src/Modules/Shared/Infrastructure/Observability/EmailMetrics.cs`) diretamente ao
  redor da chamada HTTP à API da Brevo, registrando seu próprio `Histogram<double> email.latency`
  (toda tentativa) e `Counter<long> email.failures` (só em exceção), criados em
  `LabViroMolDiagnostics.Meter`, fora do padrão `LabViroMolMetrics`/`OutboxMetrics`/`TranslationMetrics`
  usado nos outros componentes. Não há `Activity`/span manual para essa chamada — o tracing vem
  apenas da instrumentação automática `AddHttpClientInstrumentation` aplicada ao `HttpClient` tipado
  `HttpClient<ISendEmail, BrevoEmailSender>`.
- **Microsoft.Extensions.Logging → OpenTelemetry Logging Provider** (roxo) é o único caminho de
  logging usado pela aplicação (`builder.Logging.AddOpenTelemetry(...)`); sempre
  escreve no console (provider padrão do MEL) e, quando há endpoint OTLP configurado
  (`OpenTelemetry:OtlpEndpoint` ou `OTEL_EXPORTER_OTLP_ENDPOINT`), também via `AddOtlpExporter`,
  incluindo `TraceId`/`SpanId` automáticos via `Activity.Current` para correlação log↔trace.
  O `PiiRedactionLogProcessor` roda como `AddProcessor(...)` redigindo
  atributos sensíveis antes da exportação. Timing de request HTTP não gera log dedicado — vive
  apenas como atributos do span (`AddAspNetCoreInstrumentation`).
- **OpenTelemetry SDK** (verde) é configurado uma única vez em `AddObservabilityTelemetry`: tracing
  com `TraceIdRatioBasedSampler` (taxa configurável via `OpenTelemetry:Tracing:SamplingRatio`,
  default 1.0) e instrumentação automática de ASP.NET Core/HttpClient/Npgsql, mais o
  `ActivitySource` próprio (`LabViroMolDiagnostics.Name = "LabViroMol"`); métricas com
  instrumentação automática de ASP.NET Core/HttpClient/Runtime e o `Meter` próprio
  (mesmo nome), exportadas com `MetricReaderTemporalityPreference.Delta` (exigência da New Relic).
- **OTLP Exporters → New Relic** (preto): logs, traces e métricas convergem para o mesmo endpoint
  OTLP resolvido por `ResolveOtlpEndpoint` (`OpenTelemetry:OtlpEndpoint` em appsettings, com
  fallback para a env var `OTEL_EXPORTER_OTLP_ENDPOINT`) — sem endpoint configurado, nenhum
  exporter/sink OTLP é registrado e a API sobe normalmente apenas com log de console.
