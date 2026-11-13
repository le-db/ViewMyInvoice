using System.Text.Json;
using System.Text.Json.Serialization;

namespace ViewMyInvoice.Services;

// workaround: reflection-based System.Text.Json fails in trimmed builds (e.g. WASM)
[JsonSourceGenerationOptions(
    PropertyNameCaseInsensitive = true,
    ReadCommentHandling = JsonCommentHandling.Skip,
    AllowTrailingCommas = true)]
[JsonSerializable(typeof(InvoiceFieldConfig))]
internal partial class InvoiceFieldJsonContext : JsonSerializerContext;
