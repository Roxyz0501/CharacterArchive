using System.Text;
using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Shell;

namespace CharacterArchive.Services;

/// <summary>
/// Executes a built-in text command through the same chat-box entry point used by the game UI.
/// This class never constructs or sends network packets directly.
/// </summary>
public static unsafe class GameChatCommandService
{
    public static bool CanExecuteTextCommand()
    {
        var uiModule = UIModule.Instance();
        var shellModule = RaptureShellModule.Instance();
        return uiModule is not null && shellModule is not null && !shellModule->IsTextCommandUnavailable;
    }

    public static bool RequestPlayTime()
    {
        const string command = "/playtime";
        var uiModule = UIModule.Instance();
        if (uiModule is null || !CanExecuteTextCommand())
            return false;

        var message = Utf8String.FromSequence(Encoding.UTF8.GetBytes(command));
        try
        {
            uiModule->ProcessChatBoxEntry(message);
            return true;
        }
        finally
        {
            message->Dtor(true);
        }
    }
}
