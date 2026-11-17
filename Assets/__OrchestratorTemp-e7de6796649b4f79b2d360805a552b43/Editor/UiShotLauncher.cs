using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class UiShotLauncher
{
    private const string Flag = "UiShotLauncher.Active";

    static UiShotLauncher()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredEditMode && SessionState.GetBool(Flag, false))
            {
                SessionState.SetBool(Flag, false);
                EditorSceneManager.OpenScene("Assets/Game/Main.unity", OpenSceneMode.Single);
            }
        };
    }

    [MenuItem("Tools/UiShot/Run")]
    public static void Run()
    {
        SessionState.SetBool(Flag, true);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        new GameObject("UiShotRunner").AddComponent<UiShotRunner>();
        EditorApplication.isPlaying = true;
    }
}
