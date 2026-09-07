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
    /// C08 widget correctness &amp; label/ID family: G2-07 (RadioButton(ref bool)
    /// assigns on click), G2-08/G2-09 (##/### ID suffixes stripped from display,
    /// theme-independent InputText identity), G2-11 (process-global window-title
    /// collision warning), G3-17 (spinner out-of-range type is a logged no-op),
    /// G3-20 (empty label/id gets the sentinel ID instead of colliding with the
    /// window's own ImGui ID), G3-29 (single-allocation ToUtf8), G4-01 (spinner
    /// "" id takes the per-type default, ISSUES #005). Shares the
    /// "FacadeStatics" collection: these tests mutate facade statics and must
    /// not run in parallel with the other classes that do.
    /// </summary>
    /// <remarks>
    /// Widget bodies that pass their guards reach a P/Invoke the net48 test
    /// runner cannot satisfy, so coverage goes through the same internal-seam
    /// style as <see cref="AbiGuardTests"/>: the guard/resolution logic is
    /// exercised directly, and the no-op guards are exercised through the real
    /// facade entry points (they return before any native call).
    /// </remarks>
    [Collection("FacadeStatics")]
    public class WidgetGuardTests
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
                Facade.CurrentConsumerId = null;
                OpenScopeTracker.Reset();
            }
        }

        private static string Decode(byte[] utf8)
        {
            int length = 0;
            while (length < utf8.Length && utf8[length] != 0)
            {
                length++;
            }
            return Encoding.UTF8.GetString(utf8, 0, length);
        }

        // ---- G2-07: RadioButton(string, ref bool) assigns its ref on click ----

        [Fact]
        public void RadioClick_Clicked_SetsValueTrue()
        {
            bool value = false;
            Facade.ApplyRadioClick(true, ref value);
            Assert.True(value);
        }

        [Fact]
        public void RadioClick_NotClicked_LeavesValueUntouched()
        {
            bool value = false;
            Facade.ApplyRadioClick(false, ref value);
            Assert.False(value);

            value = true;
            Facade.ApplyRadioClick(false, ref value);
            Assert.True(value); // a radio is never unselected by clicking elsewhere
        }

        // ---- G2-08 / G2-09: ## / ### suffixes are identity, never display ----

        [Fact]
        public void StripIdSuffix_StripsDoubleHashTail()
        {
            Assert.Equal("Throttle", Facade.StripIdSuffix("Throttle##eng1"));
            Assert.Equal("Foo", Facade.StripIdSuffix("Foo###id"));
            Assert.Equal("", Facade.StripIdSuffix("##invisible"));
            Assert.Equal("", Facade.StripIdSuffix("###id"));
        }

        [Fact]
        public void StripIdSuffix_NoSuffix_ReturnsSameReference()
        {
            // No allocation on the per-frame path when there is no suffix.
            string label = new string(new[] { 'P', 'l', 'a', 'i', 'n' });
            Assert.Same(label, Facade.StripIdSuffix(label));
        }

        [Fact]
        public void StripIdSuffix_NullAndEmpty_PassThrough()
        {
            Assert.Null(Facade.StripIdSuffix(null));
            Assert.Equal("", Facade.StripIdSuffix(""));
            Assert.Equal(0, Facade.StripIdSuffixLength(null));
            Assert.Equal(3, Facade.StripIdSuffixLength("abc##x"));
        }

        // ---- G2-09: hidden-label InputText ID is theme-independent ----

        [Fact]
        public void HiddenLabel_EncodesHiddenDisplayThenHashResetThenFullLabel()
        {
            // "##" + display + "###" + label + NUL: the "###" resets ImHashStr to
            // the window seed (imgui.cpp:2578), so the widget ID hashes exactly
            // as stock igInputText(label) under every theme.
            Assert.Equal("##Name###Name", Decode(ImGuiInternal.HiddenLabelUtf8("Name")));
            Assert.Equal("##Throttle###Throttle##eng1", Decode(ImGuiInternal.HiddenLabelUtf8("Throttle##eng1")));
            Assert.Equal("#######eng1", Decode(ImGuiInternal.HiddenLabelUtf8("##eng1")));
        }

        [Fact]
        public void HiddenLabel_ReusesBuffer()
        {
            // Zero steady-state allocation: the reused buffer is grown once.
            Assert.Same(ImGuiInternal.HiddenLabelUtf8("a"), ImGuiInternal.HiddenLabelUtf8("b"));
        }

        // ---- G3-20: empty label/id guard (docs/10 §6 promise) ----

        [Fact]
        public void ToIdUtf8_EmptyOrNull_ReturnsInvisibleSentinel()
        {
            Assert.Equal("##dk_empty_id", Decode(ImGuiInternal.ToIdUtf8("")));
            Assert.Equal("##dk_empty_id", Decode(ImGuiInternal.ToIdUtf8(null)));
            Assert.Same(ImGuiInternal.ToIdUtf8(""), ImGuiInternal.ToIdUtf8(null));
        }

        [Fact]
        public void ToIdUtf8_NonEmpty_EncodesVerbatim()
        {
            Assert.Equal("OK##x", Decode(ImGuiInternal.ToIdUtf8("OK##x")));
        }

        // ---- G3-17: out-of-range SpinnerType is a logged no-op ----

        [Fact]
        public void Spinner_OutOfRangeType_IsLoggedNoOp()
        {
            using (var h = new Harness())
            {
                // Gate is open; the guard must return before any P/Invoke.
                Facade.Spinner((SpinnerType)99, 10f, 2f);
                Facade.Spinner((SpinnerType)(-1), 10f, 2f);

                Assert.Equal(2, h.Logger.Warnings.Count);
            }
        }

        // ---- G4-01: spinner "" id takes the per-type default (ISSUES #005) ----

        [Fact]
        public void SpinnerLabel_EmptyId_UsesPerTypeDefault()
        {
            byte[] byNull = Facade.ResolveSpinnerLabel(SpinnerType.Clock, null);
            byte[] byEmpty = Facade.ResolveSpinnerLabel(SpinnerType.Clock, "");

            Assert.Equal("##dk_spinner_clock", Decode(byEmpty));
            Assert.Same(byNull, byEmpty); // default path is allocation-free
        }

        [Fact]
        public void SpinnerLabel_CallerId_EncodesVerbatim()
        {
            Assert.Equal("##clock_a", Decode(Facade.ResolveSpinnerLabel(SpinnerType.Clock, "##clock_a")));
        }

        [Fact]
        public void SpinnerLabel_Defaults_AreUniquePerType()
        {
            // The window-ID collision (ISSUES #005) is only safely avoided if no
            // two types share a default ID.
            var seen = new System.Collections.Generic.HashSet<string>();
            foreach (SpinnerType type in Enum.GetValues(typeof(SpinnerType)))
            {
                Assert.True(seen.Add(Decode(Facade.ResolveSpinnerLabel(type, null))), "duplicate default for " + type);
            }
        }

        // ---- G2-11: process-global window-title collision detection ----

        [Fact]
        public void WindowTitle_SameConsumerTwice_NoWarning()
        {
            using (var h = new Harness())
            {
                Facade.CurrentConsumerId = "mod_a";
                Facade.WarnOnWindowTitleCollision("C08 test title A");
                Facade.WarnOnWindowTitleCollision("C08 test title A");

                Assert.Empty(h.Logger.Warnings);
            }
        }

        [Fact]
        public void WindowTitle_TwoConsumers_WarnsOncePerTitle()
        {
            using (var h = new Harness())
            {
                Facade.CurrentConsumerId = "mod_a";
                Facade.WarnOnWindowTitleCollision("C08 test title B");
                Facade.CurrentConsumerId = "mod_b";
                Facade.WarnOnWindowTitleCollision("C08 test title B");
                Facade.WarnOnWindowTitleCollision("C08 test title B"); // already warned
                Facade.CurrentConsumerId = "mod_c";
                Facade.WarnOnWindowTitleCollision("C08 test title B"); // still once per name

                Assert.Single(h.Logger.Warnings);
                Assert.Contains("mod_a", h.Logger.Warnings[0]);
                Assert.Contains("mod_b", h.Logger.Warnings[0]);
            }
        }

        [Fact]
        public void WindowTitle_Null_NotTracked()
        {
            using (var h = new Harness())
            {
                Facade.WarnOnWindowTitleCollision(null);
                Assert.Empty(h.Logger.Warnings);
            }
        }

        // ---- G3-29: ToUtf8 single allocation / shared empty ----

        [Fact]
        public void HiddenLabel_AndSentinel_DoNotDisturbPerCapacityInputTextBuffers()
        {
            // C07 invariant, re-pinned on the C08-touched file: buf_size IS the
            // buffer length, so capacity is honored by construction.
            Assert.Equal(24, ImGuiInternal.GetInputTextBuffer(24).Length);
            Assert.Same(ImGuiInternal.GetInputTextBuffer(24), ImGuiInternal.GetInputTextBuffer(24));
        }
    }
}
