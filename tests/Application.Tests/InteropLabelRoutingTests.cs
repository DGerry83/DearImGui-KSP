using System;
using System.Reflection;
using DearImGuiKSP.Interop;
using Xunit;

namespace Application.Tests
{
    /// <summary>
    /// C14 closure of C08's recorded G3-20 gap: every ID-bearing label encode in
    /// <see cref="ExtensionShimsNative"/> (Toggle/Knob/Wheel) and
    /// <see cref="ImPlotNative"/> (BeginPlot/PlotLine/BeginSubplots) must route
    /// through <see cref="ImGuiInternal.ToIdUtf8"/>, whose null/empty →
    /// "##dk_empty_id" sentinel behavior is pinned behaviorally in
    /// WidgetGuardTests (docs/10 §6: every library widget guards the empty-label
    /// collision with the window's own ImGui ID).
    /// The wrappers P/Invoke immediately after encoding, so the net48 test
    /// runner cannot invoke them (same constraint C07/C08 recorded). Instead the
    /// routing is pinned at the metadata level: each wrapper's IL must contain a
    /// call to ToIdUtf8's method token, and must NOT fall back to a local
    /// unguarded ToUtf8. Reverting any call site to the old encode fails here
    /// for a real regression, not tautologically. Format strings (Knob/Wheel
    /// printf formats) are display text, never IDs, and must stay on plain
    /// ToUtf8 — pinned in the opposite direction so the guard can't leak there.
    /// No facade statics are touched.
    /// </summary>
    public class InteropLabelRoutingTests
    {
        private const BindingFlags WrapperFlags = BindingFlags.Static | BindingFlags.NonPublic;

        private static readonly int ToIdUtf8Token = typeof(ImGuiInternal)
            .GetMethod("ToIdUtf8", WrapperFlags)
            .MetadataToken;

        private static MethodInfo ShimMethod(string name, params Type[] parameterTypes)
        {
            MethodInfo method = typeof(ExtensionShimsNative).GetMethod(name, WrapperFlags, null, parameterTypes, null);
            Assert.True(method != null, "ExtensionShimsNative." + name + " not found");
            return method;
        }

        private static bool ContainsToken(MethodInfo method, int token)
        {
            byte[] il = method.GetMethodBody().GetILAsByteArray();
            return IndexOfToken(il, token) >= 0;
        }

        private static int IndexOfToken(byte[] il, int token)
        {
            byte b0 = (byte)token, b1 = (byte)(token >> 8), b2 = (byte)(token >> 16), b3 = (byte)(token >> 24);
            for (int i = 0; i + 4 <= il.Length; i++)
            {
                if (il[i] == b0 && il[i + 1] == b1 && il[i + 2] == b2 && il[i + 3] == b3)
                {
                    return i;
                }
            }
            return -1;
        }

        private static void AssertRoutesLabelThroughToIdUtf8(MethodInfo wrapper, int? forbiddenLocalToken)
        {
            Assert.True(
                ContainsToken(wrapper, ToIdUtf8Token),
                wrapper.DeclaringType.Name + "." + wrapper.Name +
                " must encode its ID-bearing label via ImGuiInternal.ToIdUtf8 (empty-label sentinel guard)");
            if (forbiddenLocalToken.HasValue)
            {
                Assert.False(
                    ContainsToken(wrapper, forbiddenLocalToken.Value),
                    wrapper.DeclaringType.Name + "." + wrapper.Name +
                    " routes an ID-bearing label through the unguarded local ToUtf8");
            }
        }

        // ---- ExtensionShimsNative: all six wrappers, formats stay plain ----

        [Fact]
        public void ToggleOverloads_Labels_RouteThroughToIdUtf8()
        {
            int localToUtf8 = typeof(ExtensionShimsNative)
                .GetMethod("ToUtf8", WrapperFlags)
                .MetadataToken;

            AssertRoutesLabelThroughToIdUtf8(
                ShimMethod("Toggle", typeof(string), typeof(bool).MakeByRefType()), localToUtf8);
            AssertRoutesLabelThroughToIdUtf8(
                ShimMethod("Toggle", typeof(string), typeof(bool).MakeByRefType(), typeof(int)), localToUtf8);
        }

        [Fact]
        public void KnobAndWheel_Labels_RouteThroughToIdUtf8_FormatsStayPlain()
        {
            int localToUtf8 = typeof(ExtensionShimsNative)
                .GetMethod("ToUtf8", WrapperFlags)
                .MetadataToken;
            int toUtf8OrNull = typeof(ExtensionShimsNative)
                .GetMethod("ToUtf8OrNull", WrapperFlags)
                .MetadataToken;

            MethodInfo[] wrappers =
            {
                ShimMethod("Knob", typeof(string), typeof(float).MakeByRefType(),
                    typeof(float), typeof(float), typeof(float), typeof(string),
                    typeof(int), typeof(float), typeof(int), typeof(int)),
                ShimMethod("KnobInt", typeof(string), typeof(int).MakeByRefType(),
                    typeof(int), typeof(int), typeof(float), typeof(string),
                    typeof(int), typeof(float), typeof(int), typeof(int)),
                ShimMethod("WheelFloat", typeof(string), typeof(float).MakeByRefType(),
                    typeof(float), typeof(float), typeof(float), typeof(float),
                    typeof(int), typeof(string), typeof(float), typeof(bool)),
                ShimMethod("WheelInt", typeof(string), typeof(int).MakeByRefType(),
                    typeof(int), typeof(int), typeof(float), typeof(float),
                    typeof(int), typeof(string), typeof(float), typeof(bool)),
            };

            foreach (MethodInfo wrapper in wrappers)
            {
                AssertRoutesLabelThroughToIdUtf8(wrapper, localToUtf8);
                // The printf format path is still wired to the local encoder.
                Assert.True(ContainsToken(wrapper, toUtf8OrNull),
                    wrapper.Name + " must keep encoding its printf format via ToUtf8OrNull");
            }
        }

        [Fact]
        public void ShimFormatHelper_DoesNotUseTheIdGuard()
        {
            // ToUtf8OrNull feeds native varargs printf formats — display text,
            // never an item ID. The sentinel must not leak into it.
            MethodInfo formatHelper = ShimMethod("ToUtf8OrNull", typeof(string));

            Assert.False(ContainsToken(formatHelper, ToIdUtf8Token),
                "printf format strings must not go through ToIdUtf8");
        }

        // ---- ImPlotNative: plot/subplot identity and series labels ----

        [Fact]
        public void ImPlotIdentityLabels_RouteThroughToIdUtf8()
        {
            MethodInfo beginPlot = typeof(ImPlotNative).GetMethod(
                "BeginPlot", WrapperFlags, null,
                new[] { typeof(string), typeof(ImVec2), typeof(ImPlotFlags) }, null);
            MethodInfo beginSubplots = typeof(ImPlotNative).GetMethod(
                "BeginSubplots", WrapperFlags, null,
                new[] { typeof(string), typeof(int), typeof(int), typeof(ImVec2), typeof(ImPlotSubplotFlags) }, null);

            Assert.NotNull(beginPlot);
            Assert.NotNull(beginSubplots);
            AssertRoutesLabelThroughToIdUtf8(beginPlot, null);
            AssertRoutesLabelThroughToIdUtf8(beginSubplots, null);
        }

        [Fact]
        public void ImPlotLineOverloads_RouteSeriesLabelsThroughToIdUtf8()
        {
            // Both PlotLine overloads take an unsafe float*/double* — referenced
            // via reflection only, so no unsafe context is needed here.
            MethodInfo[] overloads = Array.FindAll(
                typeof(ImPlotNative).GetMethods(WrapperFlags),
                m => m.Name == "PlotLine" && m.GetParameters().Length == 5);

            Assert.Equal(2, overloads.Length);
            foreach (MethodInfo plotLine in overloads)
            {
                Assert.True(plotLine.GetParameters()[1].ParameterType.IsPointer,
                    "expected the pinned-values pointer parameter");
                AssertRoutesLabelThroughToIdUtf8(plotLine, null);
            }
        }

        [Fact]
        public void ImPlotNative_HasNoLocalUnguardedToUtf8Anymore()
        {
            // The old private ToUtf8 had exactly the four ID-bearing call sites
            // now routed through ToIdUtf8; it must be gone, not silently re-used.
            Assert.Null(typeof(ImPlotNative).GetMethod("ToUtf8", WrapperFlags));
        }
    }
}
