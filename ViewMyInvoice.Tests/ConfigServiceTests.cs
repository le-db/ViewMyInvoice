using ViewMyInvoice.Services;

namespace ViewMyInvoice.Tests;

[TestFixture]
public class ConfigServiceTests
{
    [Test]
    public void Config_ShouldLoadMappingsFromEmbeddedFile()
    {
        var mappings = new ConfigService().Config.Mappings;

        mappings.Should().NotBeNullOrEmpty();
        mappings!.Should().Contain(m => m.FieldKey == "BT-10" && m.GroupKey == "buyer_information");
        mappings.Should().OnlyContain(m => m.FieldQuery.StartsWith("/"));
    }
}
