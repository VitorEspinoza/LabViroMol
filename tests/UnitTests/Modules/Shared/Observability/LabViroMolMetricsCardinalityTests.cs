using System.Diagnostics.Metrics;
using LabViroMol.Modules.Shared.Infrastructure.Observability;

namespace LabViroMol.Modules.Shared.Infrastructure.UnitTests.Observability;

public class LabViroMolMetricsCardinalityTests
{
    private static readonly HashSet<string> AllowedOutcomeValues = new() { "success", "failure" };

    private static readonly HashSet<string> AllowedErrorTypeValues = new()
    {
        "Validation", "NotFound", "Conflict", "BusinessRule", "InvalidReference", "Unexpected", "none",
    };

    private static List<IReadOnlyDictionary<string, object?>> CollectCqrsRequestsTags(Action recordMeasurements)
    {
        var capturedTagSets = new List<IReadOnlyDictionary<string, object?>>();
        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, l) =>
        {
            if (instrument.Meter.Name == LabViroMolDiagnostics.Name && instrument.Name == "cqrs.requests")
                l.EnableMeasurementEvents(instrument);
        };
        listener.SetMeasurementEventCallback<long>((_, _, tags, _) =>
        {
            var dictionary = new Dictionary<string, object?>();
            foreach (var tag in tags)
                dictionary[tag.Key] = tag.Value;
            capturedTagSets.Add(dictionary);
        });
        listener.Start();

        recordMeasurements();

        return capturedTagSets;
    }

    [Fact]
    public void RecordSuccess_EmitsOnlyAllowedTagKeys()
    {
        var metrics = new LabViroMolMetrics();

        var tagSets = CollectCqrsRequestsTags(() => metrics.RecordSuccess("SomeCommand"));

        var tagSet = Assert.Single(tagSets);
        Assert.Equal(new HashSet<string> { "request", "outcome", "error_type" }, tagSet.Keys.ToHashSet());
    }

    [Fact]
    public void RecordSuccess_EmitsOutcomeFromAllowedSet()
    {
        var metrics = new LabViroMolMetrics();

        var tagSets = CollectCqrsRequestsTags(() => metrics.RecordSuccess("SomeCommand"));

        var outcome = (string)tagSets.Single()["outcome"]!;
        Assert.Contains(outcome, AllowedOutcomeValues);
    }

    [Theory]
    [InlineData("Validation")]
    [InlineData("NotFound")]
    [InlineData("Conflict")]
    [InlineData("BusinessRule")]
    [InlineData("InvalidReference")]
    [InlineData("Unexpected")]
    public void RecordFailure_EmitsErrorTypeFromAllowedSet(string errorType)
    {
        var metrics = new LabViroMolMetrics();

        var tagSets = CollectCqrsRequestsTags(() => metrics.RecordFailure("SomeCommand", errorType));

        var emittedErrorType = (string)tagSets.Single()["error_type"]!;
        Assert.Contains(emittedErrorType, AllowedErrorTypeValues);
        Assert.Equal("failure", tagSets.Single()["outcome"]);
    }

    [Fact]
    public void RecordSuccess_DoesNotEmitHighCardinalityTags()
    {
        var metrics = new LabViroMolMetrics();
        var forbiddenTagKeys = new[] { "userId", "scheduleId", "orderId", "email", "id" };

        var tagSets = CollectCqrsRequestsTags(() => metrics.RecordSuccess("SomeCommand"));

        var tagSet = tagSets.Single();
        foreach (var forbiddenKey in forbiddenTagKeys)
            Assert.DoesNotContain(forbiddenKey, tagSet.Keys);
    }
}
