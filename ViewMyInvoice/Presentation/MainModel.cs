using System.Xml;

namespace ViewMyInvoice.Presentation;

public partial record MainModel
{
    const string OPEN_LABEL_KEY = "OpenDocumentButton.Content";

    readonly IOptions<AppConfig> _appInfo;
    readonly INavigator _navigator;
    readonly IStringLocalizer _localizer;
    readonly IDocumentService _documentService;
    readonly IXPathService _xPathService;

    public MainModel(
        IOptions<AppConfig> appInfo,
        INavigator navigator,
        IStringLocalizer localizer,
        IDocumentService documentService,
        IXPathService xPathService,
        ILanguageNotifier languageNotifier)
    {
        _appInfo = appInfo;
        _navigator = navigator;
        _localizer = localizer;
        _documentService = documentService;
        _xPathService = xPathService;
        languageNotifier.LanguageChanged += async () => await RefreshLocalization();
    }

    public IState<string> Title => State<string>.Value(this, CreateTitle);
    public IState<string> OpenLabel => State<string>.Value(this, () => _localizer[OPEN_LABEL_KEY].ToString());

    public async Task OpenDocument()
    {
        var (doc, docName) = await _documentService.OpenDocument()
            ?? throw new NullReferenceException("MainModel.GoToViewer: received null document from DocumentService");
        await GoToViewer(doc, docName);
    }

    string CreateTitle() => $"{_localizer["ApplicationName"]} - {_appInfo?.Value?.Environment}";

    async Task RefreshLocalization()
    {
        await Title.UpdateAsync(_ => CreateTitle(), CancellationToken.None);
        await OpenLabel.UpdateAsync(_ => _localizer[OPEN_LABEL_KEY].ToString(), CancellationToken.None);
    }

    async Task GoToViewer(XmlDocument doc, string docName)
    {
        _xPathService.RegisterDocument(doc, docName);
        await _navigator.NavigateViewModelAsync<InvoiceViewModel>(this, data: doc);
    }
}
