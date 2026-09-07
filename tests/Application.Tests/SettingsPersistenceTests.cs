using DearImGuiKSP;
using DearImGuiKSP.Application;
using Xunit;

namespace Application.Tests
{
    /// <summary>
    /// Pins the C06 debounced-persist behavior (G3-08/09/14/16/19): setters only
    /// flag the model dirty; the frame loop's <see cref="SettingsModel.PersistIfSettled"/>
    /// writes once the changes settle. A slider drag must cost one disk write.
    /// </summary>
    public class SettingsPersistenceTests
    {
        private static readonly float HalfDebounce = LibraryConfig.SettingsSaveDebounceSeconds * 0.5f;
        private static readonly float FullDebounce = LibraryConfig.SettingsSaveDebounceSeconds;

        private static SettingsModel CreateModel(out FakeSettingsStore store)
        {
            store = new FakeSettingsStore();
            return new SettingsModel(store);
        }

        [Fact]
        public void PersistIfSettled_NothingPending_NeverSaves()
        {
            SettingsModel model = CreateModel(out FakeSettingsStore store);

            for (int i = 0; i < 10; i++)
            {
                model.PersistIfSettled(FullDebounce);
            }

            Assert.False(model.PersistPending);
            Assert.Empty(store.Saves);
        }

        [Fact]
        public void PersistIfSettled_BelowDebounce_StillPending_NoSave()
        {
            SettingsModel model = CreateModel(out FakeSettingsStore store);

            model.UiScale = 1.5f;
            model.PersistIfSettled(HalfDebounce);

            Assert.True(model.PersistPending);
            Assert.Empty(store.Saves);
        }

        [Fact]
        public void PersistIfSettled_QuietPeriodElapses_SavesOnce_WithLatestValues()
        {
            SettingsModel model = CreateModel(out FakeSettingsStore store);

            model.UiScale = 1.5f;
            model.VerboseLogging = true;
            model.PersistIfSettled(HalfDebounce);
            model.PersistIfSettled(HalfDebounce);

            Assert.False(model.PersistPending);
            LibrarySettings saved = Assert.Single(store.Saves);
            Assert.Equal(1.5f, saved.UiScale);
            Assert.True(saved.VerboseLogging);
        }

        [Fact]
        public void SliderDrag_ContinuedChanges_RearmDebounce_OneWriteAfterSettle()
        {
            SettingsModel model = CreateModel(out FakeSettingsStore store);

            // Simulated drag: a change every frame, each frame shorter than the
            // debounce window — the write must never fire mid-drag.
            for (int i = 0; i < 30; i++)
            {
                model.UiScale = 1.0f + i * 0.01f;
                model.PersistIfSettled(HalfDebounce);
            }

            Assert.True(model.PersistPending);
            Assert.Empty(store.Saves);

            // Drag ends; the quiet period elapses — exactly one write, latest value.
            model.PersistIfSettled(FullDebounce);

            Assert.False(model.PersistPending);
            LibrarySettings saved = Assert.Single(store.Saves);
            Assert.Equal(model.UiScale, saved.UiScale);
        }

        [Fact]
        public void AfterSettle_FurtherChange_RearmsAndSavesAgain()
        {
            SettingsModel model = CreateModel(out FakeSettingsStore store);

            model.UiScale = 1.5f;
            model.PersistIfSettled(FullDebounce);
            Assert.Single(store.Saves);

            model.UiScale = 1.25f;
            model.PersistIfSettled(FullDebounce);

            Assert.Equal(2, store.Saves.Count);
            Assert.Equal(1.25f, store.Saves[1].UiScale);
        }

        [Fact]
        public void SaveNow_PendingChange_FlushesImmediately()
        {
            SettingsModel model = CreateModel(out FakeSettingsStore store);

            model.Theme = LibraryConfig.DarkThemeName;
            model.SaveNow();

            Assert.False(model.PersistPending);
            LibrarySettings saved = Assert.Single(store.Saves);
            Assert.Equal(LibraryConfig.DarkThemeName, saved.Theme);

            // A later tick must not save a second time.
            model.PersistIfSettled(FullDebounce);
            Assert.Single(store.Saves);
        }

        [Fact]
        public void SaveNow_NothingPending_IsNoOp()
        {
            SettingsModel model = CreateModel(out FakeSettingsStore store);

            model.SaveNow();

            Assert.False(model.PersistPending);
            Assert.Empty(store.Saves);
        }
    }
}
