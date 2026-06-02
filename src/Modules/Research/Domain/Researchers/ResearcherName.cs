using LabViroMol.Modules.Shared.Kernel.Primitives;

namespace LabViroMol.Modules.Research.Domain.Researchers;
public record ResearcherName
{
    public string FirstName { get; init; }
    public string LastName { get; init; }
    public string? CitationName { get; init; }
    public string? DisplayName { get; init; }

    public ResearcherName(string firstName, string lastName, string? citationName, string? displayName)
    {
        Guard.AgainstEmpty(firstName, "O primeiro nome é obrigatório.");
        Guard.AgainstEmpty(lastName, "O sobrenome é obrigatório.");
        
        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        CitationName = citationName?.Trim();
        DisplayName = displayName?.Trim();
    }
    
    public string FullName => $"{FirstName} {LastName}";

    public string PublicDisplayName 
    {
        get 
        {
            if (!string.IsNullOrWhiteSpace(DisplayName))
                return DisplayName;

            var parts = LastName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var lastWord = parts.Length > 0 ? parts[^1] : string.Empty;

            return $"{FirstName} {lastWord}".Trim();
        }
    }

    public string PublicCitationName 
    {
        get 
        {
            if (!string.IsNullOrWhiteSpace(CitationName))
                return CitationName;

            var parts = LastName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var lastWord = parts.Length > 0 ? parts[^1] : LastName;

            return $"{lastWord.ToUpper()}, {FirstName[0]}."; 
        }
    }
}