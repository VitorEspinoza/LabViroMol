using System.Text.Json;

namespace LabViroMol.LoadTests.Data;

public sealed class SeedCatalog
{
    public required List<string> AuthUserEmails { get; init; } = [];
    public required List<Guid> MaterialTypeIds { get; init; } = [];
    public required List<Guid> EquipmentIds { get; init; } = [];
    public required List<Guid> PendingScheduleIds { get; init; } = [];
    public required List<Guid> ApprovedScheduleIds { get; init; } = [];
    public required List<ProjectWriteTarget> ProjectTargets { get; init; } = [];
    public required List<Guid> ResearcherCandidateIds { get; init; } = [];

    public static SeedCatalog Load(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"Catálogo de seed não encontrado: {path}");

        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<SeedCatalog>(json, SerializerOptions)
               ?? throw new InvalidOperationException("Não foi possível desserializar o catálogo de seed.");
    }

    public static SeedCatalog Empty() => new()
    {
        AuthUserEmails = [],
        MaterialTypeIds = [],
        EquipmentIds = [],
        PendingScheduleIds = [],
        ApprovedScheduleIds = [],
        ProjectTargets = [],
        ResearcherCandidateIds = []
    };

    public void Save(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, JsonSerializer.Serialize(this, SerializerOptions));
    }

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };
}

public sealed class ProjectWriteTarget
{
    public required Guid ProjectId { get; init; }
    public required Guid LeadResearcherId { get; init; }
}

public sealed class ProjectMemberWriteTarget
{
    public required Guid ProjectId { get; init; }
    public required Guid LeadResearcherId { get; init; }
    public required Guid ResearcherId { get; init; }
}
