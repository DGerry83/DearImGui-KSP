using DearImGuiKSP.Application.Interfaces;
using UnityEngine;

namespace DearImGuiKSP.Infrastructure
{
    /// <summary>
    /// Implements <see cref="IImguiEventEaterGateway"/> with an IMGUI capture
    /// grab (ISSUES #003, mechanism v2). While a shield is on, every OnGUI pass
    /// — including Layout and Repaint, which run every frame long before any
    /// click's MouseDown arrives — assigns the shield's capture primitive to
    /// our control id: <see cref="GUIUtility.hotControl"/> for the mouse shield
    /// and <see cref="GUIUtility.keyboardControl"/> for the keyboard shield.
    /// IMGUI's own per-control event filter (Event.GetTypeForControl) then
    /// reports EventType.Used to every control except the hot one — this is
    /// how IMGUI itself blocks controls behind an active drag, so the block
    /// does not depend on OnGUI dispatch order across mods. v1 relied on
    /// Event.current.Use() from an early-ordered OnGUI, but that starves later
    /// handlers only if our OnGUI runs first in that dispatch — ordering across
    /// runtime-loaded assemblies proved unreliable (G1 FAIL), so v1 alone
    /// could not work. v2 keeps the Use() eating as belt-and-suspenders for
    /// the case where our OnGUI does run first.
    /// Release guard: when a shield is off, the capture primitive is reset to 0
    /// only if it is still ours, on every pass (idempotent) — so a missed
    /// transition can never permanently deaden IMGUI, and with both shields
    /// off the eater does nothing (IMGUI bit-identical to a library-less
    /// install). Shields are driven transition-only by InputCaptureTracker,
    /// and ReleaseAll clears both (F2 hide, loading, failure paths grab and
    /// eat nothing). Retained edge case: an IMGUI control already hot (drag
    /// in progress) when the mouse shield goes up has its hotControl clobbered
    /// and its drag cancels mid-way — eating/grabbing cannot let it finish.
    /// Layout/Repaint events are never Use()d (that would break all IMGUI
    /// rendering); EventType.Used events pass through harmlessly. Created in
    /// Composition.WireLifecycle with DontDestroyOnLoad, before frames run.
    /// </summary>
    [DefaultExecutionOrder(ExecutionOrder)]
    internal sealed class ImguiEventEaterGateway : MonoBehaviour, IImguiEventEaterGateway
    {
        // Forces our OnGUI ahead of other mods' OnGUI handlers where Unity
        // honors script-execution order, so the retained v1 Use() eating still
        // starves them on the first dispatch. The v2 grab no longer depends on
        // this ordering.
        private const int ExecutionOrder = -32000;

        // IMGUI control ids handed out by GUIUtility.GetControlID are sequential
        // per event starting near zero, so even control-heavy mods stay orders
        // of magnitude below this; 0 means "no control", so it cannot be ours.
        private const int ShieldControlId = int.MaxValue - 1024;

        private bool _mouseShielded;
        private bool _keyboardShielded;

        /// <inheritdoc/>
        public void SetMouseShielded(bool shielded)
        {
            _mouseShielded = shielded;
        }

        /// <inheritdoc/>
        public void SetKeyboardShielded(bool shielded)
        {
            _keyboardShielded = shielded;
        }

        private void OnGUI()
        {
            // The grab must run on EVERY pass, including Layout/Repaint: those
            // events are dispatched every frame while the pointer is over the
            // game window, so the grab lands long before any click's MouseDown
            // and IMGUI's own GetTypeForControl filter blocks the click no
            // matter whose OnGUI runs first.
            if (_mouseShielded)
            {
                GUIUtility.hotControl = ShieldControlId;
            }
            else if (GUIUtility.hotControl == ShieldControlId)
            {
                GUIUtility.hotControl = 0; // release iff still ours, idempotent
            }

            if (_keyboardShielded)
            {
                GUIUtility.keyboardControl = ShieldControlId;
            }
            else if (GUIUtility.keyboardControl == ShieldControlId)
            {
                GUIUtility.keyboardControl = 0; // release iff still ours
            }

            Event e = Event.current;

            // Layout/Repaint must reach every IMGUI handler or all IMGUI
            // rendering breaks; a Used event re-passed here is harmless.
            if (e.type == EventType.Layout || e.type == EventType.Repaint || e.type == EventType.Used)
            {
                return;
            }

            // Retained v1 belt-and-suspenders: if our OnGUI did run first in
            // this dispatch, starving the event here hides it even from any
            // control that ignores its GetTypeForControl filter.
            if (_mouseShielded && (e.isMouse || e.type == EventType.ScrollWheel))
            {
                e.Use();
            }
            else if (_keyboardShielded && e.isKey)
            {
                e.Use();
            }
        }
    }
}
