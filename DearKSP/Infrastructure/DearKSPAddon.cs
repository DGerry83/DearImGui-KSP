using UnityEngine;

namespace DearKSP.Infrastructure
{
    /// <summary>
    /// KSP entry point (spec §5.1). Created once at main menu; must DontDestroyOnLoad itself —
    /// the game does not do it for once-addons (KSP Knowledge Library, assembly-loading notes).
    /// TODO(milestone 1+): delegate to Composition; this class stays a thin shell.
    /// </summary>
    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    public sealed class DearKSPAddon : MonoBehaviour
    {
        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            Debug.Log(LibraryConfig.LogPrefix + " Dear KSP loaded (skeleton).");
        }
    }
}
