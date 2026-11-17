using CharacterArchive.Services;

internal static class PlayTimeLogTests
{
    public static void Run(List<string> failures, string pluginSource)
    {
        var checks = 0;
        void Check(bool ok, string label) { checks++; if (!ok) failures.Add(label); }
        Check(!new[] { "OnUiModulePacket", "HookFromAddress", "HookFromFunctionPointerVariable", "Marshal.ReadInt32", "IGameInteropProvider", "UIModulePacketType" }
            .Any(pluginSource.Contains), "Plugin has no native UI-packet detour, hook service or raw packet reader");
        Check(pluginSource.Contains("Chat.LogMessage += OnLogMessage") && pluginSource.Contains("Chat.LogMessage -= OnLogMessage"), "managed log callback has paired subscription/unsubscription");
        Check(PlayTimeLogReceiver.TryConvert(12, 3, 45, out var total) && total == 17505, "three log parameters convert to total minutes");
        Check(PlayTimeLogReceiver.TryConvert(0, 0, 0, out total) && total == 0, "zero-time character supported");
        foreach (var value in new[] { (-1, 0, 0), (0, -1, 0), (0, 24, 0), (0, 0, -1), (0, 0, 60), (int.MaxValue, 0, 0) })
            Check(!PlayTimeLogReceiver.TryConvert(value.Item1, value.Item2, value.Item3, out _), "invalid/overflow time rejected: " + value);
        var days = (int)(uint.MaxValue / 1440); var hours = (int)(uint.MaxValue % 1440 / 60); var minutes = (int)(uint.MaxValue % 60);
        Check(PlayTimeLogReceiver.TryConvert(days, hours, minutes, out total) && total == uint.MaxValue, "maximum stored minute value supported");
        Check(!PlayTimeLogReceiver.TryConvert(days, hours, minutes + 1, out _), "minute total cannot wrap");

        var delivered = new Queue<PlayTimeObservation>(); var reports = 0; var characterReads = 0;
        using (var receiver = new PlayTimeLogReceiver(() => { characterReads++; return 1; }, delivered.Enqueue, _ => reports++))
        {
            var unrelated = new Stub { IdValue = 100, ThrowCount = true };
            for (var i = 0; i < 10000; i++) receiver.Receive(unrelated);
            Check(delivered.Count == 0 && characterReads == 0 && reports == 0 && !receiver.Stopped, "10,000 unrelated housing/system logs inspect no parameters/character or save data");
            var log = new Stub { Values = [12, 3, 45] };
            receiver.Receive(log);
            log.Values = []; log.ThrowId = log.ThrowCount = log.ThrowInt = true;
            Check(delivered.Count == 1 && delivered.Dequeue() == new PlayTimeObservation(1, 17505), "queued observation copies primitives and never retains invalidated host message");
            receiver.Dispose();
            receiver.Receive(log);
            Check(reports == 0 && delivered.Count == 0, "late callback after disposal reads no invalid host message");
        }
        using (var receiver = new PlayTimeLogReceiver(() => 0, _ => throw new Exception(), _ => reports++))
        {
            receiver.Receive(new Stub { ThrowCount = true });
            Check(!receiver.Stopped && reports == 0, "logged-out or unloaded character is ignored before parameters are read");
        }
        foreach (var bad in new[] { new Stub { ThrowId = true }, new Stub { ThrowCount = true }, new Stub { ThrowInt = true },
            new Stub { NonInteger = true }, new Stub { Values = [1, 2] }, new Stub { Values = [1, 2, 3, 4] }, new Stub { Values = [-1, 0, 0] } })
        {
            var notices = 0;
            using var receiver = new PlayTimeLogReceiver(() => 1, _ => throw new Exception("must not deliver"), _ => notices++);
            for (var i = 0; i < 1000; i++) receiver.Receive(bad);
            Check(receiver.Stopped && notices == 1, "getter/type/shape failure is contained, stops at three and reports once");
        }
        using (var receiver = new PlayTimeLogReceiver(() => throw new Exception("character API failure"), _ => { }, _ => throw new Exception("notification failure")))
        {
            for (var i = 0; i < 10; i++) receiver.Receive(new Stub());
            Check(receiver.Stopped, "character lookup and notification exceptions never escape callback");
        }
        using (var receiver = new PlayTimeLogReceiver(() => 1, _ => throw new Exception("queue failure"), _ => { }))
        {
            for (var i = 0; i < 3; i++) receiver.Receive(new Stub());
            Check(receiver.Stopped, "delivery failure cannot escape managed observer");
        }
        var successNotices = 0;
        using (var receiver = new PlayTimeLogReceiver(() => 1, _ => { }, _ => successNotices++))
        {
            receiver.Receive(new Stub { ThrowCount = true }); receiver.Receive(new Stub { ThrowCount = true });
            receiver.Receive(new Stub());
            receiver.Receive(new Stub { ThrowCount = true }); receiver.Receive(new Stub { ThrowCount = true });
            Check(!receiver.Stopped && successNotices == 0, "valid playtime resets only consecutive targeted failures");
        }
        var detachCount = 0;
        using (var subscription = new CallbackSubscription(() => detachCount++)) { subscription.Dispose(); subscription.Dispose(); }
        Check(detachCount == 1, "managed event detaches once");
        Action? host = null;
        Action callback = () => { };
        using (var optional = new OptionalResource<CallbackSubscription>())
        {
            optional.Initialize(() => new CallbackSubscription(() => host -= callback), _ => { host += callback; throw new Exception("after subscribe"); }, _ => { });
            Check(optional.Value is null && host is null, "partial registration is undone without aborting startup");
        }
        Console.WriteLine($"Managed playtime log checks passed: {checks}");
    }

    private sealed class Stub : IPlayTimeLog
    {
        public uint IdValue = 859;
        public int[] Values = [0, 1, 2];
        public bool ThrowId, ThrowCount, ThrowInt, NonInteger;
        public uint Id => ThrowId ? throw new Exception("synthetic ID getter") : IdValue;
        public int ParameterCount => ThrowCount ? throw new Exception("synthetic count getter") : Values.Length;
        public bool TryGetInt(int index, out int value)
        {
            if (ThrowInt) throw new Exception("synthetic parameter getter");
            value = Values[index]; return !NonInteger;
        }
    }
}
