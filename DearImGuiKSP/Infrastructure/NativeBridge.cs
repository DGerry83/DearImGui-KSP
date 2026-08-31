using System;
using System.IO;
using System.Runtime.InteropServices;
using DearImGuiKSP.Application;
using DearImGuiKSP.Application.Interfaces;
using UnityEngine;
using ILogger = DearImGuiKSP.Application.Interfaces.ILogger;

namespace DearImGuiKSP.Infrastructure
{
    /// <summary>
    /// Implements <see cref="INativeBridge"/>. Loads DearImGuiKSPNative.dll explicitly
    /// (LoadLibrary + SetDllDirectory) from GameData/DearImGuiKSP/PluginData/ — native DLLs
    /// must stay out of the loader's assembly scan path (D19), and implicit [DllImport]
    /// resolution fails from GameData subfolders, so every native function is bound via
    /// GetProcAddress + Marshal.GetDelegateForFunctionPointer (kernel32 imports are fine;
    /// the gotcha is only about loading OUR dll implicitly). Performs the managed/native
    /// version handshake (spec §5.4) and the D3D11 device gate (chunk C4 PoC).
    /// </summary>
    internal sealed class NativeBridge : INativeBridge
    {
        // Initialize() result codes — 0 is success, each failure mode is distinct.
        internal const int InitOk = 0;
        internal const int InitErrSetDllDirectory = 1;
        internal const int InitErrLoadLibrary = 2;
        internal const int InitErrMissingExport = 3;
        internal const int InitErrVersionMismatch = 4;
        internal const int InitErrUnsupportedDevice = 5;
        internal const int InitErrContextInit = 6;
        internal const int InitErrDeviceTexture = 7;

        // Managed/native handshake constant for the 0.1.x line (spec §5.4, D17);
        // must match DearImGuiKSPNative_GetVersion(). Bump both DLLs in lockstep.
        private const int ExpectedNativeVersion = 1;

        private readonly ILogger _logger;
        private readonly InputCaptureState _captureState = new InputCaptureState();

        private IntPtr _library;
        private bool _initialized;

        // Kept alive for the session: the device is captured from this texture's
        // native pointer, and we never want it collected underneath the backend.
        private Texture2D _deviceTexture;

        // Native function delegates (C3-locked C ABI + C4 device handoff). Held in
        // fields so the GC never collects a delegate the native side may call back.
        private GetVersionDelegate _getVersion;
        private SetD3D11DeviceTextureDelegate _setD3D11DeviceTexture;
        private ContextInitDelegate _contextInit;
        private ContextShutdownDelegate _contextShutdown;
        private BeginFrameDelegate _beginFrame;
        private EndFrameDelegate _endFrame;
        private GetRenderEventFuncDelegate _getRenderEventFunc;

        internal NativeBridge(ILogger logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Native render-event callback pointer for GL.IssuePluginEvent. IntPtr keeps
        /// Unity types out of the INativeBridge interface; Composition wires it (C4).
        /// Zero until Initialize() succeeds.
        /// </summary>
        internal IntPtr RenderEventFunc { get; private set; }

        /// <inheritdoc/>
        public int Initialize()
        {
            if (_initialized)
            {
                return InitOk;
            }

            // GameData is not on the loader's native DLL search path; point
            // SetDllDirectory at PluginData so LoadLibrary resolves there.
            string pluginDataPath = Path.Combine(KSPUtil.ApplicationRootPath, LibraryConfig.NativePluginDataDir);
            if (!SetDllDirectory(pluginDataPath))
            {
                _logger.Error("SetDllDirectory failed for '" + pluginDataPath + "' (Win32 error " + Marshal.GetLastWin32Error() + ").");
                return InitErrSetDllDirectory;
            }

            _library = LoadLibrary(LibraryConfig.NativeDllName);
            if (_library == IntPtr.Zero)
            {
                _logger.Error("LoadLibrary('" + LibraryConfig.NativeDllName + "') failed (Win32 error " + Marshal.GetLastWin32Error() + ").");
                return InitErrLoadLibrary;
            }

            if (!BindExports())
            {
                Unload();
                return InitErrMissingExport;
            }

            int nativeVersion = _getVersion();
            if (nativeVersion != ExpectedNativeVersion)
            {
                _logger.Error("Native/managed handshake mismatch: expected version " + ExpectedNativeVersion + ", DearImGuiKSPNative reported " + nativeVersion + " (spec §5.4).");
                Unload();
                return InitErrVersionMismatch;
            }

            // Device gate managed-side: Unity only calls UnityPluginLoad for plugins
            // it loads itself, so the native side cannot see IUnityGraphics in our
            // deployment — SystemInfo is the authoritative check (GL arrives in C5).
            if (SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Direct3D11)
            {
                _logger.Error("Graphics device " + SystemInfo.graphicsDeviceType + " is not Direct3D11; the milestone 2 PoC requires D3D11 (GL support arrives in C5).");
                Unload();
                return InitErrUnsupportedDevice;
            }

            // Hand the backend a Unity-created texture so it can capture the D3D11
            // device (texture->GetDevice) — the CinematicRecorderNative pattern,
            // since IUnityInterfaces is unavailable to a LoadLibrary'd plugin.
            _deviceTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            int deviceResult = _setD3D11DeviceTexture(_deviceTexture.GetNativeTexturePtr());
            if (deviceResult != 0)
            {
                _logger.Error("DearImGuiKSPNative_SetD3D11DeviceTexture failed with code " + deviceResult + ".");
                Unload();
                return InitErrDeviceTexture;
            }

            int contextResult = _contextInit();
            if (contextResult != 0)
            {
                _logger.Error("DearImGuiKSPNative_ContextInit failed with code " + contextResult + ".");
                Unload();
                return InitErrContextInit;
            }

            RenderEventFunc = _getRenderEventFunc();

            _initialized = true;
            _logger.Info("Native bridge initialized: DearImGuiKSPNative v" + nativeVersion + " on D3D11, context up.");
            return InitOk;
        }

        /// <inheritdoc/>
        public InputCaptureState GetIoSnapshot()
        {
            // TODO(C9): fill _captureState from the ImGui IO (WantCaptureMouse/
            // WantCaptureKeyboard). PoC returns defaults: nothing captured.
            return _captureState;
        }

        /// <inheritdoc/>
        public void BeginUiFrame(float width, float height, float deltaSeconds)
        {
            if (!_initialized)
            {
                return;
            }
            _beginFrame(width, height, deltaSeconds);
        }

        /// <inheritdoc/>
        public void EndUiFrame()
        {
            if (!_initialized)
            {
                return;
            }
            _endFrame();
        }

        /// <inheritdoc/>
        public void RebuildViewport(int width, int height)
        {
            // TODO(C12): recreate the render viewport / notify the native backend on
            // resolution or fullscreen changes. PoC no-op: the D3D11 backend draws
            // into whatever render target Unity has bound at render-event time.
        }

        /// <inheritdoc/>
        public void Shutdown()
        {
            if (_library == IntPtr.Zero)
            {
                return;
            }
            if (_initialized)
            {
                _contextShutdown();
                _initialized = false;
            }
            RenderEventFunc = IntPtr.Zero;
            Unload();
        }

        // Binds every native export; logs and returns false on the first missing one.
        private bool BindExports()
        {
            _getVersion = Bind<GetVersionDelegate>("DearImGuiKSPNative_GetVersion");
            _setD3D11DeviceTexture = Bind<SetD3D11DeviceTextureDelegate>("DearImGuiKSPNative_SetD3D11DeviceTexture");
            _contextInit = Bind<ContextInitDelegate>("DearImGuiKSPNative_ContextInit");
            _contextShutdown = Bind<ContextShutdownDelegate>("DearImGuiKSPNative_ContextShutdown");
            _beginFrame = Bind<BeginFrameDelegate>("DearImGuiKSPNative_BeginFrame");
            _endFrame = Bind<EndFrameDelegate>("DearImGuiKSPNative_EndFrame");
            _getRenderEventFunc = Bind<GetRenderEventFuncDelegate>("DearImGuiKSPNative_GetRenderEventFunc");
            return _getVersion != null
                && _setD3D11DeviceTexture != null
                && _contextInit != null
                && _contextShutdown != null
                && _beginFrame != null
                && _endFrame != null
                && _getRenderEventFunc != null;
        }

        private T Bind<T>(string exportName) where T : class
        {
            IntPtr proc = GetProcAddress(_library, exportName);
            if (proc == IntPtr.Zero)
            {
                _logger.Error("Export '" + exportName + "' not found in " + LibraryConfig.NativeDllName + ".");
                return null;
            }
            return Marshal.GetDelegateForFunctionPointer<T>(proc);
        }

        private void Unload()
        {
            if (_library != IntPtr.Zero)
            {
                FreeLibrary(_library);
                _library = IntPtr.Zero;
            }
        }

        // Native signatures (x64 Windows: one calling convention, Cdecl is correct).
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int GetVersionDelegate();

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int SetD3D11DeviceTextureDelegate(IntPtr d3d11TexturePtr);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int ContextInitDelegate();

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void ContextShutdownDelegate();

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void BeginFrameDelegate(float width, float height, float deltaSeconds);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void EndFrameDelegate();

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate IntPtr GetRenderEventFuncDelegate();

        // kernel32 only — the GameData load-path gotcha applies to OUR dll, not these.
        [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetDllDirectory(string lpPathName);

        [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr LoadLibrary(string lpFileName);

        [DllImport("kernel32", SetLastError = true)]
        private static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);

        [DllImport("kernel32", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool FreeLibrary(IntPtr hModule);
    }
}
