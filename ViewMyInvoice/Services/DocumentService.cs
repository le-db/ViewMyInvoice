using System.Xml;
using Uno.Extensions.Specialized;
using Windows.Storage.Pickers;

namespace ViewMyInvoice.Services;

public record DocumentService : IDocumentService
{
    const string DEFAULT_SAVE_FILENAME = "edited_invoice.xml";

    readonly ILogger<DocumentService> _logger;

    #region file pickers
    readonly Dictionary<string, IList<string>> _fileSavePickerTypes = new() {
        { "XML", [ ".xml" ] },
    };
    readonly FileSavePicker _fileSavePicker = new()
    {
        SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
    };

    readonly IEnumerable<string> _fileOpenPickerTypes = [ ".xml" ];
    readonly FileOpenPicker _fileOpenPicker = new()
    {
        SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
    };
    #endregion

    public DocumentService(ILogger<DocumentService> logger)
    {
        _logger = logger;

        _fileSavePickerTypes.ForEach((_, kv) => _fileSavePicker.FileTypeChoices.Add(kv.Key, kv.Value));
        _fileOpenPickerTypes.ForEach((_, t) => _fileOpenPicker.FileTypeFilter.Add(t));
    }

    public async Task SaveDocument(XmlDocument document, string? suggestedFileName)
    {
        _fileSavePicker.SuggestedFileName = string.IsNullOrEmpty(suggestedFileName)
            ? DEFAULT_SAVE_FILENAME : $"edited_{suggestedFileName}";
        var saveFile = await _fileSavePicker.PickSaveFileAsync();
        if (saveFile == null) return;
        CachedFileManager.DeferUpdates(saveFile);

        await FileIO.WriteTextAsync(saveFile, document.OuterXml);
        await CachedFileManager.CompleteUpdatesAsync(saveFile);
    }

    public async Task<(XmlDocument doc, string docName)?> OpenDocument()
    {
        var pickedFile = await _fileOpenPicker.PickSingleFileAsync();
        if (pickedFile == null) return default;

        var text = await FileIO.ReadTextAsync(pickedFile);
        try
        {
            return (LoadXml(text), pickedFile.Name);
        }
        catch (Exception ex)
        {
            _logger.LogErrorMessage(ex.Message);
            return default;
        }
    }

    static XmlDocument LoadXml(string text)
    {
        var xml = new XmlDocument();
        xml.LoadXml(text);
        return xml;
    }
}
