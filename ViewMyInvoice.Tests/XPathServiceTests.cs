using System.Xml;
using ViewMyInvoice.Services;

namespace ViewMyInvoice.Tests;

[TestFixture]
public class XPathServiceTests
{
    const string CiiIdQuery = "/rsm:CrossIndustryInvoice/rsm:ExchangedDocument/ram:ID";

    readonly XPathService _service = new();
    readonly XmlDocument _testDocument = new();

    [SetUp]
    public void Setup() => _testDocument.LoadXml(TestSamples.MinimalUbl);

    [Test]
    public void RegisterDocument_ShouldSetDocumentNamespaceManagerAndSyntax()
    {
        string documentName = "test_invoice.xml";

        _service.RegisterDocument(_testDocument, documentName);
        var (doc, docName, nms) = _service.State;

        doc.Should().NotBeNull();
        docName.Should().Be(documentName);
        nms.Should().NotBeNull();
        _service.Syntax.Should().Be(InvoiceSyntaxRegistry.UblInvoice);
    }

    [Test]
    public void RegisterDocument_ShouldRejectUnsupportedRootAndKeepPreviousDocument()
    {
        _service.RegisterDocument(_testDocument, "first.xml");

        var act = () => _service.RegisterDocument(TestSamples.Parse("<root/>"), "second.xml");

        act.Should().Throw<UnsupportedDocumentException>();
        _service.State.documentName.Should().Be("first.xml");
        _service.Syntax.Should().Be(InvoiceSyntaxRegistry.UblInvoice);
    }

    [Test]
    public void RegisterDocument_ShouldSwitchSyntaxAndClearNodeCache()
    {
        _service.RegisterDocument(TestSamples.Load("xrechnung-cii.xml"), "cii.xml");
        var ciiField = new InvoiceField("BT-1", CiiIdQuery);
        _service.Syntax.Should().Be(InvoiceSyntaxRegistry.Cii);
        _service.Select(ciiField).Should().NotBeNull();

        _service.RegisterDocument(_testDocument, "ubl.xml");

        _service.Syntax.Should().Be(InvoiceSyntaxRegistry.UblInvoice);
        _service.Select(ciiField).Should().BeNull("the CII node must not survive in the cache");
    }

    [Test]
    public void Select_ShouldReturnXmlNodeForValidXPath()
    {
        _service.RegisterDocument(_testDocument, "test.xml");

        var field = new InvoiceField("Key", "/ubl:Invoice/cbc:ID");
        var result = _service.Select(field);

        result.Should().NotBeNull();
        result!.InnerText.Should().BeEquivalentTo("Test Data");
    }

    [Test]
    public void Select_ShouldUseCachedNode()
    {
        _service.RegisterDocument(_testDocument, "test.xml");

        var field = new InvoiceField("Key", "/ubl:Invoice/cbc:ID");
        var firstResult = _service.Select(field);
        var secondResult = _service.Select(field);

        firstResult.Should().NotBeNull();
        secondResult.Should().BeSameAs(firstResult);
    }

    [Test]
    public void RegisterElement_ShouldRegisterXmlNode()
    {
        _service.RegisterDocument(_testDocument, "test.xml");

        var field = new InvoiceField("Key", "/ubl:Invoice/cbc:Note");
        var result = _service.RegisterElement(field);

        result.Should().NotBeNull();
        result.InnerText.Should().BeEquivalentTo("More Data");
    }
}
