using System.Reflection;
using System.Text.RegularExpressions;

namespace LabViroMol.ArchitectureTests;

internal static class SourceCodeInspector
{
    public static string FindSolutionRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "LabViroMol.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("LabViroMol.sln was not found from the test base directory.");
    }

    public static string FindSourceFile(Type type)
    {
        var srcRoot = Path.Combine(FindSolutionRoot(), "src");
        var declarationPattern = $@"\b(?:public|internal)\s+(?:sealed\s+)?(?:partial\s+)?(?:class|record|record struct|interface)\s+{Regex.Escape(type.Name)}\b";
        var candidates = Directory.GetFiles(srcRoot, $"{type.Name}.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        if (candidates.Length == 1)
        {
            return candidates[0];
        }

        var normalizedNamespace = (type.Namespace ?? string.Empty).Replace('.', Path.DirectorySeparatorChar);
        var bestMatch = candidates.FirstOrDefault(path => path.Contains(normalizedNamespace, StringComparison.OrdinalIgnoreCase));

        if (bestMatch is not null)
        {
            return bestMatch;
        }

        var contentMatch = Directory.GetFiles(srcRoot, "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .FirstOrDefault(path => Regex.IsMatch(File.ReadAllText(path), declarationPattern, RegexOptions.Multiline));

        return contentMatch ?? throw new InvalidOperationException($"Source file not found uniquely for type {type.FullName}.");
    }

    public static bool IsDeclaredAsRecord(Type type)
    {
        var file = File.ReadAllText(FindSourceFile(type));
        var pattern = $@"\b(?:public|internal)\s+(?:sealed\s+)?(?:partial\s+)?record(?:\s+struct)?\s+{Regex.Escape(type.Name)}\b";
        return Regex.IsMatch(file, pattern, RegexOptions.Multiline);
    }

    public static bool IsDeclaredAsPublic(Type type)
    {
        var file = File.ReadAllText(FindSourceFile(type));
        return Regex.IsMatch(file, $@"\bpublic\s+(?:sealed\s+)?(?:partial\s+)?(?:class|record|record struct|interface)\s+{Regex.Escape(type.Name)}\b");
    }

    public static bool IsDeclaredAsInternal(Type type)
    {
        var file = File.ReadAllText(FindSourceFile(type));
        return Regex.IsMatch(file, $@"\binternal\s+(?:sealed\s+)?(?:partial\s+)?(?:class|record|record struct|interface)\s+{Regex.Escape(type.Name)}\b");
    }
}
