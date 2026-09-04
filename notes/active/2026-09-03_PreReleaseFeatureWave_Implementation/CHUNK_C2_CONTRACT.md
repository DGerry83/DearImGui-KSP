# Chunk Contract: Binding Foundation — Style Push/Pop, Draw-List, Radio Externs
## Plan: DearImGui-KSP Pre-Release Feature Wave
## Date: 2026-09-03
## Chunk ID: C2
## Advances Milestone: M1 (API ergonomics foundation)

### Scope
Add the cimgui extern declarations and safe internal wrappers that M1–M3 consume, following
the established binding pattern exactly (existing 11 bindings in `Interop/ImGuiNative.cs` /
`Interop/ImGuiInternal.cs`). **Managed-only chunk — no native changes.** All listed functions
are standard cimgui exports already compiled into the shipped `DearImGuiKSPNative.dll`
(research confirmed `igShadeVertsLinearColorGradientKeepAlpha` at cimgui.cpp:2482 / cimgui.h:5595).

New externs (cimgui names; exact signatures verified against the pinned cimgui.h at
`~/source/repos/cimgui/cimgui.h` before declaring):

- Style stack: `igPushStyleColor_Vec4`, `igPushStyleColor_U32`, `igPopStyleColor`,
  `igPushStyleVar_Float`, `igPushStyleVar_Vec2`, `igPopStyleVar`
- Color conversion: `igGetColorU32_Vec4` (for packing `Color32`/`Color` → `ImU32` where needed)
- Draw list: `igGetWindowDrawList`, `ImDrawList_AddRectFilled`,
  `ImDrawList_AddRectFilledMultiColor`, `igShadeVertsLinearColorGradientKeepAlpha`,
  `ImDrawList_AddCircle`, `ImDrawList_AddCircleFilled`, `ImDrawList_AddLine`
- Radio: `igRadioButton_Bool`, `igRadioButton_IntPtr`

New enum subsets in `ImGuiNative.cs` (values hand-verified against the pinned
`~/source/repos/cimgui/imgui/imgui.h` with a comment citing the header name — same
discipline as the existing `ImGuiChildFlags`/`ImGuiInputTextFlags` blocks):

- `ImGuiCol` — full enum (theme work in C8 needs the whole table)
- `ImGuiStyleVar` — the subset used by theme/ergonomics (Alpha, WindowRounding,
  WindowPadding, WindowBorderSize, FrameRounding, FramePadding, FrameBorderSize,
  ItemSpacing, GrabRounding, GrabMinSize at minimum; add others only if cheap to verify)

New safe wrappers in `ImGuiInternal.cs`: one internal static method per extern above,
with the same UTF-8/marshalling conventions as the existing wrappers (strings via
`ToUtf8`, `ImVec2` via the existing blittable `ImVec2.cs` struct). No public API changes
in this chunk — the public facade (`DearImGuiKSP.cs`) is touched by C1/C3, not here.

### Inputs (must exist before starting)
- Baseline: `dotnet build DearImGui-KSP.slnx` green, `dotnet test` 59/59 (verified 2026-09-03).
- `Interop/ImGuiNative.cs`, `Interop/ImGuiInternal.cs`, `Interop/ImVec2.cs` as-is.
- Pinned cimgui clone at `~/source/repos/cimgui` (present, verified).

### Outputs (must be created/changed)
- `DearImGuiKSP/Interop/ImGuiNative.cs` — new externs + enum blocks above.
- `DearImGuiKSP/Interop/ImGuiInternal.cs` — matching safe wrappers.
- No other files. No test changes expected (bindings are pure marshalling; the suite must simply stay green).

### Constraints
- Follow the existing binding pattern byte-for-byte in style: `[DllImport("DearImGuiKSPNative", CallingConvention = CallingConvention.Cdecl)]`, bool marshalled as I1, private extern + internal wrapper.
- Do NOT add bindings beyond the list above (YAGNI — ImPlot/cimspinner/shims are later chunks; orbit-specific draw-list externs belong to C21).
- Do NOT modify the public facade, settings, or any Infrastructure file.
- Plan invariants: layering unchanged (Interop stays internal); no public members added, so no new XML-doc obligations in this chunk (existing XML-doc discipline on internals unchanged).
- Native interop checklist (CORE_PROTOCOLS §5.9): this chunk touches P/Invoke declarations.
  - Check 1 (process-global state): **Not applicable** — DllImport declarations only; no loading, no environment/DLL-search mutation.
  - Check 2 (hot-path allocation): **Zero** — wrappers must not allocate per call: no string conversion in these wrappers except radio labels via the existing `ToUtf8` convention; structs passed by value/`in`; no closures, no LINQ.
  - Check 3 (resource-acquisition symmetry): **Not applicable** — no handles or IDisposable acquired.

### Verification
- `dotnet build DearImGui-KSP.slnx` — 0 errors, 0 new warnings.
- `dotnet test DearImGui-KSP.slnx` — 59/59 still passing.
- Export-table check: every new DllImport entry-point name must appear in the export table
  of `GameData/DearImGuiKSP/PluginData/DearImGuiKSPNative.dll` (e.g. `dumpbin /exports` or
  equivalent). Report the exact check command and its raw output lines for each name.
- Signature check: each declared extern's parameter list compared against the pinned
  `cimgui.h` declaration; report the header line cited per function.
- Milestone contribution: provides the binding foundation M1's C1/C3 build on; M1's gate is
  verified after C3.

### Rollback
- Revert `DearImGuiKSP/Interop/ImGuiNative.cs` and `DearImGuiKSP/Interop/ImGuiInternal.cs` to the pre-chunk git checkpoint; rebuild to confirm the 59/59 baseline.
