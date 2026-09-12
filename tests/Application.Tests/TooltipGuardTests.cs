using System;
using DearImGuiKSP;
using DearImGuiKSP.Application;
using DearImGuiKSP.Interop;
using Xunit;
using Facade = DearImGuiKSP.DearImGuiKSP;

namespace Application.Tests
{
    /// <summary>
    /// FR-3 tooltip (1.3.0): internal enum value pins against the pinned
    /// cimgui header (the flags cross the ABI as raw ints into a release
    /// native build with asserts compiled out — a drifted value silently
    /// changes hover/disable behavior; explicit-value pattern per
    /// DockingEnumPinTests) and the no-op guard cases (null/empty text and
    /// unavailable/outside a callback return before any native call — the
    /// hover predicate and tooltip window are native and untestable on the
    /// net48 runner). Shares the "FacadeStatics" collection: the guard tests
    /// mutate facade statics.
    /// </summary>
    [Collection("FacadeStatics")]
    public class TooltipGuardTests : IDisposable
    {
        public void Dispose()
        {
            Facade.Log = null;
            Facade.Lifecycle = null;
            Facade.FrameOpen = false;
        }

        private sealed class Harness : IDisposable
        {
            internal Harness()
            {
                var machine = new LifecycleStateMachine(new FakeLogger());
                machine.MarkInitializing();
                machine.MarkRunning();

                Facade.Log = new FakeLogger();
                Facade.Lifecycle = machine;
                Facade.FrameOpen = true;
            }

            public void Dispose()
            {
                Facade.Log = null;
                Facade.Lifecycle = null;
                Facade.FrameOpen = false;
            }
        }

        // ---- Enum pins against the pinned cimgui.h (imgui 1.92.9) ----

        [Fact]
        public void ImGuiHoveredFlags_Subset_MatchesPinnedHeader()
        {
            // C:\Users\Matt\source\repos\cimgui\cimgui.h, enum ImGuiHoveredFlags_.
            Assert.Equal(0, (int)ImGuiHoveredFlags.None);             // cimgui.h:461 ImGuiHoveredFlags_None
            Assert.Equal(1 << 12, (int)ImGuiHoveredFlags.ForTooltip); // cimgui.h:474 ImGuiHoveredFlags_ForTooltip
        }

        [Fact]
        public void ImGuiItemFlags_Subset_MatchesPinnedHeader()
        {
            // C:\Users\Matt\source\repos\cimgui\cimgui.h, enum ImGuiItemFlags_.
            Assert.Equal(0, (int)ImGuiItemFlags.None);              // cimgui.h:327 ImGuiItemFlags_None
            Assert.Equal(1 << 6, (int)ImGuiItemFlags.Disabled);     // cimgui.h:328 ImGuiItemFlags_Disabled
        }

        [Fact]
        public void TooltipWrapWidthFactor_IsStockValue()
        {
            // imgui demo tooltip pattern: PushTextWrapPos(GetFontSize() * 35).
            Assert.Equal(35f, Facade.TooltipWrapWidthFactor);
        }

        // ---- No-op guards (return before any native call) ----

        [Fact]
        public void Tooltip_NullText_IsNoOp()
        {
            using (var h = new Harness())
            {
                // Gate is open; the null/empty guard must return before the
                // native IsItemHovered predicate.
                Facade.Tooltip(null);
            }
        }

        [Fact]
        public void Tooltip_EmptyText_IsNoOp()
        {
            using (var h = new Harness())
            {
                Facade.Tooltip(string.Empty);
            }
        }

        [Fact]
        public void Tooltip_WhitespaceText_WouldReachNative_ByDesign()
        {
            // Boundary documentation (no facade call — the net48 runner cannot
            // satisfy the native predicate): the no-op guard is null/empty
            // only, whitespace is real text and would render a blank tooltip.
            Assert.False(string.IsNullOrEmpty(" "));
        }

        [Fact]
        public void Tooltip_OutsideFrame_IsSafeNoOp()
        {
            using (var h = new Harness())
            {
                Facade.FrameOpen = false; // availability race: no-op, never throws
                Facade.Tooltip("help");
            }
        }

        [Fact]
        public void Tooltip_UnavailableSession_IsSafeNoOp()
        {
            using (var h = new Harness())
            {
                Facade.Lifecycle = null; // IsAvailable false
                Facade.Tooltip("help");
            }
        }
    }
}
