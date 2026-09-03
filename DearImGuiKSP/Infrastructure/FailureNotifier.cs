using DearImGuiKSP.Application;
using DearImGuiKSP.Application.Interfaces;
using UnityEngine;
using ILogger = DearImGuiKSP.Application.Interfaces.ILogger;

namespace DearImGuiKSP.Infrastructure
{
    /// <summary>
    /// Implements <see cref="IFailureNotifier"/> with one stock PopupDialog (spec §5.4, §7).
    /// Plain-language text per spec §7.3; technical detail stays in the log under
    /// [DearImGuiKSP]. All current failure triggers fire during addon Start() at the
    /// main menu, so showing the dialog immediately satisfies "shown at the main menu".
    /// </summary>
    internal sealed class FailureNotifier : IFailureNotifier
    {
        private readonly ILogger _logger;
        private bool _notified;

        internal FailureNotifier(ILogger logger)
        {
            _logger = logger;
        }

        /// <inheritdoc/>
        public void Notify(FailureKind kind)
        {
            // Belt and braces alongside the state machine's terminal guard: the spec's
            // "one popup per session" holds even if later code paths notify directly.
            if (_notified)
            {
                return;
            }
            _notified = true;

            _logger.Debug("Showing startup-failure popup (" + kind + ").");
            PopupDialog.SpawnPopupDialog(
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                "DearImGuiKSP_StartupFailure",
                FailureText.DK_FailTitle,
                FailureText.BodyFor(kind),
                "OK",
                false,
                HighLogic.UISkin);
        }
    }
}
