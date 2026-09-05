using System.IO;
using KSP.UI.Screens;
using UnityEngine;

namespace DearImGuiKSP.Infrastructure
{
    /// <summary>
    /// ApplicationLauncher toolbar button for the library's own settings window
    /// (chunk C31). Follows the DearImGuiKSPDemo consumer pattern: an
    /// EveryScene addon that adds the button when the launcher is ready, in all
    /// scenes where the launcher exists (AppScenes.ALWAYS), and removes it in
    /// OnDestroy. When the library self-disabled at startup (failure, or
    /// enabled = false in settings.cfg) the state machine never reaches
    /// Running, so no button appears — that falls out naturally from the
    /// IsRunning gate in Start. The button toggles
    /// <see cref="LibraryControlPanel.Visible"/> on the shared panel instance.
    /// Icon: GameData/DearImGuiKSP/Textures/toolbar.png (38x38) via the
    /// GameDatabase when present; a runtime-generated grey placeholder
    /// otherwise, so the feature works before the art lands (no binary
    /// placeholder is committed).
    /// </summary>
    [KSPAddon(KSPAddon.Startup.EveryScene, false)]
    public sealed class LibraryPanelToolbar : MonoBehaviour
    {
        private ApplicationLauncherButton _toolbarButton;

        private void Start()
        {
            if (!Composition.StateMachine.IsRunning)
            {
                return; // library dormant or failed: no button, no panel
            }

            GameEvents.onGUIApplicationLauncherReady.Add(OnLauncherReady);
            if (ApplicationLauncher.Ready)
            {
                OnLauncherReady();
            }
        }

        private void OnDestroy()
        {
            GameEvents.onGUIApplicationLauncherReady.Remove(OnLauncherReady);
            if (_toolbarButton != null && ApplicationLauncher.Instance != null)
            {
                ApplicationLauncher.Instance.RemoveModApplication(_toolbarButton);
                _toolbarButton = null;
            }
        }

        private void OnLauncherReady()
        {
            if (_toolbarButton != null)
            {
                return;
            }
            _toolbarButton = ApplicationLauncher.Instance.AddModApplication(
                OnToolbarOn, OnToolbarOff,
                null, null, null, null,
                ApplicationLauncher.AppScenes.ALWAYS,
                LoadToolbarIcon());
        }

        private void OnToolbarOn()
        {
            Composition.ControlPanel.Visible = true;
        }

        private void OnToolbarOff()
        {
            Composition.ControlPanel.Visible = false;
        }

        // The real icon ships as GameData/DearImGuiKSP/Textures/toolbar.png
        // (loaded through the GameDatabase, which owns and caches the texture).
        // Until the art lands, generate a simple neutral placeholder so the
        // button is usable — same 38x38 stock toolbar size as the demo icons.
        private static Texture2D LoadToolbarIcon()
        {
            if (File.Exists(Path.Combine(KSPUtil.ApplicationRootPath, LibraryConfig.ToolbarIconPath)))
            {
                Texture2D icon = GameDatabase.Instance.GetTexture(LibraryConfig.ToolbarIconUrl, false);
                if (icon != null)
                {
                    return icon;
                }
            }
            return MakePlaceholderIcon();
        }

        // Neutral grey 38x38 placeholder: filled square with a lighter border,
        // visually distinct from the demo's green/blue buttons.
        private static Texture2D MakePlaceholderIcon()
        {
            const int size = 38;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color[size * size];
            var fill = new Color(0.45f, 0.48f, 0.54f, 1f);
            var edge = new Color(0.75f, 0.78f, 0.84f, 1f);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool border = x < 3 || y < 3 || x >= size - 3 || y >= size - 3;
                    pixels[y * size + x] = border ? edge : fill;
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }
    }
}
