using CommunityToolkit.Mvvm.ComponentModel;

namespace ViewMyInvoice.Models;

public partial class InvoiceFieldViewModel(InvoiceField fieldInfo, string value, IStringLocalizer localizer)
    : ObservableObject
{
    [ObservableProperty]
    InvoiceField _fieldInfo = fieldInfo;
    [ObservableProperty]
    string _value = value;

    public string Label => $"{InvoiceFieldGroup.Localize(localizer, FieldInfo.FieldKey)} ({FieldInfo.FieldKey})";

    public void RefreshLocalization() => OnPropertyChanged(nameof(Label));
}
