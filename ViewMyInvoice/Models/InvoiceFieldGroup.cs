using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ViewMyInvoice.Models;

public partial class InvoiceFieldGroup(
    string groupName,
    ObservableCollection<InvoiceFieldViewModel> fields,
    IStringLocalizer localizer) : ObservableObject
{
    readonly IStringLocalizer _localizer = localizer;

    public string GroupName { get; } = groupName;
    public ObservableCollection<InvoiceFieldViewModel> Fields { get; } = fields;
    public string DisplayName => Localize(_localizer, GroupName);

    public void RefreshLocalization()
    {
        OnPropertyChanged(nameof(DisplayName));
        foreach (var field in Fields)
            field.RefreshLocalization();
    }

    internal static string Localize(IStringLocalizer localizer, string key)
        => string.IsNullOrEmpty(key) ? key : localizer[key].ToString();
}
