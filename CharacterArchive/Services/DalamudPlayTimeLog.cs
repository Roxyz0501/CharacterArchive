using Dalamud.Game.Chat;

namespace CharacterArchive.Services;

// The host's ILogMessage must never be retained after its callback returns.
internal readonly struct DalamudPlayTimeLog(ILogMessage message) : IPlayTimeLog
{
    public uint Id => message.LogMessageId;
    public int ParameterCount => message.ParameterCount;
    public bool TryGetInt(int index, out int value) => message.TryGetIntParameter(index, out value);
}
