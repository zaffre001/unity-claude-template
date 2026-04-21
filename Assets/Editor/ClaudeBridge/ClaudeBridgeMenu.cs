using UnityEditor;
using UnityEngine;

namespace Project.Editor.ClaudeBridge
{
    /// <summary>
    /// Window > Claude Bridge > Start / Stop / Status.
    /// 현재 상태는 메뉴에 체크마크로 표시 (Validate 메서드).
    /// </summary>
    public static class ClaudeBridgeMenu
    {
        [MenuItem("Window/Claude Bridge/Start", priority = 100)]
        static void StartMenu() => ClaudeBridgeServer.Start();

        [MenuItem("Window/Claude Bridge/Start", validate = true)]
        static bool StartMenu_Validate() { Menu.SetChecked("Window/Claude Bridge/Start", ClaudeBridgeServer.IsRunning); return !ClaudeBridgeServer.IsRunning; }

        [MenuItem("Window/Claude Bridge/Stop", priority = 101)]
        static void StopMenu() => ClaudeBridgeServer.Stop();

        [MenuItem("Window/Claude Bridge/Stop", validate = true)]
        static bool StopMenu_Validate() => ClaudeBridgeServer.IsRunning;

        [MenuItem("Window/Claude Bridge/Show Status", priority = 200)]
        static void Status() =>
            Debug.Log($"[ClaudeBridge] running={ClaudeBridgeServer.IsRunning}. Inbox/Outbox under .claude-bridge/ at project root.");
    }
}
