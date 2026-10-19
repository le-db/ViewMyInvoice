using CommunityToolkit.Mvvm.ComponentModel;

namespace ViewMyInvoice.Models;

public partial class FieldFilter(IReadOnlyList<InvoiceFieldGroup> groups) : ObservableObject
{
    [ObservableProperty]
    string _text = string.Empty;
    [ObservableProperty]
    bool _hasNoMatches;

    partial void OnTextChanged(string value)
    {
        var term = Text?.Trim() ?? string.Empty;
        foreach (var group in groups)
            group.ApplyFilter(term);
        HasNoMatches = !groups.Any(g => g.IsVisible);
    }
}
