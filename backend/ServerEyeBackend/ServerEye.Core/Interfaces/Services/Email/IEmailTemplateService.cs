namespace ServerEye.Core.Interfaces.Services;

public interface IEmailTemplateService
{
    public Task<string> RenderTemplateAsync(string templateName, Dictionary<string, string> parameters);
}
