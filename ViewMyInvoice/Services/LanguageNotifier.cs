namespace ViewMyInvoice.Services;

public interface ILanguageNotifier
{
    event Action? LanguageChanged;
    void NotifyLanguageChanged();
}

public class LanguageNotifier : ILanguageNotifier
{
    public event Action? LanguageChanged;
    public void NotifyLanguageChanged() => LanguageChanged?.Invoke();
}
