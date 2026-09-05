using DearImGuiKSP;
using DearImGuiKSP.Application;
using Xunit;

namespace Application.Tests.Theming
{
    /// <summary>
    /// ThemeEngine dirty-flag tests (chunk C31): a theme change OR a uiScale
    /// change arms the deferred re-apply (<see cref="ThemeEngine.HasPendingApply"/>;
    /// the apply itself reaches native code and is proven by the native
    /// harness). Unrelated settings — font, fontScale, verboseLogging — must
    /// NOT arm it, and re-setting a value to its current value fires no change
    /// event at all.
    /// </summary>
    public class ThemeEngineTests
    {
        private static ThemeEngine CreateEngine(SettingsModel settings)
        {
            return new ThemeEngine(settings, new FakeLogger());
        }

        [Fact]
        public void Initially_NoPendingApply()
        {
            var engine = CreateEngine(new SettingsModel(new FakeSettingsStore()));

            Assert.False(engine.HasPendingApply);
        }

        [Fact]
        public void ThemeChange_SetsPendingApply()
        {
            var settings = new SettingsModel(new FakeSettingsStore());
            var engine = CreateEngine(settings);

            settings.Theme = LibraryConfig.DarkThemeName;

            Assert.True(engine.HasPendingApply);
        }

        [Fact]
        public void UiScaleChange_SetsPendingApply()
        {
            var settings = new SettingsModel(new FakeSettingsStore());
            var engine = CreateEngine(settings);

            settings.UiScale = 1.4f;

            Assert.True(engine.HasPendingApply);
        }

        [Fact]
        public void UiScaleChange_BelowRange_ClampsAndSetsPendingApply()
        {
            var settings = new SettingsModel(new FakeSettingsStore());
            var engine = CreateEngine(settings);

            settings.UiScale = 0.1f; // clamps to LibraryConfig.MinScale (0.5)

            Assert.Equal(LibraryConfig.MinScale, settings.UiScale);
            Assert.True(engine.HasPendingApply);
        }

        [Fact]
        public void UiScaleSetToSameValue_DoesNotSetPendingApply()
        {
            var settings = new SettingsModel(new FakeSettingsStore());
            var engine = CreateEngine(settings);

            settings.UiScale = LibraryConfig.DefaultUiScale; // already the default: no-op

            Assert.False(engine.HasPendingApply);
        }

        [Fact]
        public void FontChange_DoesNotSetPendingApply()
        {
            var settings = new SettingsModel(new FakeSettingsStore());
            var engine = CreateEngine(settings);

            settings.Font = LibraryConfig.EmbeddedFontName;

            Assert.False(engine.HasPendingApply);
        }

        [Fact]
        public void FontScaleChange_DoesNotSetPendingApply()
        {
            var settings = new SettingsModel(new FakeSettingsStore());
            var engine = CreateEngine(settings);

            settings.FontScale = 1.3f;

            Assert.False(engine.HasPendingApply);
        }

        [Fact]
        public void VerboseLoggingChange_DoesNotSetPendingApply()
        {
            var settings = new SettingsModel(new FakeSettingsStore());
            var engine = CreateEngine(settings);

            settings.VerboseLogging = true;

            Assert.False(engine.HasPendingApply);
        }
    }
}
