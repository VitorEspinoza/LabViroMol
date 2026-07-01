using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.Http.Headers;
using LabViroMol.LoadTests.Data;
using NBomber.Contracts;
using NBomber.CSharp;
using NBomber.Http.CSharp;

namespace LabViroMol.LoadTests.Infrastructure;

public sealed class LoadTestRuntime
{
    private int _tokenIndex = -1;
    private int _projectIndex = -1;
    private long _projectMemberIndex = -1;
    private long _firstRequestTimestamp;
    private long _lastResponseTimestamp;
    private readonly ConcurrentQueue<Guid> _pendingScheduleIds;
    private readonly SemaphoreSlim _pendingScheduleReplenishment = new(1, 1);

    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, long>> _statusBreakdown = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, OperationApdexStats> _apdexStats = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, string> _operationGroups = new(StringComparer.OrdinalIgnoreCase);

    private LoadTestRuntime(
        LoadTestConfig config,
        CommandLineOptions options,
        HttpClient httpClient,
        SeedCatalog catalog,
        IReadOnlyList<string> tokens)
    {
        Config = config;
        Options = options;
        HttpClient = httpClient;
        Catalog = catalog;
        Tokens = tokens;
        _pendingScheduleIds = new ConcurrentQueue<Guid>(catalog.PendingScheduleIds);
    }

    public LoadTestConfig Config { get; }
    public CommandLineOptions Options { get; }
    public HttpClient HttpClient { get; }
    public SeedCatalog Catalog { get; private set; }
    public IReadOnlyList<string> Tokens { get; }

    public static async Task<LoadTestRuntime> CreateAsync(
        LoadTestConfig config,
        CommandLineOptions options,
        HttpClient httpClient,
        AuthClient authClient,
        bool requiresCatalog,
        bool requiresAuthentication,
        CancellationToken ct)
    {
        var catalog = SeedCatalog.Empty();
        if (requiresCatalog)
        {
            var catalogPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, config.Data.SeedCatalogPath));
            catalog = SeedCatalog.Load(catalogPath);

            if (catalog.ResearcherCandidateIds.Count == 0)
                throw new InvalidOperationException("ResearcherCandidateIds está vazio no catálogo — execute --command=seed novamente.");
        }

        var tokens = requiresAuthentication
            ? await authClient.LoginAllAsync(ct)
            : [];

        return new LoadTestRuntime(config, options, httpClient, catalog, tokens);
    }

    public string NextToken()
    {
        if (Tokens.Count == 0)
            throw new InvalidOperationException("O cenario atual nao inicializou tokens de autenticacao.");

        var index = Interlocked.Increment(ref _tokenIndex);
        return Tokens[index % Tokens.Count];
    }

    public async Task<Guid> NextPendingScheduleIdAsync(CancellationToken ct)
    {
        if (_pendingScheduleIds.TryDequeue(out var id))
            return id;

        await _pendingScheduleReplenishment.WaitAsync(ct);
        try
        {
            if (_pendingScheduleIds.TryDequeue(out id))
                return id;

            var newIds = await Seeder.AppendPendingSchedulesAsync(Config, 500, ct);
            Catalog.PendingScheduleIds.AddRange(newIds);

            foreach (var newId in newIds)
                _pendingScheduleIds.Enqueue(newId);

            if (_pendingScheduleIds.TryDequeue(out id))
                return id;

            throw new InvalidOperationException("Nao foi possivel reabastecer agendamentos pendentes para o teste de carga.");
        }
        finally
        {
            _pendingScheduleReplenishment.Release();
        }
    }

    public ProjectWriteTarget NextProjectTarget()
    {
        var index = Interlocked.Increment(ref _projectIndex);
        return Catalog.ProjectTargets[index % Catalog.ProjectTargets.Count];
    }

    public ProjectMemberWriteTarget NextProjectMemberTarget()
    {
        var index = Interlocked.Increment(ref _projectMemberIndex);
        var project = Catalog.ProjectTargets[(int)(index % Catalog.ProjectTargets.Count)];
        var researcherIndex = (int)((index / Catalog.ProjectTargets.Count) % Catalog.ResearcherCandidateIds.Count);

        return new ProjectMemberWriteTarget
        {
            ProjectId = project.ProjectId,
            LeadResearcherId = project.LeadResearcherId,
            ResearcherId = Catalog.ResearcherCandidateIds[researcherIndex]
        };
    }

    public int RandomPageNumber() => Random.Shared.Next(1, 25);

    public int RandomPageSize() => Random.Shared.Next(0, 3) switch
    {
        0 => 10,
        1 => 20,
        _ => 50
    };

    public Guid RandomEquipmentId()
    {
        if (Catalog.EquipmentIds.Count == 0)
            throw new InvalidOperationException("EquipmentIds está vazio — execute --command=seed antes de rodar este cenário.");
        return Catalog.EquipmentIds[Random.Shared.Next(Catalog.EquipmentIds.Count)];
    }

    public Guid RandomMaterialTypeId()
    {
        if (Catalog.MaterialTypeIds.Count == 0)
            throw new InvalidOperationException("MaterialTypeIds está vazio — execute --command=seed antes de rodar este cenário.");
        return Catalog.MaterialTypeIds[Random.Shared.Next(Catalog.MaterialTypeIds.Count)];
    }

    public HttpRequestMessage CreateRequest(HttpMethod method, string path, bool authenticated = true)
    {
        var request = Http.CreateRequest(method.Method, path);

        if (authenticated)
            request.WithHeader("Authorization", $"Bearer {NextToken()}");

        return request;
    }

    public void RecordStatus(string operation, string statusCode)
    {
        var operationMap = _statusBreakdown.GetOrAdd(operation, _ => new ConcurrentDictionary<string, long>(StringComparer.OrdinalIgnoreCase));
        operationMap.AddOrUpdate(statusCode, 1, static (_, current) => current + 1);
    }

    public async Task<Response<HttpResponseMessage>> SendAsync(
        string operation,
        HttpRequestMessage request,
        TimeSpan apdexThreshold,
        string operationGroup = OperationGroups.Admin)
    {
        _operationGroups.TryAdd(operation, operationGroup);

        var startedAt = Stopwatch.GetTimestamp();
        Interlocked.CompareExchange(ref _firstRequestTimestamp, startedAt, 0);

        var response = await Http.Send(HttpClient, request);
        var elapsed = Stopwatch.GetElapsedTime(startedAt);
        Interlocked.Exchange(ref _lastResponseTimestamp, Stopwatch.GetTimestamp());
        var statusCode = ResolveStatusCode(response);

        RecordStatus(operation, statusCode);
        RecordApdex(operation, elapsed, apdexThreshold, IsSuccessful(response));

        return response;
    }

    public IReadOnlyDictionary<string, IReadOnlyDictionary<string, long>> GetStatusBreakdown()
    {
        return _statusBreakdown.ToDictionary(
            kvp => kvp.Key,
            kvp => (IReadOnlyDictionary<string, long>)kvp.Value.ToDictionary(inner => inner.Key, inner => inner.Value),
            StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyDictionary<string, OperationApdexSummary> GetApdexSummary()
    {
        return _apdexStats.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.ToSummary(),
            StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyDictionary<string, string> GetOperationGroups() =>
        _operationGroups.ToDictionary(kvp => kvp.Key, kvp => kvp.Value, StringComparer.OrdinalIgnoreCase);

    public double GetObservedWindowSeconds()
    {
        var first = Interlocked.Read(ref _firstRequestTimestamp);
        var last = Interlocked.Read(ref _lastResponseTimestamp);

        if (first <= 0 || last <= first)
            return 0;

        return Stopwatch.GetElapsedTime(first, last).TotalSeconds;
    }

    public TimeSpan WarmUpDuration() => TimeSpan.FromSeconds(Config.GetProfile(Options.Profile).WarmUpSeconds);

    public async Task ApplyThinkTimeAsync(CancellationToken ct = default)
    {
        var profile = Config.GetProfile(Options.Profile);
        if (profile.MinThinkTimeSeconds is null && profile.MaxThinkTimeSeconds is null)
            return;

        var min = profile.MinThinkTimeSeconds ?? 0;
        var max = profile.MaxThinkTimeSeconds ?? min;
        if (max <= 0)
            return;

        if (min < 0 || max < min)
            throw new InvalidOperationException($"Think time invalido no perfil '{Options.Profile}'.");

        var delay = min == max
            ? min
            : min + Random.Shared.NextDouble() * (max - min);

        await Task.Delay(TimeSpan.FromSeconds(delay), ct);
    }

    public LoadSimulation[] CreateLoadSimulations(
        bool openModel,
        int? closedCopiesOverride = null,
        int? openRateOverride = null)
    {
        var profile = Config.GetProfile(Options.Profile);
        var duration = TimeSpan.FromSeconds(profile.DurationSeconds);
        var closedCopies = closedCopiesOverride ?? profile.ClosedCopies;
        var openRate = openRateOverride ?? profile.OpenRate;

        if (!openModel)
        {
            return
            [
                Simulation.RampingConstant(copies: closedCopies, during: TimeSpan.FromSeconds(30)),
                Simulation.KeepConstant(copies: closedCopies, during: duration)
            ];
        }

        return
        [
            Simulation.RampingInject(rate: openRate, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(30)),
            Simulation.Inject(rate: openRate, interval: TimeSpan.FromSeconds(1), during: duration)
        ];
    }

    private void RecordApdex(string operation, TimeSpan elapsed, TimeSpan threshold, bool successful)
    {
        var stats = _apdexStats.GetOrAdd(operation, _ => new OperationApdexStats(threshold));
        stats.Record(elapsed, successful);
    }

    private static string ResolveStatusCode(Response<HttpResponseMessage> response)
    {
        if (response.Payload.IsSome())
            return ((int)response.Payload.Value.StatusCode).ToString();

        return response.IsError ? "error" : "unknown";
    }

    private static bool IsSuccessful(Response<HttpResponseMessage> response) =>
        response.Payload.IsSome() && response.Payload.Value.IsSuccessStatusCode;
}

public static class OperationGroups
{
    public const string Admin = "Admin";
    public const string Dashboard = "Dashboard";
    public const string Institutional = "Institutional";
    public const string PublicWrite = "PublicWrite";
}

public sealed record OperationApdexSummary(
    double ThresholdMs,
    long Satisfied,
    long Tolerated,
    long Frustrated,
    long Total,
    double Score);

internal sealed class OperationApdexStats
{
    private long _satisfied;
    private long _tolerated;
    private long _frustrated;

    public OperationApdexStats(TimeSpan threshold)
    {
        Threshold = threshold;
    }

    public TimeSpan Threshold { get; }

    public void Record(TimeSpan elapsed, bool successful)
    {
        if (!successful)
        {
            Interlocked.Increment(ref _frustrated);
            return;
        }

        if (elapsed <= Threshold)
        {
            Interlocked.Increment(ref _satisfied);
            return;
        }

        if (elapsed <= Threshold * 4)
        {
            Interlocked.Increment(ref _tolerated);
            return;
        }

        Interlocked.Increment(ref _frustrated);
    }

    public OperationApdexSummary ToSummary()
    {
        var satisfied = Interlocked.Read(ref _satisfied);
        var tolerated = Interlocked.Read(ref _tolerated);
        var frustrated = Interlocked.Read(ref _frustrated);
        var total = satisfied + tolerated + frustrated;
        var score = total == 0 ? 0 : Math.Round((satisfied + tolerated / 2.0) / total, 4);

        return new OperationApdexSummary(
            Threshold.TotalMilliseconds,
            satisfied,
            tolerated,
            frustrated,
            total,
            score);
    }
}
