using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace LabViroMol.ArchitectureTests;

public sealed class ModuleIsolationTests
{
    private static readonly string[] IsolatedModules = ArchitectureModel.IsolatedBusinessModules;

    [Fact]
    public void Modules_should_only_cross_reference_other_modules_through_contracts()
    {
        foreach (var module in IsolatedModules)
        {
            var source = Types().That().HaveFullNameContaining($"LabViroMol.Modules.{module}.");

            foreach (var target in IsolatedModules.Where(other => other != module))
            {
                var forbidden = Types().That()
                    .HaveFullNameContaining($"LabViroMol.Modules.{target}.Domain.")
                    .Or().HaveFullNameContaining($"LabViroMol.Modules.{target}.Application.")
                    .Or().HaveFullNameContaining($"LabViroMol.Modules.{target}.Infrastructure.")
                    .Or().HaveFullNameContaining($"LabViroMol.Modules.{target}.Presentation.");

                source.Should().NotDependOnAny(forbidden).Check(ArchitectureModel.Architecture);
            }
        }
    }

    [Fact]
    public void Modules_should_be_free_of_cycles()
    {
        var modules = IsolatedModules.ToDictionary(
            module => module,
            module => TestTypeCatalog.ModuleTypes(module)
                .SelectMany(type => type.Assembly.GetReferencedAssemblies())
                .Select(reference => reference.Name)
                .Where(name => name is not null)
                .Where(name => !name!.Contains(".Contracts", StringComparison.Ordinal))
                .Where(name => IsolatedModules.Any(other => name!.Contains($".{other}.", StringComparison.Ordinal)))
                .Select(name => IsolatedModules.Single(other => name!.Contains($".{other}.", StringComparison.Ordinal)))
                .Where(target => target != module)
                .Distinct()
                .ToArray());

        var visited = new HashSet<string>(StringComparer.Ordinal);
        var stack = new HashSet<string>(StringComparer.Ordinal);

        static bool HasCycle(
            string module,
            IReadOnlyDictionary<string, string[]> graph,
            ISet<string> visited,
            ISet<string> stack)
        {
            if (!visited.Add(module))
            {
                return stack.Contains(module);
            }

            stack.Add(module);

            foreach (var dependency in graph[module])
            {
                if (HasCycle(dependency, graph, visited, stack))
                {
                    return true;
                }
            }

            stack.Remove(module);
            return false;
        }

        var hasCycle = modules.Keys.Any(module => HasCycle(module, modules, visited, stack));
        Assert.False(hasCycle, "Cross-module assembly references should be free of cycles.");
    }

    [Fact]
    public void Feature_slices_within_a_module_should_be_free_of_cycles()
    {
        foreach (var module in IsolatedModules)
        {
            var graph = BuildFeatureDependencyGraph(module);
            var cycle = FindCycle(graph);

            Assert.True(
                cycle is null,
                cycle is null
                    ? string.Empty
                    : $"{module} feature slices contain a cycle: {string.Join(" -> ", cycle)}");
        }
    }

    private static Dictionary<string, string[]> BuildFeatureDependencyGraph(string module)
    {
        var applicationRoot = Path.Combine(SourceCodeInspector.FindSolutionRoot(), "src", "Modules", module, "Application");
        var prefix = $"LabViroMol.Modules.{module}.Application.";
        var files = Directory.GetFiles(applicationRoot, "*.cs", SearchOption.AllDirectories);
        var graph = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);

        foreach (var file in files)
        {
            var relativePath = Path.GetRelativePath(applicationRoot, file);
            var segments = relativePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            if (segments.Length < 2 || segments[0].Equals("Shared", StringComparison.Ordinal))
            {
                continue;
            }

            var feature = segments[0];
            graph.TryAdd(feature, []);

            var content = File.ReadAllText(file);

            foreach (var dependency in graph.Keys
                         .Concat(files
                             .Select(path => Path.GetRelativePath(applicationRoot, path))
                             .Select(path => path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
                             .Where(parts => parts.Length >= 2 && !parts[0].Equals("Shared", StringComparison.Ordinal))
                             .Select(parts => parts[0]))
                         .Distinct(StringComparer.Ordinal))
            {
                if (dependency == feature)
                {
                    continue;
                }

                if (content.Contains($"{prefix}{dependency}.", StringComparison.Ordinal))
                {
                    graph[feature].Add(dependency);
                }
            }
        }

        return graph.ToDictionary(pair => pair.Key, pair => pair.Value.ToArray(), StringComparer.Ordinal);
    }

    private static string[]? FindCycle(IReadOnlyDictionary<string, string[]> graph)
    {
        var visited = new HashSet<string>(StringComparer.Ordinal);
        var stack = new List<string>();
        var inStack = new HashSet<string>(StringComparer.Ordinal);

        foreach (var node in graph.Keys)
        {
            var cycle = FindCycle(node, graph, visited, stack, inStack);
            if (cycle is not null)
            {
                return cycle;
            }
        }

        return null;
    }

    private static string[]? FindCycle(
        string node,
        IReadOnlyDictionary<string, string[]> graph,
        ISet<string> visited,
        IList<string> stack,
        ISet<string> inStack)
    {
        if (!visited.Add(node))
        {
            if (!inStack.Contains(node))
            {
                return null;
            }

            var index = stack.IndexOf(node);
            return stack.Skip(index).Concat([node]).ToArray();
        }

        stack.Add(node);
        inStack.Add(node);

        foreach (var dependency in graph[node])
        {
            var cycle = FindCycle(dependency, graph, visited, stack, inStack);
            if (cycle is not null)
            {
                return cycle;
            }
        }

        stack.RemoveAt(stack.Count - 1);
        inStack.Remove(node);
        return null;
    }
}
