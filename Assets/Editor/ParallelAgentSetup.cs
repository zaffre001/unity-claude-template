#if UNITY_EDITOR
using UnityEditor;

[InitializeOnLoad]
public static class ParallelAgentSetup
{
    static ParallelAgentSetup()
    {
        // Domain Reload & Scene Reload 비활성화
        EditorSettings.enterPlayModeOptionsEnabled = true;
        EditorSettings.enterPlayModeOptions =
            EnterPlayModeOptions.DisableDomainReload |
            EnterPlayModeOptions.DisableSceneReload;
    }
}
#endif
