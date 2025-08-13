// LorealAvaloniaUI/Messages/LanguageChangedMessage.cs

using System.Globalization;

namespace LorealAvaloniaUI.Messages
{
    public class LanguageChangedMessage
    {
        public CultureInfo NewCulture { get; }

        public LanguageChangedMessage(CultureInfo newCulture)
        {
            NewCulture = newCulture;
        }
    }
}