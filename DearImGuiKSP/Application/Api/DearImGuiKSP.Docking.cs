using DearImGuiKSP.Interop;
using Vector2 = UnityEngine.Vector2;

namespace DearImGuiKSP
{
    /// <summary>
    /// Window docking API (ISSUES #011): a curated subset of ImGui's docking
    /// surface — a full-viewport dockspace, and the DockBuilder layout API for
    /// programmatically arranging windows each session (no layout persistence,
    /// spec §6.2: io.IniFilename stays nullptr, so consumers reapply layouts
    /// via these calls every session).
    /// Docking is gated by the library's persisted <c>docking</c> setting
    /// (default ON; the "DearImGui-KSP Settings" panel toggles it live).
    /// Every method here is only valid inside a registered callback; calling
    /// from anywhere else is a safe no-op, never an exception. Docked windows
    /// cannot leave the game window — ImGuiConfigFlags_ViewportsEnable stays
    /// OFF (documented limitation, docs/70-troubleshooting.md).
    /// </summary>
    public static partial class DearImGuiKSP
    {
        /// <summary>
        /// Submits a dockspace host region of the given size and returns its ID.
        /// Declare the dockspace inside one of your windows each frame, then
        /// dock other windows into it by ID (via
        /// <see cref="DockBuilderDockWindow(string, uint)"/> or by dragging).
        /// Only valid inside a registered callback.
        /// </summary>
        /// <param name="id">Dockspace ID; pass 0 together with
        /// <see cref="DockBuilderAddNode(uint, ImGuiDockNodeFlags)"/>-created nodes or
        /// reuse the ID returned by an earlier call.</param>
        /// <param name="size">Dockspace size in pixels. A zero or negative
        /// component fills the remaining content region in that dimension —
        /// but in an <c>autoResize</c> window there is no remaining region at
        /// the end of the content, so a zero size collapses the dockspace to a
        /// 4-pixel strip; pass an explicit height there (a zero width still
        /// fills the fitted width).</param>
        /// <param name="flags">Dock-node flags (see <see cref="ImGuiDockNodeFlags"/>).</param>
        /// <returns>The dockspace ID; 0 when called while unavailable.</returns>
        public static uint DockSpace(uint id, Vector2 size, ImGuiDockNodeFlags flags = ImGuiDockNodeFlags.None)
        {
            if (!CanDeclareUi)
            {
                return 0;
            }
            return ImGuiInternal.DockSpace(id, new ImVec2(size.x, size.y), (int)flags);
        }

        /// <summary>
        /// Submits a dockspace covering the main viewport and returns its ID —
        /// the common host for full-window docking layouts. Only valid inside a
        /// registered callback.
        /// </summary>
        /// <param name="dockspaceId">Dockspace ID; 0 derives a deterministic ID
        /// from the viewport (pass the same explicit ID every frame if you
        /// reserve one).</param>
        /// <param name="flags">Dock-node flags (see <see cref="ImGuiDockNodeFlags"/>).</param>
        /// <returns>The dockspace ID; 0 when called while unavailable.</returns>
        public static uint DockSpaceOverViewport(uint dockspaceId = 0, ImGuiDockNodeFlags flags = ImGuiDockNodeFlags.None)
        {
            if (!CanDeclareUi)
            {
                return 0;
            }
            return ImGuiInternal.DockSpaceOverViewport(dockspaceId, (int)flags);
        }

        /// <summary>
        /// Creates a dock node and returns its ID. Part of the DockBuilder
        /// layout API: build a layout once per session (e.g. guarded by a
        /// "layout applied" flag), then call <see cref="DockBuilderFinish"/>.
        /// Only valid inside a registered callback.
        /// </summary>
        /// <param name="nodeId">Requested ID; 0 lets ImGui generate a fresh one.</param>
        /// <param name="flags">Dock-node flags (see <see cref="ImGuiDockNodeFlags"/>).</param>
        /// <returns>The node's ID; 0 when called while unavailable.</returns>
        public static uint DockBuilderAddNode(uint nodeId = 0, ImGuiDockNodeFlags flags = ImGuiDockNodeFlags.None)
        {
            if (!CanDeclareUi)
            {
                return 0;
            }
            return ImGuiInternal.DockBuilderAddNode(nodeId, (int)flags);
        }

        /// <summary>
        /// Removes a dock node, undocking its windows. Only valid inside a
        /// registered callback.
        /// </summary>
        /// <param name="nodeId">The node's ID.</param>
        public static void DockBuilderRemoveNode(uint nodeId)
        {
            if (!CanDeclareUi)
            {
                return;
            }
            ImGuiInternal.DockBuilderRemoveNode(nodeId);
        }

        /// <summary>
        /// Sets a dock node's size. Only valid inside a registered callback.
        /// </summary>
        /// <param name="nodeId">The node's ID.</param>
        /// <param name="size">New size in pixels.</param>
        public static void DockBuilderSetNodeSize(uint nodeId, Vector2 size)
        {
            if (!CanDeclareUi)
            {
                return;
            }
            ImGuiInternal.DockBuilderSetNodeSize(nodeId, new ImVec2(size.x, size.y));
        }

        /// <summary>
        /// Splits a dock node in the given direction. The child at
        /// <paramref name="dir"/> receives <paramref name="sizeRatioForNodeAtDir"/>
        /// of the split axis. Only valid inside a registered callback.
        /// </summary>
        /// <param name="nodeId">The node to split.</param>
        /// <param name="dir">Which side the new node lands on.</param>
        /// <param name="sizeRatioForNodeAtDir">Fraction of the split axis the child at <paramref name="dir"/> receives (0–1).</param>
        /// <param name="idAtDir">Receives the child node's ID at <paramref name="dir"/>.</param>
        /// <param name="idAtOppositeDir">Receives the child node's ID opposite <paramref name="dir"/>.</param>
        /// <returns>The new node's ID; 0 when called while unavailable (both out-parameters are 0 then).</returns>
        public static uint DockBuilderSplitNode(uint nodeId, ImGuiDir dir, float sizeRatioForNodeAtDir, out uint idAtDir, out uint idAtOppositeDir)
        {
            if (!CanDeclareUi)
            {
                idAtDir = 0;
                idAtOppositeDir = 0;
                return 0;
            }
            return ImGuiInternal.DockBuilderSplitNode(nodeId, (int)dir, sizeRatioForNodeAtDir, out idAtDir, out idAtOppositeDir);
        }

        /// <summary>
        /// Docks an existing window — identified by its title, which is its
        /// process-global ImGui identity — into a dock node. Call while building
        /// a layout, before <see cref="DockBuilderFinish"/>. Only valid inside a
        /// registered callback.
        /// </summary>
        /// <param name="windowName">The window's title, exactly as passed to <see cref="BeginWindow(string, bool)"/>.</param>
        /// <param name="nodeId">The target node's ID.</param>
        public static void DockBuilderDockWindow(string windowName, uint nodeId)
        {
            if (!CanDeclareUi)
            {
                return;
            }
            ImGuiInternal.DockBuilderDockWindow(windowName, nodeId);
        }

        /// <summary>
        /// Finalizes a DockBuilder layout sequence and makes the constructed
        /// layout active. Only valid inside a registered callback.
        /// </summary>
        /// <param name="dockspaceId">The root dockspace/node ID passed to <see cref="DockBuilderAddNode(uint, ImGuiDockNodeFlags)"/>.</param>
        public static void DockBuilderFinish(uint dockspaceId)
        {
            if (!CanDeclareUi)
            {
                return;
            }
            ImGuiInternal.DockBuilderFinish(dockspaceId);
        }

        /// <summary>
        /// Returns the central dock node's ID for a dockspace hierarchy, or 0
        /// when the hierarchy has no central node (or the call is unavailable).
        /// Useful with <see cref="ImGuiDockNodeFlags.PassthruCentralNode"/> to
        /// reserve a see-through region. Only valid inside a registered callback.
        /// </summary>
        /// <param name="dockspaceId">The dockspace's ID.</param>
        /// <returns>The central node's ID; 0 when there is none or when unavailable.</returns>
        public static uint DockBuilderGetCentralNode(uint dockspaceId)
        {
            if (!CanDeclareUi)
            {
                return 0;
            }
            return ImGuiInternal.DockBuilderGetCentralNode(dockspaceId);
        }
    }
}
