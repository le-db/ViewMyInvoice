using System.Xml;
using ViewMyInvoice.Services;

namespace ViewMyInvoice.Tests;

[TestFixture]
public class XPathServiceCreationTests
{
    const string SupplierParty = "/ubl:Invoice/cac:AccountingSupplierParty/cac:Party";

    static (XPathService service, XmlDocument doc, XmlNamespaceManager nsm) Register(string xml)
    {
        var doc = TestSamples.Parse(xml);
        var service = new XPathService();
        service.RegisterDocument(doc, "t.xml");
        return (service, doc, service.State.nsManager!);
    }

    static InvoiceField Field(string query) => new("BT-x", query);

    [Test]
    public void RegisterElement_ShouldCreateEachElementInItsOwnNamespace()
    {
        var (service, _, _) = Register(TestSamples.EmptyUbl);

        var node = service.RegisterElement(Field($"{SupplierParty}/cbc:EndpointID"));

        node.NamespaceURI.Should().Be(TestSamples.CbcNs);
        node.ParentNode!.NamespaceURI.Should().Be(TestSamples.CacNs);
        node.ParentNode.ParentNode!.NamespaceURI.Should().Be(TestSamples.CacNs);
    }

    [Test]
    public void RegisterElement_ShouldCreateAttributePredicate()
    {
        var (service, doc, nsm) = Register(TestSamples.EmptyUbl);
        var query = $"{SupplierParty}/cac:PartyIdentification/cbc:ID[@schemeID='SEPA']";

        var node = service.RegisterElement(Field(query));
        node.InnerText = "DE98ZZZ09999999999";

        ((XmlElement)node).GetAttribute("schemeID").Should().Be("SEPA");
        doc.SelectSingleNode(query, nsm).Should().BeSameAs(node);
    }

    [Test]
    public void RegisterElement_ShouldCreateChildValuePredicate()
    {
        var (service, doc, nsm) = Register(TestSamples.EmptyUbl);
        var query = $"{SupplierParty}/cac:PartyTaxScheme[cac:TaxScheme/cbc:ID='VAT']/cbc:CompanyID";

        var node = service.RegisterElement(Field(query));

        node.LocalName.Should().Be("CompanyID");
        doc.SelectSingleNode($"{SupplierParty}/cac:PartyTaxScheme/cac:TaxScheme/cbc:ID", nsm)!
            .InnerText.Should().Be("VAT");
        doc.SelectSingleNode(query, nsm).Should().BeSameAs(node);
    }

    [Test]
    public void RegisterElement_ShouldNotClobberSiblingThatDoesNotMatchPredicate()
    {
        var (service, doc, nsm) = Register("""
            <ubl:Invoice xmlns:ubl="urn:oasis:names:specification:ubl:schema:xsd:Invoice-2"
                         xmlns:cac="urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2"
                         xmlns:cbc="urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2">
              <cac:AccountingSupplierParty><cac:Party>
                <cac:PartyTaxScheme>
                  <cbc:CompanyID>123/456/789</cbc:CompanyID>
                  <cac:TaxScheme><cbc:ID>FC</cbc:ID></cac:TaxScheme>
                </cac:PartyTaxScheme>
              </cac:Party></cac:AccountingSupplierParty>
            </ubl:Invoice>
            """);
        var query = $"{SupplierParty}/cac:PartyTaxScheme[cac:TaxScheme/cbc:ID='VAT']/cbc:CompanyID";

        var node = service.RegisterElement(Field(query));
        node.InnerText = "DE123456789";

        doc.SelectNodes($"{SupplierParty}/cac:PartyTaxScheme", nsm)!.Count.Should().Be(2);
        doc.SelectSingleNode($"{SupplierParty}/cac:PartyTaxScheme[cac:TaxScheme/cbc:ID='FC']/cbc:CompanyID", nsm)!
            .InnerText.Should().Be("123/456/789");
    }

    [Test]
    public void RegisterElement_ShouldCreateAttributeForTerminalAttributeStep()
    {
        var (service, doc, nsm) = Register(TestSamples.EmptyUbl);
        var query = "/ubl:Invoice/cac:InvoiceLine/cbc:InvoicedQuantity/@unitCode";

        var node = service.RegisterElement(Field(query));
        node.InnerText = "XPP";

        node.Should().BeOfType<XmlAttribute>();
        doc.SelectSingleNode(query, nsm)!.InnerText.Should().Be("XPP");
        doc.SelectSingleNode("/ubl:Invoice/cac:InvoiceLine/cbc:InvoicedQuantity", nsm).Should().NotBeNull();
    }

    [Test]
    public void RegisterElement_ShouldReuseDocumentPrefixForNamespace()
    {
        var (service, _, _) = Register(
            $"""<Invoice xmlns="{TestSamples.UblNs}" xmlns:b="{TestSamples.CbcNs}" />""");

        var node = service.RegisterElement(Field("/ubl:Invoice/cbc:Note"));

        node.Prefix.Should().Be("b");
        node.NamespaceURI.Should().Be(TestSamples.CbcNs);
    }

    [Test]
    public void RegisterElement_ShouldCreateSiblingWithAttributeOfChildPredicate()
    {
        var (service, doc, nsm) = Register("""
            <ubl:Invoice xmlns:ubl="urn:oasis:names:specification:ubl:schema:xsd:Invoice-2"
                         xmlns:cac="urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2"
                         xmlns:cbc="urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2">
              <cac:AccountingSupplierParty><cac:Party>
                <cac:PartyIdentification><cbc:ID>GLN-1</cbc:ID></cac:PartyIdentification>
              </cac:Party></cac:AccountingSupplierParty>
            </ubl:Invoice>
            """);
        var query = $"{SupplierParty}/cac:PartyIdentification[cbc:ID/@schemeID='SEPA']/cbc:ID";

        var node = service.RegisterElement(Field(query));
        node.InnerText = "DE98ZZZ09999999999";

        doc.SelectNodes($"{SupplierParty}/cac:PartyIdentification", nsm)!.Count.Should().Be(2);
        doc.SelectNodes($"{SupplierParty}/cac:PartyIdentification/cbc:ID", nsm)!.Count
            .Should().Be(2, "each PartyIdentification holds exactly one ID");
        ((XmlElement)node).GetAttribute("schemeID").Should().Be("SEPA");
        doc.SelectSingleNode(query, nsm).Should().BeSameAs(node);
    }

    [Test]
    public void RegisterElement_ShouldRejectUnsupportedPredicate()
    {
        var (service, _, _) = Register(TestSamples.EmptyUbl);

        var act = () => service.RegisterElement(Field("/ubl:Invoice/cac:TaxTotal[2]/cbc:TaxAmount"));

        act.Should().Throw<NotSupportedException>().WithMessage("*[2]*");
    }

    [Test]
    public void RegisterElement_ShouldRejectUnknownPrefix()
    {
        var (service, _, _) = Register(TestSamples.EmptyUbl);

        var act = () => service.RegisterElement(Field("/ubl:Invoice/zzz:Foo"));

        act.Should().Throw<System.Xml.XPath.XPathException>().WithMessage("*zzz*");
    }

    [Test]
    public void RegisterElement_ShouldCreateCiiElementsInRsmAndRamNamespaces()
    {
        var (service, _, _) = Register(TestSamples.MinimalCii);

        var node = service.RegisterElement(Field(
            "/rsm:CrossIndustryInvoice/rsm:SupplyChainTradeTransaction/ram:ApplicableHeaderTradeAgreement/ram:BuyerReference"));

        node.NamespaceURI.Should().Be(TestSamples.RamNs);
        node.ParentNode!.NamespaceURI.Should().Be(TestSamples.RamNs);
        node.ParentNode.ParentNode!.NamespaceURI.Should().Be(TestSamples.CiiNs);
    }
}
