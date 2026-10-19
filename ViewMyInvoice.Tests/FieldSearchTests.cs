using System.Collections.ObjectModel;
using Microsoft.Extensions.Localization;
using ViewMyInvoice.Services;

namespace ViewMyInvoice.Tests;

[TestFixture]
public class FieldSearchTests
{
    sealed class FakeLocalizer : IStringLocalizer
    {
        public LocalizedString this[string name] => new(name, $"label of {name}");
        public LocalizedString this[string name, params object[] arguments] => this[name];
        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => [];
    }

    static readonly FakeLocalizer Localizer = new();

    static InvoiceFieldViewModel Field(string key, string value, params string[] labels)
        => new(new InvoiceField(key, "/x", "g"), value, Localizer, labels);

    static InvoiceFieldGroup Group(string name, params InvoiceFieldViewModel[] fields)
        => new(name, new ObservableCollection<InvoiceFieldViewModel>(fields), Localizer);

    [Test]
    public void Catalog_ShouldReturnEnglishAndGermanLabels()
    {
        var catalog = new FieldLabelCatalog();

        catalog.GetLabels("BT-10").Should().BeEquivalentTo(["Buyer reference", "Leitweg-ID"]);
        catalog.GetLabels("does-not-exist").Should().BeEmpty();
    }

    [TestCase("")]
    [TestCase("4711")]
    [TestCase("buyer REF")]
    [TestCase("leitweg")]
    [TestCase("bt-10")]
    public void Matches_ShouldFindValueKeyOrAnyLanguageLabel(string term)
        => Field("BT-10", "ID-4711", "Buyer reference", "Leitweg-ID").Matches(term).Should().BeTrue();

    [Test]
    public void Matches_ShouldRejectUnrelatedTerm()
        => Field("BT-10", "ID-4711", "Buyer reference", "Leitweg-ID").Matches("Rechnung").Should().BeFalse();

    [Test]
    public void Filter_ShouldHideNonMatchingFieldsAndEmptyGroups()
    {
        var buyerRef = Field("BT-10", "4711", "Buyer reference", "Leitweg-ID");
        var buyerName = Field("BT-44", "ACME", "Buyer name", "Name");
        var total = Field("BT-112", "100.00", "Invoice total amount with VAT", "Gesamtbetrag");
        var buyer = Group("buyer", buyerRef, buyerName);
        var totals = Group("totals", total);
        var filter = new FieldFilter([buyer, totals])
        {
            Text = "acme"
        };

        buyerName.IsVisible.Should().BeTrue();
        buyerRef.IsVisible.Should().BeFalse();
        buyer.IsVisible.Should().BeTrue();
        totals.IsVisible.Should().BeFalse();
        filter.HasNoMatches.Should().BeFalse();

        filter.Text = "nothing matches this";
        filter.HasNoMatches.Should().BeTrue();

        filter.Text = "  ";
        new[] { buyerRef, buyerName, total }.Should().OnlyContain(f => f.IsVisible);
        new[] { buyer, totals }.Should().OnlyContain(g => g.IsVisible);
        filter.HasNoMatches.Should().BeFalse();
    }
}
