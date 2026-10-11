using System.Collections.Concurrent;
using System.Text.Json;

namespace ViewMyInvoice.Services;

public class ConfigService : IConfigService
{
    readonly ConcurrentDictionary<string, InvoiceFieldConfig> _configs = new();

    public InvoiceFieldConfig GetConfig(InvoiceSyntax syntax)
        => _configs.GetOrAdd(syntax.Id, _ => Load(syntax));

    static InvoiceFieldConfig Load(InvoiceSyntax syntax)
    {
        var resource = syntax.MappingResource;
        using var stream = typeof(ConfigService).Assembly.GetManifestResourceStream(resource)
            ?? throw new FileNotFoundException($"ConfigService: embedded resource '{resource}' not found");
        return JsonSerializer.Deserialize(stream, InvoiceFieldJsonContext.Default.InvoiceFieldConfig)
            ?? throw new InvalidDataException($"ConfigService: '{resource}' is empty or invalid");
    }
}
