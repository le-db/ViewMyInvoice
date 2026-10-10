namespace ViewMyInvoice.Services;

public interface IConfigService
{
    InvoiceFieldConfig GetConfig(InvoiceSyntax syntax);
}
