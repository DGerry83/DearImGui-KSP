using DearImGuiKSP;
using DearImGuiKSP.Application;
using DearImGuiKSP.Infrastructure;
using Xunit;
using Color32 = UnityEngine.Color32;

namespace Application.Tests
{
    /// <summary>
    /// G3-27 pure-helper coverage: gradient color math (Pack/Lerp/Lighten, spec
    /// §6.1 technique D28), the player-facing failure-body mapping (spec §7.1),
    /// and the native init-result → failure-kind mapping (spec §5.4 popup path).
    /// No facade statics and no native code: every assertion is pure struct or
    /// string logic, so this class needs no "FacadeStatics" collection.
    /// </summary>
    public class HelperTests
    {
        // ---- ImGuiGradients.Pack: IM_COL32 layout (imgui.h: A<<24 | B<<16 | G<<8 | R) ----

        [Fact]
        public void Pack_ChannelsLandLittleEndianRgba()
        {
            // 0x04030201, not 0x01020304: a BGR<->RGB swap or big-endian pack fails here.
            Assert.Equal(0x04030201u, ImGuiGradients.Pack(new Color32(1, 2, 3, 4)));
        }

        [Fact]
        public void Pack_KnownColorsMatchImCol32()
        {
            Assert.Equal(0xFF0000FFu, ImGuiGradients.Pack(new Color32(255, 0, 0, 255))); // IM_COL32 red
            Assert.Equal(0xFF00FF00u, ImGuiGradients.Pack(new Color32(0, 255, 0, 255))); // IM_COL32 green
            Assert.Equal(0x800000FFu, ImGuiGradients.Pack(new Color32(255, 0, 0, 128))); // alpha in the top byte
            Assert.Equal(0u, ImGuiGradients.Pack(new Color32(0, 0, 0, 0)));
            Assert.Equal(0xFFFFFFFFu, ImGuiGradients.Pack(new Color32(255, 255, 255, 255)));
        }

        // ---- ImGuiGradients.Lerp: per-channel, alpha included, truncating ----

        [Fact]
        public void Lerp_Endpoints_AreExact()
        {
            var a = new Color32(10, 20, 30, 40);
            var b = new Color32(110, 120, 130, 140);

            Assert.Equal(a, ImGuiGradients.Lerp(a, b, 0f));
            Assert.Equal(b, ImGuiGradients.Lerp(a, b, 1f));
        }

        [Fact]
        public void Lerp_Midpoint_TruncatesTowardZeroPerChannel()
        {
            // (byte)(255 * 0.5f) = 127, not 128: pins truncation, not rounding.
            var mid = ImGuiGradients.Lerp(
                new Color32(0, 0, 0, 0), new Color32(255, 255, 255, 255), 0.5f);

            Assert.Equal(new Color32(127, 127, 127, 127), mid); // alpha lerps too
        }

        [Fact]
        public void Lerp_QuarterStep_IsPerChannelIndependent()
        {
            var mid = ImGuiGradients.Lerp(
                new Color32(10, 20, 30, 40), new Color32(110, 120, 130, 140), 0.25f);

            Assert.Equal(new Color32(35, 45, 55, 65), mid);
        }

        [Fact]
        public void Lerp_ReverseDirection_TruncatesTheSameWay()
        {
            var mid = ImGuiGradients.Lerp(
                new Color32(255, 255, 255, 255), new Color32(0, 0, 0, 0), 0.5f);

            Assert.Equal(new Color32(127, 127, 127, 127), mid);
        }

        // ---- ImGuiGradients.Lighten: lerp toward opaque white ----

        [Fact]
        public void Lighten_ZeroAmount_IsIdentity()
        {
            var c = new Color32(12, 34, 56, 78);

            Assert.Equal(c, ImGuiGradients.Lighten(c, 0f));
        }

        [Fact]
        public void Lighten_FullAmount_IsOpaqueWhite()
        {
            Assert.Equal(
                new Color32(255, 255, 255, 255),
                ImGuiGradients.Lighten(new Color32(12, 34, 56, 78), 1f));
        }

        [Fact]
        public void Lighten_Half_BrightensEveryChannelTowardWhite()
        {
            // Lerp((0,0,0,0) -> (255,255,255,255), 0.5): midpoint, alpha included.
            Assert.Equal(
                new Color32(127, 127, 127, 127),
                ImGuiGradients.Lighten(new Color32(0, 0, 0, 0), 0.5f));
        }

        // ---- FailureText.BodyFor: §7.1 kind → body table (C13 render-hook mapping) ----

        [Fact]
        public void BodyFor_VersionMismatch_UsesVersionBody()
        {
            Assert.Equal(
                "DearImGui-KSP could not start because its components are from different versions. " +
                "Mods that depend on DearImGui-KSP will not work. See KSP.log for details.",
                FailureText.BodyFor(FailureKind.VersionMismatch));
        }

        [Fact]
        public void BodyFor_GraphicsApi_UsesGraphicsBody()
        {
            Assert.Equal(
                "DearImGui-KSP could not start because this graphics API is not supported. " +
                "Mods that depend on DearImGui-KSP will not work. See KSP.log for details.",
                FailureText.BodyFor(FailureKind.GraphicsApi));
        }

        [Fact]
        public void BodyFor_NativeComponent_UsesNativeBody()
        {
            Assert.Equal(FailureText.DK_FailNative, FailureText.BodyFor(FailureKind.NativeComponent));
        }

        [Fact]
        public void BodyFor_RenderHook_MapsToNativeBody()
        {
            // C13 contract decision: §7.1 defines only three bodies; the render
            // hook is part of the native/render component. A future fourth body
            // for RenderHook is a deliberate change and must trip this pin.
            Assert.Equal(FailureText.DK_FailNative, FailureText.BodyFor(FailureKind.RenderHook));
        }

        [Fact]
        public void BodyFor_AllKinds_ReturnNonEmptyDistinctBodies()
        {
            Assert.NotEqual(FailureText.DK_FailNative, FailureText.DK_FailGraphics);
            Assert.NotEqual(FailureText.DK_FailNative, FailureText.DK_FailVersion);
            Assert.NotEqual(FailureText.DK_FailGraphics, FailureText.DK_FailVersion);

            foreach (FailureKind kind in System.Enum.GetValues(typeof(FailureKind)))
            {
                Assert.False(string.IsNullOrEmpty(FailureText.BodyFor(kind)), "empty body for " + kind);
            }
        }

        // ---- NativeBridge.KindForInitResult: init code → popup failure kind ----

        [Fact]
        public void KindForInitResult_SuccessMapsToNativeComponent()
        {
            Assert.Equal(0, NativeBridge.InitOk); // the native protocol pins 0 = success
            Assert.Equal(FailureKind.NativeComponent, NativeBridge.KindForInitResult(NativeBridge.InitOk));
        }

        [Theory]
        [InlineData(NativeBridge.InitErrVersionMismatch, 1)] // FailureKind.VersionMismatch
        [InlineData(NativeBridge.InitErrUnsupportedDevice, 2)] // FailureKind.GraphicsApi
        [InlineData(NativeBridge.InitErrRenderHook, 3)] // FailureKind.RenderHook
        public void KindForInitResult_DistinctFailures_MapToTheirKinds(int result, int expectedKindValue)
        {
            Assert.Equal((FailureKind)expectedKindValue, NativeBridge.KindForInitResult(result));
        }

        [Theory]
        [InlineData(NativeBridge.InitErrSetDllDirectory)]
        [InlineData(NativeBridge.InitErrLoadLibrary)]
        [InlineData(NativeBridge.InitErrMissingExport)]
        [InlineData(NativeBridge.InitErrContextInit)]
        [InlineData(NativeBridge.InitErrDeviceTexture)]
        [InlineData(999)] // any unknown code: never a null/default enum escape
        public void KindForInitResult_AllOtherCodes_MapToNativeComponent(int result)
        {
            Assert.Equal(FailureKind.NativeComponent, NativeBridge.KindForInitResult(result));
        }
    }
}
