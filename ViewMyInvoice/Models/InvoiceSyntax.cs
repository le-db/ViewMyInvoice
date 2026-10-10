namespace ViewMyInvoice.Models;

/// <summary>One supported invoice XML syntax and the embedded resource holding its field mappings.</summary>
public partial record InvoiceSyntax(
    string Id,
    string RootName,
    string RootNamespace,
    string MappingResource);
