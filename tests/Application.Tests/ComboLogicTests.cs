using System;
using System.Collections.Generic;
using DearImGuiKSP;
using Xunit;
using Facade = DearImGuiKSP.DearImGuiKSP;

namespace Application.Tests
{
    /// <summary>
    /// FR-2 combo logic (1.3.0): preview resolution (null/empty list and
    /// out-of-range index all show "(none)" without touching the ref) and
    /// selection mapping (a valid click writes the index and reports true).
    /// The widget body past the guard is native (BeginCombo/Selectable) and
    /// untestable on the net48 runner, so coverage goes through the internal
    /// seams plus the real facade guard (unavailable = false, never throws).
    /// Shares the "FacadeStatics" collection so the guard test can pin the
    /// facade statics without racing the other mutating classes.
    /// </summary>
    [Collection("FacadeStatics")]
    public class ComboLogicTests : IDisposable
    {
        public void Dispose()
        {
            Facade.Lifecycle = null;
            Facade.FrameOpen = false;
        }

        // ---- ResolveComboPreview: clamping / "(none)" cases ----

        [Fact]
        public void Preview_NullItems_ShowsNone()
        {
            Assert.Equal("(none)", Facade.ResolveComboPreview(null, 0));
        }

        [Fact]
        public void Preview_EmptyList_ShowsNone()
        {
            Assert.Equal("(none)", Facade.ResolveComboPreview(new string[0], 0));
            Assert.Equal("(none)", Facade.ResolveComboPreview(new List<string>(), 0));
        }

        [Fact]
        public void Preview_NegativeIndex_ShowsNone()
        {
            var items = new[] { "a", "b" };
            Assert.Equal("(none)", Facade.ResolveComboPreview(items, -1));
        }

        [Fact]
        public void Preview_IndexEqualToCount_ShowsNone()
        {
            var items = new[] { "a", "b" };
            Assert.Equal("(none)", Facade.ResolveComboPreview(items, 2));
        }

        [Fact]
        public void Preview_IndexBeyondCount_ShowsNone()
        {
            var items = new[] { "a", "b" };
            Assert.Equal("(none)", Facade.ResolveComboPreview(items, 99));
            Assert.Equal("(none)", Facade.ResolveComboPreview(new[] { "only" }, 5));
        }

        [Fact]
        public void Preview_ValidIndex_ReturnsItemText()
        {
            var items = new[] { "Kerbin", "Mun", "Minmus" };
            Assert.Equal("Kerbin", Facade.ResolveComboPreview(items, 0));
            Assert.Equal("Minmus", Facade.ResolveComboPreview(items, 2));
        }

        [Fact]
        public void Preview_NullEntry_RendersEmptyPreview()
        {
            var items = new string[] { null };
            Assert.Equal(string.Empty, Facade.ResolveComboPreview(items, 0));
        }

        // ---- ApplyComboSelection: selection mapping ----

        [Fact]
        public void Selection_ValidClick_WritesIndexAndReturnsTrue()
        {
            int selected = 0;
            bool changed = Facade.ApplyComboSelection(3, 2, ref selected);

            Assert.True(changed);
            Assert.Equal(2, selected);
        }

        [Fact]
        public void Selection_ClickAlreadySelected_WritesSameIndexAndReturnsTrue()
        {
            // Stock combo semantics: clicking the highlighted row is a click.
            int selected = 1;
            bool changed = Facade.ApplyComboSelection(3, 1, ref selected);

            Assert.True(changed);
            Assert.Equal(1, selected);
        }

        [Fact]
        public void Selection_NegativeClick_LeavesRefUntouched()
        {
            int selected = 1;
            Assert.False(Facade.ApplyComboSelection(3, -1, ref selected));
            Assert.Equal(1, selected);
        }

        [Fact]
        public void Selection_ClickBeyondCount_LeavesRefUntouched()
        {
            int selected = 1;
            Assert.False(Facade.ApplyComboSelection(3, 3, ref selected));
            Assert.Equal(1, selected);
        }

        // ---- Facade guard: unavailable / outside a callback ----

        [Fact]
        public void Combo_OutsideFrame_ReturnsFalseWithoutThrowing()
        {
            // No lifecycle wired: CanDeclareUi is false, so the call must
            // return at the gate before any native call, for any item state.
            int selected = 0;
            Assert.False(Facade.Combo("Preset", ref selected, new[] { "a" }));
            Assert.False(Facade.Combo("Preset", ref selected, null));
            Assert.False(Facade.Combo("Preset", ref selected, new string[0]));
            Assert.Equal(0, selected); // the ref is never rewritten on the no-op path
        }
    }
}
