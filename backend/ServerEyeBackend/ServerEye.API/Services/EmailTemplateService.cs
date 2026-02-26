namespace ServerEye.API.Services;

using System.Text;
using ServerEye.Core.Interfaces.Services;

public sealed class EmailTemplateService(IWebHostEnvironment environment) : IEmailTemplateService
{
    private readonly IWebHostEnvironment environment = environment;

    public async Task<string> RenderTemplateAsync(string templateName, Dictionary<string, string> parameters)
    {
        var templatePath = Path.Combine(this.environment.ContentRootPath, "Templates", "Email", $"{templateName}.html");

        if (!File.Exists(templatePath))
        {
            throw new FileNotFoundException($"Email template not found: {templateName}");
        }

        var template = await File.ReadAllTextAsync(templatePath, Encoding.UTF8);

        foreach (var parameter in parameters)
        {
            template = template.Replace($"{{{{{parameter.Key}}}}}", parameter.Value, StringComparison.Ordinal);
        }

        return template;
    }
}
