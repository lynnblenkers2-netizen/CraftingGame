using UnityEditor;
using UnityEditor.Compilation;

// Clears the current selection when entering Play Mode or after a script domain reload.
// This prevents the Inspector from holding onto destroyed prefab stage previews,
// which can trigger SerializedObjectNotCreatable exceptions on reload.
[InitializeOnLoad]
static class ClearSelectionOnPlay
{
    static ClearSelectionOnPlay()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        CompilationPipeline.compilationFinished += _ => ClearSelection();
    }

    static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingEditMode || state == PlayModeStateChange.EnteredPlayMode)
            ClearSelection();
    }

    static void ClearSelection()
    {
        // Unconditionally clear selection to avoid the Inspector holding onto destroyed prefab-stage previews.
        Selection.activeObject = null;
        Selection.objects = System.Array.Empty<UnityEngine.Object>();
    }
}
