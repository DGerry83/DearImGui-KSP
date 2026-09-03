namespace DearImGuiKSP.Application
{
    /// <summary>
    /// Player-facing failure strings, verbatim from the spec §7.1 string table.
    /// Voice rules §7.3: plain language, no technical detail — that stays in the log.
    /// Unity-free so the kind→body mapping is decoupled from the dialog mechanics.
    /// </summary>
    internal static class FailureText
    {
        internal const string DK_FailTitle = "DearImGui-KSP — Startup Failed";

        internal const string DK_FailNative =
            "DearImGui-KSP could not start because its native component is missing or corrupt. " +
            "Mods that depend on DearImGui-KSP will not work. See KSP.log for details.";

        internal const string DK_FailGraphics =
            "DearImGui-KSP could not start because this graphics API is not supported. " +
            "Mods that depend on DearImGui-KSP will not work. See KSP.log for details.";

        internal const string DK_FailVersion =
            "DearImGui-KSP could not start because its components are from different versions. " +
            "Mods that depend on DearImGui-KSP will not work. See KSP.log for details.";

        /// <summary>
        /// Popup body for a failure kind. §7.1 defines only three bodies; render-hook
        /// failure maps to DK_FailNative because the render hook is part of the
        /// native/render component (C13 contract design note).
        /// </summary>
        internal static string BodyFor(FailureKind kind)
        {
            switch (kind)
            {
                case FailureKind.VersionMismatch:
                    return DK_FailVersion;
                case FailureKind.GraphicsApi:
                    return DK_FailGraphics;
                case FailureKind.NativeComponent:
                case FailureKind.RenderHook:
                default:
                    return DK_FailNative;
            }
        }
    }
}
