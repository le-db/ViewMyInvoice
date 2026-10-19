using CommunityToolkit.Mvvm.ComponentModel;

namespace ViewMyInvoice.Models;

public partial class InvoiceFieldViewModel(
    InvoiceField fieldInfo,
    string value,
    IStringLocalizer localizer,
    IReadOnlyList<string>? _allLanguageLabels = null)
    : ObservableObject
{
    readonly IReadOnlyList<string> _allLanguageLabels = _allLanguageLabels ?? [];

    [ObservableProperty]
    InvoiceField _fieldInfo = fieldInfo;
    [ObservableProperty]
    string _value = value;
    [ObservableProperty]
    bool _isVisible = true;

    public string Label => $"{InvoiceFieldGroup.Localize(localizer, FieldInfo.FieldKey)} ({FieldInfo.FieldKey})";

    public void RefreshLocalization() => OnPropertyChanged(nameof(Label));

    /// <summary>
    /// Whether a filter string matches BT key, field name or field value
    /// </summary>
    /// <param name="filter">filter string</param>
    /// <returns>true if filter is null or empty, otherwise whether BT key, field name or field value contain filter</returns>
    public bool Matches(string filter)
    {
        if (string.IsNullOrEmpty(filter))
            return true;
        return Contains(Value, filter)
            || Contains(FieldInfo.FieldKey, filter)
            || Contains(Label, filter)
            || _allLanguageLabels.Any(label => Contains(label, filter));
    }

    static bool Contains(string? text, string term)
        => text?.Contains(term, StringComparison.OrdinalIgnoreCase) == true;
}
