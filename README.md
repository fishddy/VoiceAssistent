<p align="center"><img src="docs/AppIcon.png" alt="Voice Assistent icon" width="128"></p>

# Voice Assistent

A Windows tray application that listens to a microphone for a wake phrase ("hey james"). When it hears it, a semi-transparent overlay with rounded corners appears on the main monitor and shows what you say. After a short pause, what you said is sent to ChatGPT and the answer appears right below it in the overlay. The overlay stays open until you say a close phrase ("thanks james" or "danke james"), then the app goes back to waiting for the wake phrase.

Speech recognition runs locally. Only the transcribed text is sent to ChatGPT.

## Download

No coding knowledge or .NET installation is needed to use the app on Windows 10 or 11 (64-bit):

1. Download `VoiceAssistent.exe` from the [latest release](https://github.com/fishddy/VoiceAssistent/releases/latest).
2. Double-click it.
3. If Windows shows "Windows protected your PC", click **More info**, then **Run anyway**. The app is not code-signed, so Windows warns about it.

The requirements below about a microphone, a speech recognizer and an internet connection still apply. The .NET SDK is only needed to build from source.

## Requirements

- Windows 10 or 11 (64-bit)
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- A microphone
- At least one Windows speech recognizer for the wake phrase (Settings > Time & Language > Speech). The recognizer matching your Windows display language is used by default.
- An internet connection, for the one-time speech model download and for ChatGPT answers
- About 550 MB of disk space for the default speech model, which is downloaded once on first start
- The Microsoft Edge WebView2 Runtime for ChatGPT answers. It is preinstalled on current Windows versions.
- Recommended: a graphics card with a Vulkan driver (any current NVIDIA, AMD or Intel GPU). Without one, speech recognition runs on the CPU and needs a smaller model.

## Getting started

```powershell
git clone https://github.com/fishddy/VoiceAssistent.git
```

```powershell
cd VoiceAssistent
```

```powershell
dotnet run --project src/VoiceAssistent
```

On first start the speech model is downloaded; a tray notification shows while that happens. After that, say "hey james", ask your question, and say "thanks james" or "danke james" to close the overlay.

To run the tests:

```powershell
dotnet test
```

The app runs in the system tray. Right-click the tray icon for **Settings…** and **Exit**; double-clicking the icon also opens the settings.

## How it works

1. Audio is captured from the selected microphone (or the Windows default) and converted to 16 kHz mono.
2. The wake phrase is detected with the classic Windows speech engine (`System.Speech`). Because that engine also matches similar-sounding phrases, the recognized audio is then transcribed with Whisper, and the overlay only opens if the transcript contains one of the wake phrases as whole words.
3. After the wake phrase, speech is transcribed with [Whisper](https://github.com/openai/whisper) through [Whisper.net](https://github.com/sandrohanea/whisper.net). The text in the overlay is refreshed about once a second while you talk and finalized after a short pause.
4. When you stop talking for the configured pause, what you said is sent to ChatGPT as a single question. The answer appears below your question, in an indented card marked "ChatGPT", while it is being written. Anything you say after that is ignored, except the close phrase.

Models are stored in `%LOCALAPPDATA%\VoiceAssistent\Models`.

Whisper runs on the GPU through Vulkan when a Vulkan-capable graphics driver is installed and falls back to the CPU otherwise. On the CPU, the large models take many seconds per sentence; choose `Small` or `Base` in that case.

## ChatGPT

The app uses the free ChatGPT website, not the paid API. When the overlay opens, it starts an off-screen browser window (WebView2) with chatgpt.com, types your question into the page and reads the answer from it. The browser is closed again as soon as the answer is complete, so it uses no memory while the app waits for the wake phrase.

- Each time the overlay opens, a new ChatGPT conversation starts. Exactly one message is sent per overlay session: your question, preceded by the configured instructions, which ask ChatGPT to keep answers short.
- The browser window always stays hidden, so ChatGPT is used without logging in. If ChatGPT is not ready or does not answer, the reason is shown in the overlay.
- The browser profile (cookies and site data) is stored in `%LOCALAPPDATA%\VoiceAssistent\ChatGpt`.

Automating the ChatGPT website is not covered by OpenAI's terms of use, which only allow programmatic access through their API, so your ChatGPT account could be restricted. The page structure also changes from time to time; the element lookups are kept in `src/VoiceAssistent/Chat/ChatGptBridge.js` so they can be updated in one place.

## Settings window

The tray settings window changes the most common options:

- **Microphone**: the input device, with a live level meter to check that it picks up your voice.
- **Box position**: the screen corner and the distance from the screen edge.
- **Phrases**: the phrases that open the box and the phrases that close it, one per line.
- **ChatGPT**: the instructions sent with each question and the pause before sending.
- **Speech recognition**: the transcription language and the Whisper model. Larger models are more accurate but slower.

Saving restarts the listener with the new settings. Settings are saved to `%APPDATA%\VoiceAssistent\appsettings.json`. Once that file exists, it is used instead of the bundled defaults.

## Configuration file

The bundled defaults live in `appsettings.json` next to the executable (`src/VoiceAssistent/appsettings.json` in the source tree). Missing values fall back to their defaults; invalid values are reported on startup.

| Setting | Default | Description |
| --- | --- | --- |
| `Audio.InputDeviceId` | `null` | Windows endpoint ID of the microphone. `null`, or a device that is no longer connected, uses the Windows default recording device. |
| `Speech.WakePhraseCulture` | `null` | Language of the wake phrase recognizer, e.g. `de-DE`. `null` picks the installed one matching the Windows display language. |
| `Speech.WakePhrases` | `["hey james"]` | Phrases that open the overlay. Extra spellings can help recognizers of other languages match the pronunciation. At least one phrase must be spelled the way Whisper writes it (e.g. `hey james`), because the Whisper check compares the transcript with these phrases. |
| `Speech.WakePhraseMinimumConfidence` | `0.6` | Confidence (0 to 1) the Windows speech engine needs before the Whisper check runs. Lower it if the wake phrase is often missed. |
| `Speech.ClosePhrases` | `["thanks james", "danke james"]` | The overlay closes when a transcribed sentence ends with one of these phrases. Case and punctuation are ignored; the phrase must be whole words. |
| `Speech.TranscriptionLanguage` | `null` | Whisper language code such as `de` or `en`, `auto` to detect it, or `null` for the Windows display language. |
| `Speech.TranscriptionModel` | `LargeV3Turbo` | `LargeV3Turbo`, `Medium`, `Small` or `Base`. |
| `Chat.Instructions` | Short-answer instructions | Text placed before the first message of each conversation. Empty sends no instructions. |
| `Chat.SendAfterPauseSeconds` | `1` | Seconds of silence after a finished sentence before it is sent (at most 10). |
| `Overlay.Corner` | `TopRight` | `TopLeft`, `TopRight`, `BottomLeft` or `BottomRight` of the main monitor's work area. |
| `Overlay.Margin` | `24` | Distance from the screen edges. |
| `Overlay.Width` | `460` | Overlay width. |
| `Overlay.MaxHeight` | `480` | Maximum height of the conversation area before it scrolls. |
| `Overlay.CornerRadius` | `16` | Rounding of the overlay corners. |
| `Overlay.BackgroundOpacity` | `0.85` | Background opacity from 0 (invisible) to 1 (solid). |
| `Overlay.FontSize` | `16` | Text font size. |

Sizes are in device-independent pixels.
