using System;
using DearImGuiKSP;
using DearImGuiKSP.Application;
using DearImGuiKSP.Application.Animation;
using Xunit;
using Facade = DearImGuiKSP.DearImGuiKSP;

namespace Application.Tests
{
    /// <summary>
    /// C02 tween fault containment (S3, G3-07): a throwing tween setter kills only
    /// its own tween and never escapes Tick; a throwing baseline set(from) in
    /// Tween.To is contained and the tween is not started.
    /// </summary>
    [Collection("FacadeStatics")]
    public class TweenFaultTests
    {
        [Fact]
        public void ThrowingSetter_KillsOnlyThatTween_SiblingsKeepRunning()
        {
            var logger = new FakeLogger();
            var engine = new TweenEngine(logger);
            int throwerCalls = 0;
            float sibling = 0f;

            TweenHandle bad = engine.StartFloat(
                v => { throwerCalls++; throw new InvalidOperationException("boom"); },
                0f, 1f, 10f, Ease.Linear);
            TweenHandle good = engine.StartFloat(v => sibling = v, 0f, 1f, 1f, Ease.Linear);

            engine.Tick(0.1f);
            engine.Tick(0.1f);

            Assert.Equal(1, throwerCalls); // stopped after the first throw, not retried
            Assert.False(bad.IsPlaying);
            Assert.True(good.IsPlaying);
            Assert.True(sibling > 0f);
            Assert.NotEmpty(logger.Errors);
        }

        [Fact]
        public void ThrowingSetter_OnFinalTick_StillSwept()
        {
            var engine = new TweenEngine(new FakeLogger());
            int calls = 0;
            TweenHandle handle = engine.StartFloat(
                v => { calls++; throw new InvalidOperationException("boom"); },
                0f, 1f, 0f, Ease.Linear); // zero duration: completes first tick

            engine.Tick(1f);

            Assert.Equal(1, calls);
            Assert.False(handle.IsPlaying);
        }

        [Fact]
        public void BaselineSetter_Throw_IsContained_TweenNotStarted()
        {
            TweenEngine previousEngine = Tween.Engine;
            var previousLifecycle = Facade.Lifecycle;
            var previousLog = Facade.Log;
            try
            {
                var logger = new FakeLogger();
                var engine = new TweenEngine(logger);
                var machine = new LifecycleStateMachine(new FakeLogger());
                machine.MarkInitializing();
                machine.MarkRunning();
                Tween.Engine = engine;
                Facade.Lifecycle = machine;
                Facade.Log = logger;

                int calls = 0;
                TweenHandle handle = Tween.To(
                    v => { calls++; throw new InvalidOperationException("boom"); },
                    0f, 1f, 1f, Ease.Linear);

                Assert.Equal(1, calls);            // baseline ran once...
                Assert.False(handle.IsPlaying);    // ...but the tween never started
                Assert.NotEmpty(logger.Errors);

                // The engine stays clean: a later well-behaved tween ticks normally.
                float value = 0f;
                Tween.To(v => value = v, 2f, 4f, 1f, Ease.Linear);
                engine.Tick(0.5f);
                Assert.True(value > 2f);
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
