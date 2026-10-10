using System.Xml;

namespace ViewMyInvoice.Services;

public interface IXPathService
{
    (XmlDocument? document, string? documentName, XmlNamespaceManager? nsManager) State { get; }
    InvoiceSyntax? Syntax { get; }
    void RegisterDocument(XmlDocument document, string documentName);
    XmlNode? Select(InvoiceField mapping);
    XmlNode RegisterElement(InvoiceField mapping);
}
