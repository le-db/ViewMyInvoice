using ViewMyInvoice.Services;

namespace ViewMyInvoice.Tests;

[TestFixture]
public class ConfigServiceTests
{
    [Test]
    public void GetConfig_Cii_ShouldLoadMappingsFromEmbeddedFile()
    {
        var mappings = new ConfigService().GetConfig(InvoiceSyntaxRegistry.Cii).Mappings;

        mappings.Should().NotBeNullOrEmpty();
        mappings!.Should().Contain(m => m.FieldKey == "BT-10" && m.GroupKey == "buyer_information");
        mappings.Should().OnlyContain(m => m.FieldQuery.StartsWith("/rsm:CrossIndustryInvoice/"));
    }

    [Test]
    public void GetConfig_ShouldCachePerSyntax()
    {
        var service = new ConfigService();

        service.GetConfig(InvoiceSyntaxRegistry.Cii)
            .Should().BeSameAs(service.GetConfig(InvoiceSyntaxRegistry.Cii));
    }

    [Test]
    public void GetConfig_Ubl_ShouldLoadItsOwnMappings()
    {
        var mappings = new ConfigService().GetConfig(InvoiceSyntaxRegistry.UblInvoice).Mappings;

        mappings.Should().NotBeNullOrEmpty();
        mappings!.Should().Contain(m => m.FieldKey == "BT-10" && m.GroupKey == "buyer_information");
    }

    [Test]
    public void GetConfig_ShouldNotMixSyntaxes()
    {
        var service = new ConfigService();

        service.GetConfig(InvoiceSyntaxRegistry.Cii).Should()
            .NotBeSameAs(service.GetConfig(InvoiceSyntaxRegistry.UblInvoice));
        service.GetConfig(InvoiceSyntaxRegistry.UblInvoice).Mappings!
            .Should().NotContain(m => m.FieldQuery.Contains("CrossIndustryInvoice"));
    }

    [Test]
    public void GetConfig_ShouldThrowNamingTheMissingResource()
    {
        var syntax = new InvoiceSyntax("x", "X", "urn:x", "does_not_exist.json");

        var act = () => new ConfigService().GetConfig(syntax);

        act.Should().Throw<FileNotFoundException>().WithMessage("*does_not_exist.json*");
    }
}
