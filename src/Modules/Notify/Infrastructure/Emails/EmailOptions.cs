namespace LabViroMol.Modules.Notify.Infrastructure.Emails;

public sealed class EmailOptions
{
    public string ApiKey { get; set; } = null!;
    public string SenderName { get; set; } = null!;
    public string SenderEmail { get; set; } = null!;
}