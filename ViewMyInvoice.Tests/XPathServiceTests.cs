using System.Xml;
using ViewMyInvoice.Services;

namespace ViewMyInvoice.Tests;

[TestFixture]
public class XPathServiceTests
{
    readonly XPathService _service = new();
    readonly XmlDocument _testDocument = new();

    [SetUp]
    public void Setup()
    {
        var xmlContent = @"
            <root>
                <element1>Test Data</element1>
                <element2>More Data</element2>
            </root>";
        _testDocument.LoadXml(xmlContent);
    }

    [Test]
    public void RegisterDocument_ShouldSetDocumentAndNamespaceManager()
    {
        string documentName = "test_invoice.xml";

        _service.RegisterDocument(_testDocument, documentName);
        var (doc, docName, nms) = _service.State;

        doc.Should().NotBeNull();
        docName.Should().Be(documentName);
        nms.Should().NotBeNull();
    }

    [Test]
    public void Select_ShouldReturnXmlNodeForValidXPath()
    {
        _service.RegisterDocument(_testDocument, "test.xml");

        var field = new InvoiceField("Key", "/root/element1");
        var result = _service.Select(field);

        result.Should().NotBeNull();
        result!.InnerText.Should().BeEquivalentTo("Test Data");
    }

    [Test]
    public void Select_ShouldUseCachedNode()
    {
        _service.RegisterDocument(_testDocument, "test.xml");

        var field = new InvoiceField("Key", "/root/element1");
        var firstResult = _service.Select(field);
        var secondResult = _service.Select(field);

        firstResult.Should().NotBeNull();
        secondResult.Should().NotBeNull();
        firstResult.Should().BeEquivalentTo(secondResult);
    }

    [Test]
    public void RegisterElement_ShouldRegisterXmlNode()
    {
        _service.RegisterDocument(_testDocument, "test.xml");

        var field = new InvoiceField("Key", "/root/element2");
        var result = _service.RegisterElement(field);

        result.Should().NotBeNull();
        result.InnerText.Should().BeEquivalentTo("More Data");
    }
}
