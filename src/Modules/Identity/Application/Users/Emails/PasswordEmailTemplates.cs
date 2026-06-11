namespace LabViroMol.Modules.Identity.Application.Users.Emails;

internal static class PasswordEmailTemplates
{
    public static (string Subject, string Body) BuildWelcomeEmail(string firstName, string resetLink)
    {
        var subject = "Bem-vindo(a) ao LabViroMol - Defina sua senha";
        var body = $$"""
            <div style="font-family: Arial, sans-serif; max-width: 560px; margin: 0 auto; color: #333;">
                <h2 style="color: #2c3e50;">Bem-vindo(a) ao LabViroMol, {{firstName}}!</h2>
                <p>Sua conta foi criada com sucesso. Para começar a utilizar o sistema, defina sua senha de acesso clicando no botão abaixo.</p>
                <p style="text-align: center; margin: 32px 0;">
                    <a href="{{resetLink}}" style="background-color: #2c7be5; color: #ffffff; padding: 12px 28px; border-radius: 6px; text-decoration: none; font-weight: bold;">
                        Definir minha senha
                    </a>
                </p>
                <p>Se o botão não funcionar, copie e cole o link abaixo no seu navegador:</p>
                <p style="word-break: break-all; color: #2c7be5;">{{resetLink}}</p>
                <p>Se você não solicitou a criação desta conta, pode ignorar este email com segurança.</p>
                <hr style="border: none; border-top: 1px solid #e0e0e0; margin: 24px 0;" />
                <p style="font-size: 13px; color: #888;">
                    Atenciosamente,<br />
                    Laboratório de Virologia Molecular - Hospital de Curitiba UFPR
                </p>
            </div>
            """;

        return (subject, body);
    }

    public static (string Subject, string Body) BuildPasswordResetEmail(string firstName, string resetLink)
    {
        var subject = "Redefinição de senha - LabViroMol";
        var greeting = string.IsNullOrWhiteSpace(firstName) ? "Olá," : $"Olá, {firstName}.";
        var body = $$"""
            <div style="font-family: Arial, sans-serif; max-width: 560px; margin: 0 auto; color: #333;">
                <h2 style="color: #2c3e50;">Redefinição de senha</h2>
                <p>{{greeting}}</p>
                <p>Recebemos uma solicitação para redefinir a senha da sua conta no LabViroMol. Clique no botão abaixo para criar uma nova senha.</p>
                <p style="text-align: center; margin: 32px 0;">
                    <a href="{{resetLink}}" style="background-color: #2c7be5; color: #ffffff; padding: 12px 28px; border-radius: 6px; text-decoration: none; font-weight: bold;">
                        Redefinir minha senha
                    </a>
                </p>
                <p>Se o botão não funcionar, copie e cole o link abaixo no seu navegador:</p>
                <p style="word-break: break-all; color: #2c7be5;">{{resetLink}}</p>
                <p>Se você não solicitou esta alteração, ignore este email — sua senha permanecerá inalterada.</p>
                <hr style="border: none; border-top: 1px solid #e0e0e0; margin: 24px 0;" />
                <p style="font-size: 13px; color: #888;">
                    Atenciosamente,<br />
                    Laboratório de Virologia Molecular - Hospital de Curitiba UFPR
                </p>
            </div>
            """;

        return (subject, body);
    }
}
