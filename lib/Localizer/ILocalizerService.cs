namespace Utharn.Library.Localizer;

public interface ILocalizerService
{
    string? GetCurrentLanguage();
    bool IsThai();
    bool IsEng();
    void SetLanguage(string culture);
    void SetThai();
    void SetEnglish();
}