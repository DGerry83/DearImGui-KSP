using System;
using System.Text;
using DearImGuiKSP;
using DearImGuiKSP.Application;
using DearImGuiKSP.Interop;
using Xunit;
using Facade = DearImGuiKSP.DearImGuiKSP;

namespace Application.Tests
{
    /// <summary>
    /// C07 ABI enum/count validation family: the release native build compiles out
    /// ImGui/ImPlot's assert guards (/DNDEBUG), so the managed layer must validate
    /// before crossing the ABI. Covers G2-10 (InputText honors per-call capacity),
    /// G3-18 (UTF-8-boundary-safe seed clamp), G3-22 (BeginSubplots rows/cols),
    /// G3-23/G3-24 (PushStyleColor/PushStyleVar reject COUNT and out-of-range).
    /// G3-30 (SliderFloat AlwaysClamp) is native-flag only — verified in-game at
    /// gate B. Shares the "FacadeStatics" collection: these tests mutate facade
    /// statics and must not run in parallel with the other classes that do.
    /// </summary>
    /// <remarks>
    /// InputText coverage goes through the internal <see cref="ImGuiInternal"/>
    /// seams: the native call's buf_size is the buffer's length
    /// (ImGuiNative.InputText passes buffer.Length), so per-capacity buffers are
    /// the capacity guarantee, and the seed staging is tested directly. The real
    /// P/Invoke cannot be intercepted on the net48 test runner (its reflection
    /// does not expose the extern's function-pointer field).
    /// </remarks>
    [Collection("FacadeStatics")]
    public class AbiGuardTests
    {
        private sealed class Harness : IDisposable
        {
            internal readonly FakeLogger Logger = new FakeLogger();

            internal Harness()
            {
                var machine = new LifecycleStateMachine(new FakeLogger());
                machine.MarkInitializing();
                machine.MarkRunning();

                Facade.Log = Logger;
                Facade.Lifecycle = machine;
                Facade.FrameOpen = true;
                OpenScopeTracker.Reset();
            }

            public void Dispose()
            {
                Facade.Log = null;
                Facade.Lifecycle = null;
                Facade.FrameOpen = false;
                OpenScopeTracker.Reset();
            }
        }

        private static string SeededText(byte[] buffer)
        {
            int length = 0;
            while (length < buffer.Length && buffer[length] != 0)
            {
                length++;
            }
            return Encoding.UTF8.GetString(buffer, 0, length);
        }

        [Fact]
        public void PushStyleColor_Count_ColorOverload_IsLoggedNoOp()
        {
            using (var h = new Harness())
            {
                Facade.PushStyleColor(ImGuiCol.COUNT, UnityEngine.Color.white);

                Assert.Equal(0, OpenScopeTracker.StyleColors);
                Assert.Single(h.Logger.Warnings);
            }
        }

        [Fact]
        public void PushStyleColor_Count_Color32Overload_IsLoggedNoOp()
        {
            using (var h = new Harness())
            {
                Facade.PushStyleColor(ImGuiCol.COUNT, new UnityEngine.Color32(1, 2, 3, 4));

                Assert.Equal(0, OpenScopeTracker.StyleColors);
                Assert.Single(h.Logger.Warnings);
            }
        }

        [Fact]
        public void PushStyleColor_NegativeCast_IsLoggedNoOp()
        {
            using (var h = new Harness())
            {
                Facade.PushStyleColor((ImGuiCol)(-1), UnityEngine.Color.white);

                Assert.Equal(0, OpenScopeTracker.StyleColors);
                Assert.Single(h.Logger.Warnings);
            }
        }

        [Fact]
        public void PushStyleVar_Count_FloatOverload_IsLoggedNoOp()
        {
            using (var h = new Harness())
            {
                Facade.PushStyleVar(ImGuiStyleVar.COUNT, 4f);

                Assert.Equal(0, OpenScopeTracker.StyleVars);
                Assert.Single(h.Logger.Warnings);
            }
        }

        [Fact]
        public void PushStyleVar_Count_Vector2Overload_IsLoggedNoOp()
        {
            using (var h = new Harness())
            {
                Facade.PushStyleVar(ImGuiStyleVar.COUNT, UnityEngine.Vector2.one);

                Assert.Equal(0, OpenScopeTracker.StyleVars);
                Assert.Single(h.Logger.Warnings);
            }
        }

        [Fact]
        public void PushStyleVar_NegativeCast_IsLoggedNoOp()
        {
            using (var h = new Harness())
            {
                Facade.PushStyleVar((ImGuiStyleVar)(-1), 4f);

                Assert.Equal(0, OpenScopeTracker.StyleVars);
                Assert.Single(h.Logger.Warnings);
            }
        }

        [Fact]
        public void BeginSubplots_NonPositiveRows_IsLoggedNoOp()
        {
            using (var h = new Harness())
            {
                ImGuiPlot.SubplotScope scope = ImGuiPlot.BeginSubplots("t", 0, 2, UnityEngine.Vector2.zero);

                Assert.False(scope.Visible);
                Assert.Equal(0, OpenScopeTracker.Subplots);
                Assert.Single(h.Logger.Warnings);
            }
        }

        [Fact]
        public void BeginSubplots_NegativeCols_IsLoggedNoOp()
        {
            using (var h = new Harness())
            {
                ImGuiPlot.SubplotScope scope = ImGuiPlot.BeginSubplots("t", 2, -1, UnityEngine.Vector2.zero);

                Assert.False(scope.Visible);
                Assert.Equal(0, OpenScopeTracker.Subplots);
                Assert.Single(h.Logger.Warnings);
            }
        }

        [Fact]
        public void InputTextBuffer_Length_EqualsCallerCapacity()
        {
            // The native call's buf_size is the buffer's length, so a right-sized
            // buffer IS the capacity guarantee (G2-10).
            Assert.Equal(16, ImGuiInternal.GetInputTextBuffer(16).Length);
            Assert.Equal(64, ImGuiInternal.GetInputTextBuffer(64).Length);
        }

        [Fact]
        public void InputTextBuffer_ReusedPerCapacity_DistinctAcrossCapacities()
        {
            // Steady-state frames allocate nothing: one buffer per capacity,
            // created once (G2-10; hot-path checklist).
            Assert.Same(ImGuiInternal.GetInputTextBuffer(32), ImGuiInternal.GetInputTextBuffer(32));
            Assert.NotSame(ImGuiInternal.GetInputTextBuffer(32), ImGuiInternal.GetInputTextBuffer(48));
        }

        [Fact]
        public void InputTextSeed_Clamp_HonorsCapacity()
        {
            byte[] buffer = ImGuiInternal.GetInputTextBuffer(8);
            ImGuiInternal.StageInputTextSeed("abcdefghij", buffer); // 10 UTF-8 bytes

            // capacity-1 = 7 bytes seeded, not some grown shared buffer's length.
            Assert.Equal("abcdefg", SeededText(buffer));
        }

        [Fact]
        public void InputTextSeed_Clamp_DoesNotSplitUtf8Sequence()
        {
            // "ab" + three U+00E9 (2 bytes each) = 8 bytes; capacity-1 = 5 lands
            // mid-sequence on the second U+00E9. Boundary-safe clamp keeps "ab" +
            // one U+00E9 (G3-18) — no persisted U+FFFD.
            byte[] buffer = ImGuiInternal.GetInputTextBuffer(6);
            ImGuiInternal.StageInputTextSeed("abééé", buffer);

            Assert.Equal("abé", SeededText(buffer));
            Assert.DoesNotContain('\uFFFD', SeededText(buffer));
        }
    }
}
