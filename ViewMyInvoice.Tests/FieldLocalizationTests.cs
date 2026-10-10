using System.Collections.ObjectModel;
using Microsoft.Extensions.Localization;

namespace ViewMyInvoice.Tests;

[TestFixture]
public class FieldLocalizationTests
{
    sealed class FakeLocalizer : IStringLocalizer
    {
        public string Prefix { get; set; } = "en";
        public LocalizedString this[string name] => new(name, $"{Prefix}:{name}");
        public LocalizedString this[string name, params object[] arguments] => this[name];
        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => [];
    }

    [Test]
    public void RefreshLocalization_ShouldRaiseChangesAndReturnNewTexts()
    {
        var localizer = new FakeLocalizer();
        var field = new InvoiceFieldViewModel(new InvoiceField("BT-10", "/x", "g"), "v", localizer);
        var group = new InvoiceFieldGroup("g", new ObservableCollection<InvoiceFieldViewModel> { field }, localizer);
        var raised = new List<string?>();
        group.PropertyChanged += (_, e) => raised.Add(e.PropertyName);
        field.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        group.DisplayName.Should().Be("en:g");
        field.Label.Should().Be("en:BT-10 (BT-10)");

        localizer.Prefix = "de";
        group.RefreshLocalization();

        raised.Should().Contain([nameof(InvoiceFieldGroup.DisplayName), nameof(InvoiceFieldViewModel.Label)]);
        group.DisplayName.Should().Be("de:g");
        field.Label.Should().Be("de:BT-10 (BT-10)");
    }
}
