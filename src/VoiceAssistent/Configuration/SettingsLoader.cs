using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace VoiceAssistent.Configuration;

public static class SettingsLoader
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter(allowIntegerValues: false) }
    };

    public static AssistantSettings LoadFromFile(string path)
    {
        if (!File.Exists(path))
        {
            return Validated(new AssistantSettings());
        }

        return Parse(File.ReadAllText(path));
    }

    public static AssistantSettings Parse(string json)
    {
        try
        {
            var settings = JsonSerializer.Deserialize<AssistantSettings>(json, SerializerOptions);
            return Validated(settings ?? new AssistantSettings());
        }
        catch (JsonException exception)
        {
            throw new InvalidSettingsException($"The settings file is not valid: {exception.Message}", exception);
        }
    }

    public static string Serialize(AssistantSettings settings) =>
        JsonSerializer.Serialize(Validated(settings), SerializerOptions);

    private static AssistantSettings Validated(AssistantSettings settings)
    {
        settings.Validate();
        return settings;
    }
}
