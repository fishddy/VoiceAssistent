namespace VoiceAssistent.Configuration;

public sealed class InvalidSettingsException : Exception
{
    public InvalidSettingsException(string message)
        : base(message)
    {
    }

    public InvalidSettingsException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    internal static void ThrowUnless(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidSettingsException(message);
        }
    }
}
