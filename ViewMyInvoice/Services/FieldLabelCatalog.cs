using System.Xml.Linq;

namespace ViewMyInvoice.Services;

public interface IFieldLabelCatalog
{
    /// <summary>
    /// Returns labels of a field by its key in all bundled languages.
    /// </summary>
    /// <param name="key">field key</param>
    /// <returns>field labels of all saved languages</returns>
    IReadOnlyList<string> GetLabels(string key);
}

public class FieldLabelCatalog : IFieldLabelCatalog
{
    const string RESOURCE_PREFIX = "Strings.";
    const string RESOURCE_SUFFIX = ".resw";

    readonly Dictionary<string, List<string>> _labels = new(StringComparer.Ordinal);

    public FieldLabelCatalog()
    {
        var assembly = typeof(FieldLabelCatalog).Assembly;
        var resources = assembly.GetManifestResourceNames()
            .Where(n => n.StartsWith(RESOURCE_PREFIX, StringComparison.Ordinal)
                && n.EndsWith(RESOURCE_SUFFIX, StringComparison.Ordinal));
        foreach (var resource in resources)
        {
            using var stream = assembly.GetManifestResourceStream(resource)!;
            foreach (var data in XDocument.Load(stream).Root?.Elements("data") ?? [])
                Add(data.Attribute("name")?.Value, data.Element("value")?.Value);
        }
    }

    public IReadOnlyList<string> GetLabels(string key)
        => _labels.TryGetValue(key, out var labels) ? labels : [];

    void Add(string? key, string? label)
    {
        if (string.IsNullOrEmpty(key) || string.IsNullOrWhiteSpace(label)) return;

        if (!_labels.TryGetValue(key, out var labels))
            _labels[key] = labels = [];
        if (!labels.Contains(label))
            labels.Add(label);
    }
}
