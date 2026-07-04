# Observability Overview — LabViroMol

**English** · [Português](./observability-overview.pt-BR.md)

This document shows the actual telemetry flow implemented in the observability initiative:
how an HTTP request, an Outbox cycle and a background translation job generate structured
logs (`ILogger<T>` + OpenTelemetry provider), metrics and spans (OpenTelemetry SDK), and how
everything converges into the same OTLP exporter toward New Relic. The configuration of the
whole pipeline is in `src/Modules/Shared/Infrastructure/Observability/ObservabilityExtensions.cs`.

Sources used for this diagram: `ObservabilityExtensions.cs`,
`src/Modules/Shared/Infrastructure/Behaviors/{ValidationBehavior,LoggingBehavior}.cs`,
`src/Modules/Shared/Infrastructure/Observability/{LabViroMolDiagnostics,SlowQueryInterceptor}.cs`,
`src/Modules/Shared/Infrastructure/Persistence/Outbox/OutboxProcessor.cs`,
`src/Modules/Shared/Infrastructure/Translation/{TranslationBackgroundWorker,LibreTranslator}.cs`,
`src/Modules/Notify/Infrastructure/Emails/SendEmail.cs` (the `SmtpEmailSender` class).

## Diagram

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
        Smtp["SmtpEmailSender.SendEmail\nActivity 'email.send' + Counters email.sent/email.failed + Histogram email.latency"]
    end

    Logging -- "ILogger.LogWarning/LogInformation\n+ LabViroMolMetrics.RecordSuccess/RecordFailure/RecordDuration\n(cqrs.requests, cqrs.duration)" --> MelLogging["Microsoft.Extensions.Logging (ILogger&lt;T&gt;)\n→ OpenTelemetry Logging Provider"]
    SlowQuery -- "ILogger.LogWarning" --> MelLogging
    Outbox -- "ILogger.LogError/LogInformation\n+ OutboxMetrics (outbox.messages.processed/failed, outbox.batch.duration, outbox.pending)" --> MelLogging
    Translation -- "ILogger.LogInformation/LogWarning\n+ TranslationMetrics (translation.records, translation.failures, translation.duration)" --> MelLogging
    Smtp -- "ILogger.LogWarning" --> MelLogging

    Logging -. "Activity.Current (AspNetCore instrumentation)" .-> OtelSdk
    Outbox -. "LabViroMolDiagnostics.ActivitySource.StartActivity" .-> OtelSdk
    Translation -. "LabViroMolDiagnostics.ActivitySource.StartActivity" .-> OtelSdk
    LibreHttp -. "Activity.Current?.SetTag(translate.*)\n(HttpClient instrumentation)" .-> OtelSdk
    Smtp -. "LabViroMolDiagnostics.ActivitySource.StartActivity" .-> OtelSdk

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
    class Smtp external;
    class OtelSdk,Tracing,Metrics otel;
    class MelLogging,OtlpExporterLogs,OtlpExporterTraces,OtlpExporterMetrics sink;
    class NewRelic backend;
```

## Legend

- **HTTP pipeline** (blue): every request goes through the Mediator before the handler —
  `ValidationBehavior` runs first (a validation failure short-circuits the pipeline via
  `Result.Validation`), `LoggingBehavior` wraps the handler execution with a `Stopwatch`,
  always recording `cqrs.duration` (Histogram), and `cqrs.requests` as success/failure
  (`outcome=success|failure`, `error_type` in the failure case) — see `LoggingBehavior.cs`.
  `SlowQueryInterceptor` (registered per module via `AddSlowQueryLogging`) logs at `Warning`
  level when an EF Core/Npgsql query exceeds `Observability:SlowQueryMs` (default 500ms).
- **Background** (yellow): `OutboxProcessor<TContext>` opens one `Activity` per cycle
  (`outbox.process_cycle`) and one per published message (`outbox.publish`), reporting
  `outbox.messages.processed`/`outbox.messages.failed` (Counter), `outbox.batch.duration`
  (Histogram) and `outbox.pending` (Gauge). `TranslationBackgroundWorker` opens one `Activity`
  (`translation.run_job`) per `ITranslationJob` executed and reports `translation.records`,
  `translation.failures`, `translation.duration`; the actual HTTP call to LibreTranslate
  (`LibreTranslator.TranslateAsync`) adds tags (`translate.source_lang`, `translate.target_lang`,
  `translate.text_length`) to the current `Activity`, without opening its own.
  - Small fidelity note: `LibreTranslator` uses `Activity.Current` (the `Activity` opened by
    the worker), not `LabViroMolDiagnostics.ActivitySource` directly — that's why the dotted
    arrow from `LibreHttp` to the SDK represents the continuation of the same span, not a new one.
- **Manually instrumented external integration** (red): `SmtpEmailSender.SendEmail` opens
  `Activity("email.send", ActivityKind.Producer)` and uses its own `Counter<long>`/`Histogram<double>`
  (`email.sent`, `email.failed`, `email.latency`) created directly on
  `LabViroMolDiagnostics.Meter`, outside the `LabViroMolMetrics`/`OutboxMetrics`/`TranslationMetrics`
  pattern used by the other components.
- **Microsoft.Extensions.Logging → OpenTelemetry Logging Provider** (purple) is the only logging
  path used by the application (`builder.Logging.AddOpenTelemetry(...)`); it always writes to
  the console (MEL's default provider) and, when an OTLP endpoint is configured
  (`OpenTelemetry:OtlpEndpoint` or `OTEL_EXPORTER_OTLP_ENDPOINT`), also via `AddOtlpExporter`,
  including automatic `TraceId`/`SpanId` via `Activity.Current` for log↔trace correlation.
  `PiiRedactionLogProcessor` runs as an `AddProcessor(...)`, redacting sensitive attributes
  before export. HTTP request timing does not generate a dedicated log — it lives only as
  span attributes (`AddAspNetCoreInstrumentation`).
- **OpenTelemetry SDK** (green) is configured once in `AddObservabilityTelemetry`: tracing with
  `TraceIdRatioBasedSampler` (rate configurable via `OpenTelemetry:Tracing:SamplingRatio`,
  default 1.0) and automatic ASP.NET Core/HttpClient/Npgsql instrumentation, plus its own
  `ActivitySource` (`LabViroMolDiagnostics.Name = "LabViroMol"`); metrics with automatic
  ASP.NET Core/HttpClient/Runtime instrumentation and its own `Meter` (same name), exported
  with `MetricReaderTemporalityPreference.Delta` (a New Relic requirement).
- **OTLP Exporters → New Relic** (black): logs, traces and metrics all converge on the same
  OTLP endpoint resolved by `ResolveOtlpEndpoint` (`OpenTelemetry:OtlpEndpoint` in appsettings,
  falling back to the `OTEL_EXPORTER_OTLP_ENDPOINT` env var) — with no endpoint configured, no
  OTLP exporter/sink is registered and the API still starts up normally with only console logging.
