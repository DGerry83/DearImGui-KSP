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
        public IReadOnlyList<string> LastConsumerIds;

        public void ApplyLocks(InputCaptureState state, IReadOnlyList<string> consumerIds)
        {
            ApplyLocksCallCount++;
            LastConsumerIds = consumerIds;
        }

        public void ReleaseLocks() { ReleaseLocksCallCount++; }
    }

    /// <summary>
    /// Recording <see cref="INativeBridge"/> double shared by the frame-loop test
    /// classes (C14: replaces the per-class RecordingBridge/FakeNativeBridge copies).
    /// Counts frame boundaries and records viewport-clamp calls; everything else
    /// is inert.
    /// </summary>
    internal sealed class FakeNativeBridge : INativeBridge
    {
        public int BeginCount;
        public int EndCount;
        public readonly List<KeyValuePair<float, float>> ClampCalls =
            new List<KeyValuePair<float, float>>();

        private readonly InputCaptureState _captureState = new InputCaptureState();

        public int Initialize() { return 0; }
        public bool LoadFontFromFile(string utf8Path, float sizePixels) { return true; }
        public InputCaptureState GetIoSnapshot() { return _captureState; }
        public void BeginUiFrame(float width, float height, float deltaSeconds) { BeginCount++; }
        public void EndUiFrame() { EndCount++; }
        public void RebuildViewport(int width, int height) { }

        public void ClampWindowsToViewport(float width, float height)
        {
            ClampCalls.Add(new KeyValuePair<float, float>(width, height));
        }

        public void Shutdown() { }
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
