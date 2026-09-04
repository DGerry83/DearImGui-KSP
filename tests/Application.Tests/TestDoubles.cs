using System.Collections.Generic;
using DearImGuiKSP.Application;
using DearImGuiKSP.Application.Interfaces;

namespace Application.Tests
{
    /// <summary>Hand-written test doubles implementing the Application.Interfaces (no mocking framework).</summary>
    internal sealed class FakeLogger : ILogger
    {
        public readonly List<string> Errors = new List<string>();
        public readonly List<string> Warnings = new List<string>();

        public void Error(string message) { Errors.Add(message); }
        public void Warn(string message) { Warnings.Add(message); }
        public void Info(string message) { }
        public void Debug(string message) { }
    }

    internal sealed class FakeSettingsStore : ISettingsStore
    {
        public LibrarySettings Loaded = new LibrarySettings();
        public readonly List<LibrarySettings> Saves = new List<LibrarySettings>();

        public LibrarySettings Load() { return Loaded; }

        public void Save(LibrarySettings settings) { Saves.Add(settings); }
    }

    internal sealed class FakeInputLockGateway : IInputLockGateway
    {
        public int ApplyLocksCallCount;
        public int ReleaseLocksCallCount;
        public InputCaptureState LastState;
        public IReadOnlyList<string> LastConsumerIds;

        public void ApplyLocks(InputCaptureState state, IReadOnlyList<string> consumerIds)
        {
            ApplyLocksCallCount++;
            LastState = state;
            LastConsumerIds = consumerIds;
        }

        public void ReleaseLocks() { ReleaseLocksCallCount++; }
    }

    internal sealed class FakePointerBlockerGateway : IPointerBlockerGateway
    {
        public readonly List<bool> Calls = new List<bool>();

        public void SetBlocked(bool blocked) { Calls.Add(blocked); }

        public int CallCount => Calls.Count;
    }

    internal sealed class FakeImguiEventEaterGateway : IImguiEventEaterGateway
    {
        public readonly List<bool> MouseCalls = new List<bool>();
        public readonly List<bool> KeyboardCalls = new List<bool>();

        public void SetMouseShielded(bool shielded) { MouseCalls.Add(shielded); }

        public void SetKeyboardShielded(bool shielded) { KeyboardCalls.Add(shielded); }
    }
}
