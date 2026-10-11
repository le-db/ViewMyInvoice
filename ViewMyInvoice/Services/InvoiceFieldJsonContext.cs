using System.Text.Json;
using System.Text.Json.Serialization;

namespace ViewMyInvoice.Services;

// Source-generated metadata: reflection-based System.Text.Json fails in trimmed (Release WASM) builds.
[JsonSourceGenerationOptions(
    PropertyNameCaseInsensitive = true,
    ReadCommentHandling = JsonCommentHandling.Skip,
    AllowTrailingCommas = true)]
[JsonSerializable(typeof(InvoiceFieldConfig))]
internal partial class InvoiceFieldJsonContext : JsonSerializerContext;
