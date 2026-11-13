namespace ViewMyInvoice.Models;

// e.g. CII, UBL
public partial record InvoiceSyntax(
    string Id,
    string RootName,
    string RootNamespace,
    string MappingResource);
