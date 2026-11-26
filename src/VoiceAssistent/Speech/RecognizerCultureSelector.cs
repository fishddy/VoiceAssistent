using System.Globalization;

namespace VoiceAssistent.Speech;

public static class RecognizerCultureSelector
{
    public static CultureInfo Select(
        IReadOnlyCollection<CultureInfo> installedCultures,
        string? preferredCultureName,
        CultureInfo systemCulture)
    {
        if (installedCultures.Count == 0)
        {
            throw new SpeechSetupException(
                "No speech recognizer is installed. Add a speech language under Windows Settings > Time & Language > Speech.");
        }

        if (!string.IsNullOrWhiteSpace(preferredCultureName))
        {
            return FindPreferred(installedCultures, preferredCultureName);
        }

        return installedCultures.FirstOrDefault(culture => culture.Name == systemCulture.Name)
            ?? installedCultures.FirstOrDefault(culture =>
                culture.TwoLetterISOLanguageName == systemCulture.TwoLetterISOLanguageName)
            ?? installedCultures.First();
    }

    private static CultureInfo FindPreferred(IReadOnlyCollection<CultureInfo> installedCultures, string preferredCultureName)
    {
        var match = installedCultures.FirstOrDefault(culture =>
            culture.Name.Equals(preferredCultureName, StringComparison.OrdinalIgnoreCase));

        if (match is null)
        {
            var installedNames = string.Join(", ", installedCultures.Select(culture => culture.Name));
            throw new SpeechSetupException(
                $"No speech recognizer for '{preferredCultureName}' is installed. Installed recognizers: {installedNames}.");
        }

        return match;
    }
}
