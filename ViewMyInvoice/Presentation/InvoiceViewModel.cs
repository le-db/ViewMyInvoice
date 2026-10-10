using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using Uno.Extensions.Specialized;

namespace ViewMyInvoice.Presentation;

public partial record InvoiceViewModel
{
    readonly ILogger<InvoiceViewModel> _logger;
    readonly ILocalizationService _localizationService;
    readonly IConfigService _configService;
    readonly IStringLocalizer _localizer;
    readonly IXPathService _xPathService;
    readonly IDocumentService _documentService;
    readonly ILanguageNotifier _languageNotifier;

    public ObservableCollection<InvoiceFieldGroup> GroupedFields { get; init; }
    public IState<CultureInfo> CurrentCulture => State<CultureInfo>
        .Value(this, () => _localizationService.CurrentCulture);
    public IState<string> SaveLabel => State<string>
        .Value(this, () => _localizer["SaveDocumentButton.Content"].ToString());
    public IState<string> ViewerTitle => State<string>
        .Value(this, () => _localizer["ViewerNavigationBar.Content"].ToString());

    public InvoiceViewModel(
        ILogger<InvoiceViewModel> logger,
        ILocalizationService localizationService,
        IConfigService configService,
        IStringLocalizer localizer,
        IXPathService xPathService,
        IDocumentService documentService,
        ILanguageNotifier languageNotifier)
    {
        _languageNotifier = languageNotifier;
        _logger = logger;
        _localizationService = localizationService;
        _configService = configService;
        _localizer = localizer;
        _xPathService = xPathService;
        _documentService = documentService;

        var mappings = _configService.Config.Mappings
            ?? throw new NullReferenceException("InvoiceViewModel: encountered misconfigured invoice field mappings");
        GroupedFields = [ ..CreateFieldViews(mappings) ];
        RegisterFieldPropertyCallbacks();
    }

    public async Task ToggleLocalization(CancellationToken ct)
    {
        // Read the service, not the CurrentCulture state: the state is only a display copy.
        var currentCulture = _localizationService.CurrentCulture;
        var nextCulture = _localizationService.SupportedCultures
            .First(culture => culture.Name != currentCulture.Name);
        await _localizationService.SetCurrentCultureAsync(nextCulture);
        await CurrentCulture.UpdateAsync(_ => nextCulture, ct);
        await SaveLabel.Set(_localizer["SaveDocumentButton.Content"].ToString(), ct);
        await ViewerTitle.Set(_localizer["ViewerNavigationBar.Content"].ToString(), ct);
        _languageNotifier.NotifyLanguageChanged();
        foreach (var group in GroupedFields)
            group.RefreshLocalization();
    }

    public async Task SaveDocument()
    {
        var (doc, docName, _) = _xPathService.State;
        if (doc == null)
            throw new NullReferenceException("InvoiceViewModel.SaveDocument: received null document from IXPathService");
        await _documentService.SaveDocument(doc, docName);
    }

    IEnumerable<InvoiceFieldGroup> CreateFieldViews(IEnumerable<InvoiceField> mappings) => mappings
        .GroupBy(m => m.GroupKey)
        .Select(g => new InvoiceFieldGroup(
            groupName: g.Key ?? "",
            fields: [.. g.Select(f => new InvoiceFieldViewModel(f, GetFieldValue(f), _localizer))],
            localizer: _localizer));

    void RegisterFieldPropertyCallbacks() => GroupedFields
        .ForEach((i_, g) => g.Fields.ForEach((_, f) => f.PropertyChanged += OnFieldPropertyChanged));

    string GetFieldValue(InvoiceField mapping)
    {
        var node = _xPathService.Select(mapping);
        return node?.InnerText ?? string.Empty;
    }

    void OnFieldPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender == null || e.PropertyName != nameof(InvoiceFieldViewModel.Value))
            return;

        var viewModel = (InvoiceFieldViewModel)sender;
        try
        {
            var node = _xPathService.Select(viewModel.FieldInfo)
                ?? _xPathService.RegisterElement(viewModel.FieldInfo);

            node?.InnerText = viewModel.Value;
        }
        catch (Exception ex)
        {
            _logger.LogErrorMessage(ex.Message);
            _logger.LogErrorMessage($"OnFieldPropertyChanged failed for field key: {viewModel.FieldInfo.FieldKey}");
        }
    }
}
