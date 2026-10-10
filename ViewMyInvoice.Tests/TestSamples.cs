using System.Xml;

namespace ViewMyInvoice.Tests;

public static class TestSamples
{
    public const string UblNs = "urn:oasis:names:specification:ubl:schema:xsd:Invoice-2";
    public const string CacNs = "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2";
    public const string CbcNs = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2";
    public const string CiiNs = "urn:un:unece:uncefact:data:standard:CrossIndustryInvoice:100";
    public const string RamNs = "urn:un:unece:uncefact:data:standard:ReusableAggregateBusinessInformationEntity:100";

    public const string MinimalUbl = """
        <ubl:Invoice xmlns:ubl="urn:oasis:names:specification:ubl:schema:xsd:Invoice-2"
                     xmlns:cac="urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2"
                     xmlns:cbc="urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2">
          <cbc:ID>Test Data</cbc:ID>
          <cbc:Note>More Data</cbc:Note>
        </ubl:Invoice>
        """;

    public const string EmptyUbl = """
        <ubl:Invoice xmlns:ubl="urn:oasis:names:specification:ubl:schema:xsd:Invoice-2"
                     xmlns:cac="urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2"
                     xmlns:cbc="urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2" />
        """;

    public const string MinimalCii = """
        <rsm:CrossIndustryInvoice xmlns:rsm="urn:un:unece:uncefact:data:standard:CrossIndustryInvoice:100"
                                  xmlns:ram="urn:un:unece:uncefact:data:standard:ReusableAggregateBusinessInformationEntity:100"
                                  xmlns:udt="urn:un:unece:uncefact:data:standard:UnqualifiedDataType:100" />
        """;

    public static XmlDocument Parse(string xml)
    {
        var doc = new XmlDocument();
        doc.LoadXml(xml);
        return doc;
    }

    public static XmlDocument Load(string sampleFileName)
    {
        var doc = new XmlDocument();
        doc.Load(Path.Combine(AppContext.BaseDirectory, "Samples", sampleFileName));
        return doc;
    }
}
