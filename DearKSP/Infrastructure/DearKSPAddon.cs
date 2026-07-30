using UnityEngine;

namespace DearKSP.Infrastructure
{
    /// <summary>
    /// KSP entry point (spec §5.1). Created once at main menu; must DontDestroyOnLoad itself —
    /// the game does not do it for once-addons (KSP Knowledge Library, assembly-loading notes).
    /// Thin shell only: all wiring lives in Composition.
    /// </summary>
    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    public sealed class DearKSPAddon : MonoBehaviour
    {
        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            Composition.Logger.Info("Dear KSP loaded. Waiting for initialization (milestone 2).");
        }
    }
}
