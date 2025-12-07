# Copilot / AI Agent instructions — VRArena

This Unity XR project uses Unity packages (XR Interaction Toolkit, XR Hands, URP). The goal of these notes is to help an AI coding agent be immediately productive when making code changes, implementing small features, or preparing builds.

Key places to inspect

- `Assets/Scripts/FreezeArmor.cs` — example script (class `DefenseSlot`) showing common patterns: `RequireComponent`, `Awake` for caching components, add/remove listeners in `OnEnable`/`OnDisable`, and direct manipulation of `XRGrabInteractable` and `Rigidbody` properties.
- `Assets/Scenes/` — scene assets for runtime layout and XR rigs.
- `Assets/Samples/*` — vendor/sample content (useful for reference, avoid modifying vendor samples unless necessary).
- `Packages/manifest.json` — authoritative list of package dependencies (notably `com.unity.xr.interaction.toolkit`, `com.unity.xr.hands`, `com.unity.render-pipelines.universal`).
- `ProjectSettings/ProjectVersion.txt` — project Unity editor version (`m_EditorVersion`); use this to pick the correct Unity editor when running CLI builds.
- `VRArena.slnx` and generated `Assembly-CSharp.csproj` files — open in an IDE for code navigation; regenerate by opening in Unity if stale.

Project architecture / patterns (what to know)

- This is a Unity project (MonoBehaviour-based). Most code lives inside `Assets/` and follows Unity lifecycle hooks (`Awake`, `Start`, `OnEnable`, `OnDisable`, `Update`). Prefer editing existing MonoBehaviour scripts rather than changing compiled assemblies.
- XR patterns: the project uses XR Interaction Toolkit event-driven interactions. Expect to see `XRSocketInteractor`, `XRGrabInteractable`, `SelectEnterEventArgs`/`SelectExitEventArgs` usage (see `DefenseSlot` in `FreezeArmor.cs`). When attaching/detaching listeners, use `OnEnable`/`OnDisable` symmetry.
- Physics/interaction conventions: scripts frequently toggle `Rigidbody.isKinematic` and `useGravity`, and enable/disable `XRGrabInteractable` to change interactability state.
- Component caching: follow existing style that caches components in `Awake` and references them via private fields.
- Inline comments: some files use Russian comments — be mindful if generating new comments or user-facing text.

Builds, editor and test workflows (practical commands)

- Open the project in Unity Editor (recommended for most changes). Use Unity Hub or open the editor that matches `ProjectSettings/ProjectVersion.txt`.
- Example Windows CLI build (replace `UNITY_EDITOR_PATH` with your editor path):

```powershell
& "C:\Program Files\Unity\Hub\Editor\<VERSION>\Editor\Unity.exe" `
  -projectPath "C:\Users\alexl\OneDrive\Рабочий стол\Gamedev\VRArena" `
  -quit -batchmode `
  -buildWindowsPlayer "C:\path\to\builds\VRArena.exe" `
  -buildTarget Win64 `
  -logFile "build.log"
```

- Run automated Unity tests from CLI (if tests exist):

```powershell
& "<UNITY_EDITOR_PATH>" -projectPath "<PROJECT_PATH>" -runTests -testPlatform playmode -logFile "test-results.log"
```

Developer notes and conventions

- Do not edit `Library/`, `Temp/`, `Logs/` or `ProjectSettings` except to add project-wide settings intentionally; these are either generated or environment-specific.
- To update C# project files after changing package manifest or asmdefs, open the Unity Editor and let it regenerate solution files; do not hand-edit `.csproj` files.
- Use existing event/listener patterns (see `OnEnable`/`OnDisable` usage) to avoid leaving dangling listeners.
- Keep changes localized to `Assets/` where possible; large engine/package updates require testing in the Unity Editor.

Integration points and external deps

- Unity Package Manager (see `Packages/manifest.json`) — prefer using package versions there.
- XR subsystems (OpenXR, `com.unity.xr.openxr`, `com.unity.xr.hands`) — verify runtime behavior in Editor or target device.
- Multiplayer/Netcode packages may be present in `manifest.json` — exercise caution when changing shared state code.

What examples to copy from the repo

- `Assets/Scripts/FreezeArmor.cs` — canonical example for XR interaction handling and Rigidbody/grab state toggling.

If you modify or add files

- Add concise summary in the PR description explaining why changes are safe to test in Editor, and which scenes or prefabs should be opened to verify behavior.
- If you add public API (new MonoBehaviours, ScriptableObjects), mention where to attach them in the scene hierarchy and how to exercise them during Play Mode.

Open questions / things I couldn't infer

- Project CI/build host details (exact Unity install paths or CI YAML) are not present; if you want CI build snippets, provide the CI environment or a sample Unity install path.
- Test coverage: there are no obvious Unity Test assemblies in the repo root; confirm whether PlayMode/EditMode tests are used.

If anything is missing or you'd like a different tone/length, tell me which sections to expand or remove and I will iterate.
