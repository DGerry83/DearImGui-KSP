using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
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

        // Managed/native handshake constant (spec §5.4, D17); must match
        // DearImGuiKSPNative_GetVersion(). Bump both DLLs in lockstep.
        private const int ExpectedNativeVersion = 3;

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
        private GetIoCaptureStateDelegate _getIoCaptureState;
        private FeedFrameInputDelegate _feedFrameInput;

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
            if (!_initialized || _getIoCaptureState == null)
            {
                _captureState.MouseCaptured = false;
                _captureState.KeyboardCaptured = false;
                return _captureState;
            }

            int wantMouse = 0;
            int wantKeyboard = 0;
            _getIoCaptureState(ref wantMouse, ref wantKeyboard);
            _captureState.MouseCaptured = wantMouse != 0;
            _captureState.KeyboardCaptured = wantKeyboard != 0;
            return _captureState;
        }

        /// <inheritdoc/>
        public void BeginUiFrame(float width, float height, float deltaSeconds)
        {
            if (!_initialized)
            {
                return;
            }

            FeedFrameInput(width, height);
            _beginFrame(width, height, deltaSeconds);
        }

        // Samples Unity input for this frame and queues it into the native ImGui IO
        // before NewFrame (C9b addendum). Lives in Infrastructure so Application stays
        // Unity-free.
        private void FeedFrameInput(float width, float height)
        {
            float mouseX = Input.mousePosition.x;
            float mouseY = height - Input.mousePosition.y;
            float wheel = Input.mouseScrollDelta.y;

            int mouseButtons = 0;
            if (Input.GetMouseButton(0)) mouseButtons |= 1 << 0;
            if (Input.GetMouseButton(1)) mouseButtons |= 1 << 1;
            if (Input.GetMouseButton(2)) mouseButtons |= 1 << 2;

            int keyBits = 0;
            if (Input.GetKey(KeyCode.Backspace)) keyBits |= 1 << 0;
            if (Input.GetKey(KeyCode.Delete)) keyBits |= 1 << 1;
            if (Input.GetKey(KeyCode.LeftArrow)) keyBits |= 1 << 2;
            if (Input.GetKey(KeyCode.RightArrow)) keyBits |= 1 << 3;
            if (Input.GetKey(KeyCode.UpArrow)) keyBits |= 1 << 4;
            if (Input.GetKey(KeyCode.DownArrow)) keyBits |= 1 << 5;
            if (Input.GetKey(KeyCode.Home)) keyBits |= 1 << 6;
            if (Input.GetKey(KeyCode.End)) keyBits |= 1 << 7;
            if (Input.GetKey(KeyCode.Return) || Input.GetKey(KeyCode.KeypadEnter)) keyBits |= 1 << 8;
            if (Input.GetKey(KeyCode.Escape)) keyBits |= 1 << 9;
            if (Input.GetKey(KeyCode.Tab)) keyBits |= 1 << 10;
            if (Input.GetKey(KeyCode.LeftControl)) keyBits |= 1 << 11;
            if (Input.GetKey(KeyCode.RightControl)) keyBits |= 1 << 12;

            byte[] utf8Chars = null;
            string inputString = Input.inputString;
            if (!string.IsNullOrEmpty(inputString))
            {
                StringBuilder sb = new StringBuilder(inputString.Length);
                for (int i = 0; i < inputString.Length; ++i)
                {
                    char c = inputString[i];
                    if (c >= 0x20 && c != 0x7F)
                    {
                        sb.Append(c);
                    }
                }

                string filtered = sb.ToString();
                if (filtered.Length > 0)
                {
                    byte[] bytes = Encoding.UTF8.GetBytes(filtered);
                    utf8Chars = new byte[bytes.Length + 1];
                    Buffer.BlockCopy(bytes, 0, utf8Chars, 0, bytes.Length);
                    utf8Chars[bytes.Length] = 0;
                }
            }

            _feedFrameInput(mouseX, mouseY, wheel, mouseButtons, keyBits, utf8Chars);
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
            // ImGui DisplaySize is set every BeginFrame, the backend draws into
            // whatever render target Unity has bound at render-event time, and the
            // font atlas is resolution-independent, so there is nothing to rebuild.
            _logger.Debug("Viewport resize to " + width + "x" + height + ": no native rebuild needed.");
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
            _getIoCaptureState = Bind<GetIoCaptureStateDelegate>("DearImGuiKSPNative_GetIoCaptureState");
            _feedFrameInput = Bind<FeedFrameInputDelegate>("DearImGuiKSPNative_FeedFrameInput");
            return _getVersion != null
                && _setD3D11DeviceTexture != null
                && _contextInit != null
                && _contextShutdown != null
                && _beginFrame != null
                && _endFrame != null
                && _getRenderEventFunc != null
                && _getIoCaptureState != null
                && _feedFrameInput != null;
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

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void GetIoCaptureStateDelegate(ref int wantMouse, ref int wantKeyboard);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void FeedFrameInputDelegate(float mouseX, float mouseY, float wheel, int mouseButtons, int keyBits, [In] byte[] utf8Chars);

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
