using System.Xml;

namespace ViewMyInvoice.Services;

public class UnsupportedDocumentException(string rootName, string rootNamespace)
    : Exception($"Unsupported invoice document: root element '{rootName}' in namespace '{rootNamespace}'");

public static class InvoiceSyntaxRegistry
{
    public static readonly InvoiceSyntax Cii = new(
        "cii",
        "CrossIndustryInvoice",
        "urn:un:unece:uncefact:data:standard:CrossIndustryInvoice:100",
        "invoice_fields.cii.json");

    public static readonly InvoiceSyntax UblInvoice = new(
        "ubl-invoice",
        "Invoice",
        "urn:oasis:names:specification:ubl:schema:xsd:Invoice-2",
        "invoice_fields.ubl-invoice.json");

    // Adding a syntax (e.g. UBL CreditNote) = one entry here + one embedded mapping file.
    public static IReadOnlyList<InvoiceSyntax> All { get; } = [Cii, UblInvoice];

    public static InvoiceSyntax Detect(XmlDocument document)
    {
        var root = document.DocumentElement
            ?? throw new UnsupportedDocumentException("", "");
        return All.FirstOrDefault(s => s.RootName == root.LocalName && s.RootNamespace == root.NamespaceURI)
            ?? throw new UnsupportedDocumentException(root.LocalName, root.NamespaceURI);
    }
}
