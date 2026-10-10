using ViewMyInvoice.Services;

namespace ViewMyInvoice.Tests;

[TestFixture]
public class InvoiceSyntaxRegistryTests
{
    [Test]
    public void Detect_ShouldRecognizeCiiSample()
        => InvoiceSyntaxRegistry.Detect(TestSamples.Load("xrechnung-cii.xml"))
            .Should().Be(InvoiceSyntaxRegistry.Cii);

    [Test]
    public void Detect_ShouldRecognizeUblInvoiceSample()
        => InvoiceSyntaxRegistry.Detect(TestSamples.Load("01.10a-INVOICE_ubl.xml"))
            .Should().Be(InvoiceSyntaxRegistry.UblInvoice);

    [Test]
    public void Detect_ShouldIgnorePrefixesAndAcceptDefaultNamespace()
    {
        var doc = TestSamples.Parse($"""<Invoice xmlns="{TestSamples.UblNs}" />""");

        InvoiceSyntaxRegistry.Detect(doc).Should().Be(InvoiceSyntaxRegistry.UblInvoice);
    }

    [Test]
    public void Detect_ShouldRejectUnknownRootAndNameIt()
    {
        var doc = TestSamples.Parse("<root><a/></root>");

        var act = () => InvoiceSyntaxRegistry.Detect(doc);

        act.Should().Throw<UnsupportedDocumentException>().WithMessage("*'root'*");
    }

    [Test]
    public void Detect_ShouldRejectUblCreditNoteForNow()
    {
        var doc = TestSamples.Parse(
            """<CreditNote xmlns="urn:oasis:names:specification:ubl:schema:xsd:CreditNote-2" />""");

        var act = () => InvoiceSyntaxRegistry.Detect(doc);

        act.Should().Throw<UnsupportedDocumentException>().WithMessage("*CreditNote*");
    }

    [Test]
    public void Detect_ShouldRejectInvoiceRootInWrongNamespace()
    {
        var doc = TestSamples.Parse("<Invoice xmlns=\"urn:other\" />");

        var act = () => InvoiceSyntaxRegistry.Detect(doc);

        act.Should().Throw<UnsupportedDocumentException>();
    }
}
