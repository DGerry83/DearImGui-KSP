using DearImGuiKSP;
using DearImGuiKSP.Application;
using Xunit;

namespace Application.Tests
{
    /// <summary>
    /// DockingModeApplier dirty-flag tests (ISSUES #011), mirroring
    /// <see cref="Theming.ThemeEngineTests"/>: a docking change arms the deferred
    /// apply (<see cref="DockingModeApplier.HasPendingApply"/>; the apply itself
    /// reaches native code and is proven by the native harness). Unrelated
    /// settings — theme, uiScale, font, fontScale, verboseLogging — must NOT arm
    /// it, and re-setting docking to its current value fires no change event at
    /// all.
    /// </summary>
    public class DockingModeApplierTests
    {
        private static DockingModeApplier CreateApplier(SettingsModel settings)
        {
            return new DockingModeApplier(settings, new FakeLogger());
        }

        [Fact]
        public void Initially_NoPendingApply()
        {
            var applier = CreateApplier(new SettingsModel(new FakeSettingsStore()));

            Assert.False(applier.HasPendingApply);
        }

        [Fact]
        public void DockingChange_SetsPendingApply()
        {
            var settings = new SettingsModel(new FakeSettingsStore());
            var applier = CreateApplier(settings);

            settings.Docking = false;

            Assert.True(applier.HasPendingApply);
        }

        [Fact]
        public void DockingSetToSameValue_DoesNotSetPendingApply()
        {
            var settings = new SettingsModel(new FakeSettingsStore());
            var applier = CreateApplier(settings);

            settings.Docking = LibraryConfig.DefaultDocking; // already the default: no-op

            Assert.False(applier.HasPendingApply);
        }

        [Fact]
        public void ThemeChange_DoesNotSetPendingApply()
        {
            var settings = new SettingsModel(new FakeSettingsStore());
            var applier = CreateApplier(settings);

            settings.Theme = LibraryConfig.DarkThemeName;

            Assert.False(applier.HasPendingApply);
        }

        [Fact]
        public void UiScaleChange_DoesNotSetPendingApply()
        {
            var settings = new SettingsModel(new FakeSettingsStore());
            var applier = CreateApplier(settings);

            settings.UiScale = 1.4f;

            Assert.False(applier.HasPendingApply);
        }

        [Fact]
        public void FontChange_DoesNotSetPendingApply()
        {
            var settings = new SettingsModel(new FakeSettingsStore());
            var applier = CreateApplier(settings);

            settings.Font = LibraryConfig.EmbeddedFontName;

            Assert.False(applier.HasPendingApply);
        }

        [Fact]
        public void VerboseLoggingChange_DoesNotSetPendingApply()
        {
            var settings = new SettingsModel(new FakeSettingsStore());
            var applier = CreateApplier(settings);

            settings.VerboseLogging = true;

            Assert.False(applier.HasPendingApply);
        }
    }
}
