namespace LabViroMol.Modules.Notify.Infrastructure.Emails;

public sealed class BrevoEmailRequest
{
    public BrevoContact Sender { get; set; } = null!;
    public List<BrevoContact> To { get; set; } = null!;
    public string Subject { get; set; } = null!;
    public string HtmlContent { get; set; } = null!;
}

public sealed class BrevoContact
{
    public string Email { get; set; } = null!;
    public string? Name { get; set; }
}
