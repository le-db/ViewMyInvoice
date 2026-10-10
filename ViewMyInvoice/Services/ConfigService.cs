using System.Text.Json;

namespace ViewMyInvoice.Services;

public class ConfigService : IConfigService
{
    const string CONFIG_RESOURCE = "invoice_fields.json";

    static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    readonly Lazy<InvoiceFieldConfig> _config = new(Load);

    public InvoiceFieldConfig Config => _config.Value;

    static InvoiceFieldConfig Load()
    {
        using var stream = typeof(ConfigService).Assembly.GetManifestResourceStream(CONFIG_RESOURCE)
            ?? throw new FileNotFoundException($"ConfigService: embedded resource '{CONFIG_RESOURCE}' not found");
        return JsonSerializer.Deserialize<InvoiceFieldConfig>(stream, JsonOptions)
            ?? throw new InvalidDataException($"ConfigService: '{CONFIG_RESOURCE}' is empty or invalid");
    }
}
