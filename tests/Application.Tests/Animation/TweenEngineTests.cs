using System;
using System.Collections.Generic;
using DearImGuiKSP;
using DearImGuiKSP.Application;
using DearImGuiKSP.Application.Animation;
using Xunit;
using Color = UnityEngine.Color;
using Facade = DearImGuiKSP.DearImGuiKSP;

namespace Application.Tests
{
    /// <summary>
    /// Tween engine tests (C14, spec §5.4): exact easing endpoints and closed-form
    /// midpoints, delta-time progression, exact completion/removal, cancel, mid-tick
    /// cancellation safety (self-cancel and sibling cancel), zero/negative duration,
    /// and the public Tween facade guards (availability races never throw).
    /// </summary>
    [Collection("FacadeStatics")]
    public class TweenEngineTests
    {
        private static readonly Ease[] AllEases =
        {
            Ease.Linear, Ease.QuadIn, Ease.QuadOut, Ease.QuadInOut,
            Ease.CubicIn, Ease.CubicOut, Ease.CubicInOut,
        };

        private sealed class FloatRecorder
        {
            public readonly List<float> Values = new List<float>();

            public void Record(float value)
            {
                Values.Add(value);
            }
        }

        [Fact]
        public void AllEaseValues_EndpointsAreExact()
        {
            foreach (Ease ease in AllEases)
            {
                Assert.Equal(0f, EaseFunctions.Evaluate(ease, 0f));
                Assert.Equal(1f, EaseFunctions.Evaluate(ease, 1f));
            }
        }

        [Fact]
        public void AllEaseValues_MidpointsMatchClosedForm()
        {
            const float tolerance = 1e-6f;
            Assert.Equal(0.5f, EaseFunctions.Evaluate(Ease.Linear, 0.5f), 6);
            Assert.Equal(0.25f, EaseFunctions.Evaluate(Ease.QuadIn, 0.5f), 6);
            Assert.Equal(0.75f, EaseFunctions.Evaluate(Ease.QuadOut, 0.5f), 6);
            Assert.Equal(0.5f, EaseFunctions.Evaluate(Ease.QuadInOut, 0.5f), 6);
            Assert.Equal(0.125f, EaseFunctions.Evaluate(Ease.CubicIn, 0.5f), 6);
            Assert.Equal(0.875f, EaseFunctions.Evaluate(Ease.CubicOut, 0.5f), 6);
            Assert.Equal(0.5f, EaseFunctions.Evaluate(Ease.CubicInOut, 0.5f), 6);
            Assert.InRange(
                Math.Abs(EaseFunctions.Evaluate(Ease.QuadInOut, 0.25f) - 0.125f), 0f, tolerance);
        }

        [Fact]
        public void Progression_TwoHalfDurationTicks_CompleteWithExactTo()
        {
            var engine = new TweenEngine();
            var recorder = new FloatRecorder();

            TweenHandle handle = engine.StartFloat(recorder.Record, 0f, 10f, 2f, Ease.Linear);

            engine.Tick(1f);
            Assert.True(handle.IsPlaying);
            Assert.Equal(5f, recorder.Values[0]);

            engine.Tick(1f);
            Assert.Equal(10f, recorder.Values[1]);
            Assert.False(handle.IsPlaying);
        }

        [Fact]
        public void Completion_RemovesTween_AndStopsInvoking()
        {
            var engine = new TweenEngine();
            var recorder = new FloatRecorder();

            TweenHandle handle = engine.StartFloat(recorder.Record, 0f, 1f, 1f, Ease.QuadInOut);
            engine.Tick(2f); // overshoot: clamps to t = 1

            Assert.Single(recorder.Values);
            Assert.Equal(1f, recorder.Values[0]);
            Assert.False(handle.IsPlaying);

            engine.Tick(1f);
            Assert.Single(recorder.Values); // removed: nothing more fires
        }

        [Fact]
        public void Cancel_StopsSetterAndIsPlayingFalse()
        {
            var engine = new TweenEngine();
            var recorder = new FloatRecorder();

            TweenHandle handle = engine.StartFloat(recorder.Record, 0f, 1f, 1f, Ease.Linear);
            engine.Tick(0.5f);
            handle.Cancel();

            Assert.False(handle.IsPlaying);
            engine.Tick(0.5f);
            Assert.Single(recorder.Values); // the cancel-frame value only
            Assert.Equal(0.5f, recorder.Values[0]);
        }

        [Fact]
        public void Tick_WithNoLiveTweens_InvokesNothing()
        {
            var engine = new TweenEngine();
            engine.Tick(1f / 60f); // steady state: a single Count check, no throw
        }

        [Fact]
        public void SelfCancelFromSetter_DoesNotSkipSibling()
        {
            var engine = new TweenEngine();
            var sibling = new FloatRecorder();
            TweenHandle self = default(TweenHandle);
            int selfCalls = 0;

            self = engine.StartFloat(
                v =>
                {
                    selfCalls++;
                    self.Cancel(); // cancels itself mid-tick
                },
                0f, 1f, 1f, Ease.Linear);
            TweenHandle siblingHandle = engine.StartFloat(sibling.Record, 0f, 1f, 1f, Ease.Linear);

            engine.Tick(0.5f);

            Assert.Equal(1, selfCalls); // not re-invoked after its own cancel
            Assert.Single(sibling.Values); // sibling neither skipped nor double-invoked
            Assert.Equal(0.5f, sibling.Values[0]);
            Assert.False(self.IsPlaying);
            Assert.True(siblingHandle.IsPlaying);
        }

        [Fact]
        public void CancelSiblingFromSetter_SiblingNotInvokedThatTick()
        {
            var engine = new TweenEngine();
            var recorderA = new FloatRecorder();
            var recorderB = new FloatRecorder();
            TweenHandle handleB = default(TweenHandle);

            // A is earlier in the list: its setter cancels B before B's turn.
            engine.StartFloat(
                v =>
                {
                    recorderA.Record(v);
                    handleB.Cancel();
                },
                0f, 1f, 1f, Ease.Linear);
            handleB = engine.StartFloat(recorderB.Record, 0f, 1f, 1f, Ease.Linear);

            engine.Tick(0.5f);

            Assert.Single(recorderA.Values);
            Assert.Empty(recorderB.Values); // cancelled before its turn: no invocation
            Assert.False(handleB.IsPlaying);
        }

        [Fact]
        public void NonPositiveSeconds_CompletesOnFirstTick_WithExactTo()
        {
            var engine = new TweenEngine();
            var recorder = new FloatRecorder();

            TweenHandle handle = engine.StartFloat(recorder.Record, 3f, 7f, 0f, Ease.Linear);
            engine.Tick(1f / 60f);

            Assert.Single(recorder.Values);
            Assert.Equal(7f, recorder.Values[0]);
            Assert.False(handle.IsPlaying);
        }

        [Fact]
        public void ColorTween_LerpsRgbaByEasedT_WithExactEndpoints()
        {
            var engine = new TweenEngine();
            var colors = new List<Color>();
            var from = new Color(0f, 0f, 0f, 0f);
            var to = new Color(1f, 0.5f, 0.25f, 1f);

            TweenHandle handle = engine.StartColor(colors.Add, from, to, 2f, Ease.Linear);
            engine.Tick(1f);

            Assert.Single(colors);
            Assert.Equal(0.5f, colors[0].r, 6);
            Assert.Equal(0.25f, colors[0].g, 6);
            Assert.Equal(0.125f, colors[0].b, 6);
            Assert.Equal(0.5f, colors[0].a, 6);

            engine.Tick(1f);
            Assert.Equal(to, colors[1]); // exact target, not lerp arithmetic residue
            Assert.False(handle.IsPlaying);
        }

        [Fact]
        public void To_WithNullSetter_ThrowsArgumentNullException()
        {
            var engine = new TweenEngine();
            WithFacadeWiring(engine, () =>
            {
                Assert.Throws<ArgumentNullException>(() => Tween.To((Action<float>)null, 0f, 1f, 1f, Ease.Linear));
            });
        }

        [Fact]
        public void To_BeforeWiring_ReturnsInertHandle_AndDoesNotThrow()
        {
            var logger = new FakeLogger();
            WithFacadeWiring(null, () =>
            {
                Facade.Log = logger;

                bool invoked = false;
                TweenHandle handle = Tween.To(v => invoked = true, 0f, 1f, 1f, Ease.Linear);

                Assert.False(handle.IsPlaying);
                handle.Cancel(); // no-op, must not throw
                Assert.False(invoked);
                Assert.Single(logger.Warnings);
            });
        }

        [Fact]
        public void To_WhenAvailable_InvokesBaselineAndTicks()
        {
            var engine = new TweenEngine();
            WithFacadeWiring(engine, () =>
            {
                var recorder = new FloatRecorder();

                TweenHandle handle = Tween.To(recorder.Record, 0f, 4f, 1f, Ease.Linear);

                Assert.Single(recorder.Values); // immediate t = 0 baseline
                Assert.Equal(0f, recorder.Values[0]);
                Assert.True(handle.IsPlaying);

                engine.Tick(0.5f);
                Assert.Equal(2f, recorder.Values[1]);

                engine.Tick(0.5f);
                Assert.Equal(4f, recorder.Values[2]);
                Assert.False(handle.IsPlaying);
            });
        }

        /// <summary>
        /// Pins the facade statics (Tween.Engine, Facade.Lifecycle/Log) to a
        /// known state for the duration of <paramref name="body"/>, restoring the
        /// previous values afterwards so other tests are unaffected.
        /// </summary>
        private static void WithFacadeWiring(TweenEngine engine, Action body)
        {
            TweenEngine previousEngine = Tween.Engine;
            var previousLifecycle = Facade.Lifecycle;
            var previousLog = Facade.Log;
            try
            {
                Tween.Engine = engine;
                if (engine != null)
                {
                    var machine = new LifecycleStateMachine(new FakeLogger());
                    machine.MarkInitializing();
                    machine.MarkRunning();
                    Facade.Lifecycle = machine;
                }
                else
                {
                    Facade.Lifecycle = null;
                }
                body();
            }
            finally
            {
                Tween.Engine = previousEngine;
                Facade.Lifecycle = previousLifecycle;
                Facade.Log = previousLog;
            }
        }
    }
}
