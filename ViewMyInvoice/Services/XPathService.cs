using System.Text.RegularExpressions;
using System.Xml;
using Uno.Extensions.Specialized;

namespace ViewMyInvoice.Services;

public class XPathService : IXPathService
{
    readonly Dictionary<InvoiceField, XmlNode> _xPathCache = [];

    readonly Dictionary<string, string> _namespaces = new()
    {
        { "rsm", "urn:un:unece:uncefact:data:standard:CrossIndustryInvoice:100" },
        { "ram", "urn:un:unece:uncefact:data:standard:ReusableAggregateBusinessInformationEntity:100" },
        { "qdt", "urn:un:unece:uncefact:data:standard:QualifiedDataType:100" },
        { "udt", "urn:un:unece:uncefact:data:standard:UnqualifiedDataType:100" },
        { "cbc", "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2" },
        { "cac", "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2" },
        { "ext", "urn:oasis:names:specification:ubl:schema:xsd:CommonExtensionComponents-2" },
        { "fx", "urn:factur-x:pdfa:CrossIndustryDocument:invoice:1p0#" },
        { "ubl", "urn:oasis:names:specification:ubl:schema:xsd:Invoice-2" },
        { "xs", "http://www.w3.org/2001/XMLSchema" },
    };
    XmlNamespaceManager? _namespaceManager;

    XmlDocument? _document;
    string? _documentName;
    InvoiceSyntax? _syntax;

    public (XmlDocument? document, string? documentName, XmlNamespaceManager? nsManager) State
        => (_document, _documentName, _namespaceManager);

    public InvoiceSyntax? Syntax => _syntax;

    public void RegisterDocument(XmlDocument document, string documentName = "invoice.xml")
    {
        // Detect first: an unsupported document must not disturb the currently registered one.
        var syntax = InvoiceSyntaxRegistry.Detect(document);

        _xPathCache.Clear();
        _document = document;
        _documentName = documentName;
        _syntax = syntax;
        _namespaceManager = GetNamespaceManager(document);
    }

    public XmlNode? Select(InvoiceField mapping)
    {
        if (mapping == null || _document == null || _namespaceManager == null)
            throw new ArgumentNullException(nameof(mapping), "XPathService.Select: arguments or document are null");

        var query = mapping.FieldQuery;
        var root = _document.DocumentElement;
        if (string.IsNullOrEmpty(query) || root == null)
            throw new NullReferenceException("XPathService.Select: query is empty or null, or the document root is null");

        if (_xPathCache.TryGetValue(mapping, out var node))
            return node;
        node = root.SelectSingleNode(query, _namespaceManager);
        if (node != null) _xPathCache.Add(mapping, node);
        return node;
    }

    public XmlNode RegisterElement(InvoiceField mapping)
    {
        if (_document == null || _namespaceManager == null)
            throw new NullReferenceException("XPathService.RegisterElement: no document loaded");

        var node = GetOrCreateNodeAtXPath(_document, mapping.FieldQuery)
            ?? throw new NullReferenceException($"XPathService.RegisterElement: query doesn't match");

        _xPathCache[mapping] = node;
        return node;
    }

    // [@attr='v'] or [child/path='v']; anything else is rejected when creating elements.
    static readonly Regex PredicatePattern =
        new(@"^(?<path>@[\w:]+|[\w:]+(?:/[\w:]+)*(?:/@[\w:]+)?)='(?<value>[^']*)'$");

    XmlNode? GetOrCreateNodeAtXPath(XmlDocument doc, string query)
    {
        var node = doc.SelectSingleNode(query, _namespaceManager!);
        if (node != null)
            return node;

        var steps = SplitSteps(query);
        if (steps.Length < 2)
            return null;

        var parent = GetOrCreateNodeAtXPath(doc, "/" + string.Join('/', steps[..^1]));
        if (parent == null)
            return null;

        // Creating a predicated parent may already have created the wanted child (e.g. [cbc:ID/@schemeID='x']/cbc:ID).
        return doc.SelectSingleNode(query, _namespaceManager!) ?? CreateChild(doc, parent, steps[^1]);
    }

    XmlNode CreateChild(XmlDocument doc, XmlNode parent, string step)
    {
        if (parent is not XmlElement parentElement)
            throw new InvalidOperationException($"XPathService: cannot create '{step}' below a non-element node");

        if (step.StartsWith('@'))
        {
            var attribute = CreateAttribute(doc, parentElement, step[1..]);
            parentElement.SetAttributeNode(attribute);
            return attribute;
        }

        var bracket = step.IndexOf('[');
        var name = bracket < 0 ? step : step[..bracket];
        var predicate = bracket < 0 ? null : step[(bracket + 1)..^1];

        var element = CreateElement(doc, parentElement, name);
        if (predicate != null)
            ApplyPredicate(doc, element, predicate);
        parentElement.AppendChild(element);
        return element;
    }

    void ApplyPredicate(XmlDocument doc, XmlElement element, string predicate)
    {
        var match = PredicatePattern.Match(predicate);
        if (!match.Success)
            throw new NotSupportedException(
                $"XPathService: predicate '[{predicate}]' is not supported when creating elements");

        var value = match.Groups["value"].Value;
        var path = match.Groups["path"].Value.Split('/');

        if (path[0].StartsWith('@'))
        {
            var attribute = CreateAttribute(doc, element, path[0][1..]);
            attribute.Value = value;
            element.SetAttributeNode(attribute);
            return;
        }

        var current = element;
        foreach (var segment in path)
        {
            if (segment.StartsWith('@'))
            {
                var attribute = CreateAttribute(doc, current, segment[1..]);
                attribute.Value = value;
                current.SetAttributeNode(attribute);
                return;
            }

            var child = CreateElement(doc, current, segment);
            current.AppendChild(child);
            current = child;
        }
        current.InnerText = value;
    }

    XmlElement CreateElement(XmlDocument doc, XmlElement parent, string qualifiedName)
    {
        var (queryPrefix, localName) = SplitQualifiedName(qualifiedName);
        if (queryPrefix.Length == 0)
            return doc.CreateElement(localName, parent.NamespaceURI);

        var ns = LookupNamespace(queryPrefix);
        return doc.CreateElement(PrefixFor(parent, ns, queryPrefix), localName, ns);
    }

    XmlAttribute CreateAttribute(XmlDocument doc, XmlElement parent, string qualifiedName)
    {
        var (queryPrefix, localName) = SplitQualifiedName(qualifiedName);
        if (queryPrefix.Length == 0)
            return doc.CreateAttribute(localName);

        var ns = LookupNamespace(queryPrefix);
        return doc.CreateAttribute(PrefixFor(parent, ns, queryPrefix), localName, ns);
    }

    string LookupNamespace(string prefix)
        => _namespaceManager!.LookupNamespace(prefix)
            ?? throw new InvalidOperationException($"XPathService: unknown namespace prefix '{prefix}'");

    // Reuse whatever prefix the document already uses for this namespace; fall back to the query's prefix.
    static string PrefixFor(XmlElement context, string ns, string queryPrefix)
    {
        var existing = context.GetPrefixOfNamespace(ns);
        return existing.Length > 0 ? existing : queryPrefix;
    }

    static (string prefix, string localName) SplitQualifiedName(string qualifiedName)
    {
        var colon = qualifiedName.IndexOf(':');
        return colon < 0 ? ("", qualifiedName) : (qualifiedName[..colon], qualifiedName[(colon + 1)..]);
    }

    // Splits an absolute location path on '/' that is not inside [...] (predicates may contain '/').
    static string[] SplitSteps(string query)
    {
        var steps = new List<string>();
        var depth = 0;
        var start = 0;
        for (var i = 0; i < query.Length; i++)
        {
            switch (query[i])
            {
                case '[':
                    depth++;
                    break;
                case ']':
                    depth--;
                    break;
                case '/' when depth == 0:
                    if (i > start)
                        steps.Add(query[start..i]);
                    start = i + 1;
                    break;
            }
        }
        if (start < query.Length)
            steps.Add(query[start..]);
        return [.. steps];
    }

    XmlNamespaceManager GetNamespaceManager(XmlDocument document)
    {
        var ns = new XmlNamespaceManager(document.NameTable);
        _namespaces.ForEach((i, kv) => ns.AddNamespace(kv.Key, kv.Value));
        return ns;
    }
}
