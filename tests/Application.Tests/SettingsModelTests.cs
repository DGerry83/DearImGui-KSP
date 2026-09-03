using DearImGuiKSP;
using DearImGuiKSP.Application;
using Xunit;

namespace Application.Tests
{
    public class SettingsModelTests
    {
        private static SettingsModel CreateModel(out FakeSettingsStore store)
        {
            store = new FakeSettingsStore();
            return new SettingsModel(store);
        }

        [Fact]
        public void Constructor_LoadsDefaultsFromLibraryConfig()
        {
            SettingsModel model = CreateModel(out FakeSettingsStore _);

            Assert.Equal(LibraryConfig.DefaultUiScale, model.UiScale);
            Assert.Equal(LibraryConfig.DefaultFontScale, model.FontScale);
            Assert.Equal(LibraryConfig.DefaultTheme, model.Theme);
            Assert.Equal(LibraryConfig.DefaultVerboseLogging, model.VerboseLogging);
            Assert.Equal(LibraryConfig.DefaultEnabled, model.Enabled);
            Assert.Equal(LibraryConfig.DefaultClampWindowsToViewport, model.ClampWindowsToViewport);
        }

        [Fact]
        public void Constructor_DoesNotPersist()
        {
            CreateModel(out FakeSettingsStore store);

            Assert.Empty(store.Saves);
        }

        [Fact]
        public void Constructor_ClampsLoadedScale()
        {
            var store = new FakeSettingsStore();
            store.Loaded.UiScale = 99f;
            store.Loaded.FontScale = 0.01f;
            var model = new SettingsModel(store);

            Assert.Equal(LibraryConfig.MaxScale, model.UiScale);
            Assert.Equal(LibraryConfig.MinScale, model.FontScale);
        }

        [Fact]
        public void UiScale_SetAboveMax_ClampsToMax()
        {
            SettingsModel model = CreateModel(out FakeSettingsStore _);

            model.UiScale = 10f;

            Assert.Equal(LibraryConfig.MaxScale, model.UiScale);
        }

        [Fact]
        public void UiScale_SetBelowMin_ClampsToMin()
        {
            SettingsModel model = CreateModel(out FakeSettingsStore _);

            model.UiScale = 0.1f;

            Assert.Equal(LibraryConfig.MinScale, model.UiScale);
        }

        [Fact]
        public void FontScale_SetOutOfRange_Clamps()
        {
            SettingsModel model = CreateModel(out FakeSettingsStore _);

            model.FontScale = -3f;
            Assert.Equal(LibraryConfig.MinScale, model.FontScale);

            model.FontScale = 42f;
            Assert.Equal(LibraryConfig.MaxScale, model.FontScale);
        }

        [Fact]
        public void Set_SameValue_FiresNoChangeNotification_AndDoesNotPersist()
        {
            SettingsModel model = CreateModel(out FakeSettingsStore store);
            int changeCount = 0;
            model.Changed += () => changeCount++;

            model.UiScale = model.UiScale;
            model.ClampWindowsToViewport = model.ClampWindowsToViewport;

            Assert.Equal(0, changeCount);
            Assert.Empty(store.Saves);
        }

        [Fact]
        public void Set_NewValue_FiresChangedOnce_AndPersistsOnce()
        {
            SettingsModel model = CreateModel(out FakeSettingsStore store);
            int changeCount = 0;
            model.Changed += () => changeCount++;

            model.UiScale = 1.5f;

            Assert.Equal(1, changeCount);
            Assert.Single(store.Saves);
        }

        [Fact]
        public void PersistedSnapshot_ContainsAllCurrentValues()
        {
            SettingsModel model = CreateModel(out FakeSettingsStore store);
            model.ClampWindowsToViewport = false;
            model.VerboseLogging = true;

            LibrarySettings saved = store.Saves[store.Saves.Count - 1];
            Assert.False(saved.ClampWindowsToViewport);
            Assert.True(saved.VerboseLogging);
            Assert.Equal(model.UiScale, saved.UiScale);
            Assert.Equal(model.FontScale, saved.FontScale);
            Assert.Equal(model.Theme, saved.Theme);
            Assert.Equal(model.Enabled, saved.Enabled);
        }

        [Fact]
        public void ClampWindowsToViewport_DefaultsTrue_FromLibraryConfig()
        {
            SettingsModel model = CreateModel(out FakeSettingsStore _);

            Assert.True(model.ClampWindowsToViewport);
        }

        [Fact]
        public void ClampWindowsToViewport_SetFalse_Persists()
        {
            SettingsModel model = CreateModel(out FakeSettingsStore store);
            int changeCount = 0;
            model.Changed += () => changeCount++;

            model.ClampWindowsToViewport = false;

            Assert.False(model.ClampWindowsToViewport);
            Assert.Equal(1, changeCount);
            Assert.Single(store.Saves);
            Assert.False(store.Saves[0].ClampWindowsToViewport);
        }
    }
}
