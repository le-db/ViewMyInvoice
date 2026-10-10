using System.Xml;
using ViewMyInvoice.Services;

namespace ViewMyInvoice.Tests;

[TestFixture]
public class UblMappingTests
{
    static readonly ConfigService Config = new();
    static InvoiceField[] Ubl => Config.GetConfig(InvoiceSyntaxRegistry.UblInvoice).Mappings!;
    static InvoiceField[] Cii => Config.GetConfig(InvoiceSyntaxRegistry.Cii).Mappings!;

    // First mapping with this FieldKey must resolve to this text in 01.10a-INVOICE_ubl.xml.
    static readonly Dictionary<string, string> ExpectedInSample = new()
    {
        ["BT-1"] = "Rechnungsnummer",
        ["BT-2"] = "2016-06-27",
        ["BT-3"] = "380",
        ["BT-5"] = "EUR",
        ["BT-10"] = "04011000-12350-85",
        ["BT-13"] = "Dummywert",
        ["BT-14"] = "Auftragsnummer",
        ["BT-23"] = "urn:fdc:peppol.eu:2017:poacc:billing:01:1.0",
        ["BT-24"] = "urn:cen.eu:en16931:2017#compliant#urn:xeinkauf.de:kosit:xrechnung_3.0",
        ["BT-27"] = "[Seller name]",
        ["BT-28"] = "[Seller trading name]",
        ["BT-30"] = "HRB 123456",
        ["BT-31"] = "DE123456789",
        ["BT-34"] = "seller@email.de",
        ["BT-37"] = "[Seller city]",
        ["BT-40"] = "DE",
        ["BT-41"] = "Berater / Sachbearbeiter",
        ["BT-44"] = "[Buyer name]",
        ["BT-49"] = "buyer@info.de",
        ["BT-52"] = "[Buyer city]",
        ["BT-70"] = "Außenstelle XY",
        ["BT-75"] = "[Deliver to address line 1]",
        ["BT-81"] = "58",
        ["BT-84"] = "DE75512108001245126199",
        ["BT-106"] = "2180",
        ["BT-112"] = "2594.2",
        ["BT-115"] = "2594.2",
        ["BT-118"] = "S",
        ["BT-119"] = "19",
        ["BT-126"] = "1",
        ["BT-129"] = "10",
        ["BT-130"] = "XPP",
        ["BT-131"] = "2180",
        ["BT-146"] = "218",
        ["BT-151"] = "S",
        ["BT-153"] = "Drehstuhl",
        ["BT-155"] = "AL281982",
    };

    [Test]
    public void UblKeysAndGroups_ShouldBeSubsetOfCii_AndOnlyKnownKeysOmitted()
    {
        var ciiPairs = Cii.Select(m => (m.FieldKey, m.GroupKey)).ToHashSet();
        var ublPairs = Ubl.Select(m => (m.FieldKey, m.GroupKey)).ToHashSet();

        ublPairs.Should().BeSubsetOf(ciiPairs);
        ciiPairs.Except(ublPairs).Select(p => p.FieldKey)
            .Should().BeEquivalentTo(new[] { "BT-111", "BT-21", "BT-32" });
    }

    [Test]
    public void UblQueries_ShouldAllBeRootedAtInvoice()
        => Ubl.Should().OnlyContain(m => m.FieldQuery.StartsWith("/ubl:Invoice/"));

    [Test]
    public void SampleInvoice_ShouldResolveKnownFields()
    {
        var service = new XPathService();
        service.RegisterDocument(TestSamples.Load("01.10a-INVOICE_ubl.xml"), "sample.xml");

        foreach (var (key, expected) in ExpectedInSample)
        {
            var node = service.Select(Ubl.First(m => m.FieldKey == key));

            node.Should().NotBeNull(key);
            node!.InnerText.Should().Be(expected, key);
        }
    }

    [Test]
    public void EveryMapping_ShouldBeCreatableAndSelectableOnAnEmptyInvoice()
    {
        var doc = TestSamples.Parse(TestSamples.EmptyUbl);
        var service = new XPathService();
        service.RegisterDocument(doc, "empty.xml");
        var nsm = service.State.nsManager!;

        foreach (var mapping in Ubl)
        {
            service.RegisterElement(mapping).Should().NotBeNull(mapping.FieldQuery);
            doc.SelectSingleNode(mapping.FieldQuery, nsm).Should().NotBeNull(mapping.FieldQuery);
        }
    }

    [Test]
    public void Bt17_ShouldUseOriginatorDocumentReference()
    {
        var doc = TestSamples.Parse("""
            <ubl:Invoice xmlns:ubl="urn:oasis:names:specification:ubl:schema:xsd:Invoice-2"
                         xmlns:cac="urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2"
                         xmlns:cbc="urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2">
              <cac:OriginatorDocumentReference><cbc:ID>LOT-1</cbc:ID></cac:OriginatorDocumentReference>
            </ubl:Invoice>
            """);
        var service = new XPathService();
        service.RegisterDocument(doc, "lot.xml");

        service.Select(Ubl.First(m => m.FieldKey == "BT-17"))!.InnerText.Should().Be("LOT-1");
    }

    [Test]
    public void Bt90_ShouldNotShareAPartyIdentificationWithBt29()
    {
        var doc = TestSamples.Parse("""
            <ubl:Invoice xmlns:ubl="urn:oasis:names:specification:ubl:schema:xsd:Invoice-2"
                         xmlns:cac="urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2"
                         xmlns:cbc="urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2">
              <cac:AccountingSupplierParty><cac:Party>
                <cac:PartyIdentification><cbc:ID>GLN-1</cbc:ID></cac:PartyIdentification>
              </cac:Party></cac:AccountingSupplierParty>
            </ubl:Invoice>
            """);
        var service = new XPathService();
        service.RegisterDocument(doc, "seller.xml");
        var nsm = service.State.nsManager!;

        service.RegisterElement(Ubl.First(m => m.FieldKey == "BT-90")).InnerText = "DE98ZZZ09999999999";

        service.Select(Ubl.First(m => m.FieldKey == "BT-29"))!.InnerText.Should().Be("GLN-1");
        doc.SelectNodes("//cac:PartyIdentification", nsm)!.Count.Should().Be(2);
        doc.SelectNodes("//cac:PartyIdentification/cbc:ID", nsm)!.Count.Should().Be(2);
    }

    [Test]
    public void Mappings_ShouldResolveRegardlessOfDocumentPrefixes()
    {
        var doc = TestSamples.Parse(
            $"""<Invoice xmlns="{TestSamples.UblNs}" xmlns:b="{TestSamples.CbcNs}"><b:ID>X-1</b:ID></Invoice>""");
        var service = new XPathService();
        service.RegisterDocument(doc, "prefixes.xml");

        service.Select(Ubl.First(m => m.FieldKey == "BT-1"))!.InnerText.Should().Be("X-1");
    }
}
