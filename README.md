## ViewMyInvoice

A cross-platform application for viewing and editing EN16931-compliant CII and UBL (Invoice) invoices. Supports loading XML files, searching via XPath queries defined in a (global) configuration file, and displaying structured invoice data. Meant for quick referencing of BT-fields, NOT for producing valid invoices.

Built with [Uno Platform](https://github.com/unoplatform/uno).

### Notes

- UBL `CreditNote` is not supported yet; the syntax (CII or UBL Invoice) is detected from the document's root element
- Core functionality relies on XML parsing and XPath navigation
- After saving an edited invoice, it may not be valid anymore
	- e.g. Schematron validation for CIUS XRechnung will fail with: Document MUST not contain empty elements.
