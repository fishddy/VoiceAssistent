namespace VoiceAssistant.Configuration;

public sealed record OverlaySettings
{
    public ScreenCorner Corner { get; init; } = ScreenCorner.TopRight;

    public double Margin { get; init; } = 24;

    public double Width { get; init; } = 460;

    public double MaxHeight { get; init; } = 480;

    public double CornerRadius { get; init; } = 16;

    public double BackgroundOpacity { get; init; } = 0.85;

    public double FontSize { get; init; } = 16;

    internal void Validate()
    {
        InvalidSettingsException.ThrowUnless(Enum.IsDefined(Corner), "Overlay.Corner is not a known corner.");
        InvalidSettingsException.ThrowUnless(Margin >= 0, "Overlay.Margin must not be negative.");
        InvalidSettingsException.ThrowUnless(Width > 0, "Overlay.Width must be greater than 0.");
        InvalidSettingsException.ThrowUnless(MaxHeight > 0, "Overlay.MaxHeight must be greater than 0.");
        InvalidSettingsException.ThrowUnless(CornerRadius >= 0, "Overlay.CornerRadius must not be negative.");
        InvalidSettingsException.ThrowUnless(
            BackgroundOpacity is >= 0 and <= 1,
            "Overlay.BackgroundOpacity must be between 0 and 1.");
        InvalidSettingsException.ThrowUnless(FontSize > 0, "Overlay.FontSize must be greater than 0.");
    }
}
