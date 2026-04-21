using System.IO;
using UnityEditor;
using UnityEngine;

namespace Project.Editor.ClaudeBridge
{
    /// <summary>
    /// Editor가 열려 있을 때 `.claude-bridge/inbox/`를 EditorApplication.update로 폴링해 픽업.
    ///
    /// 배치(헤드리스) 모드에서는 이 서버가 대신 <see cref="ClaudeBridgeBatch"/>가 -executeMethod로 직접 호출된다.
    /// 둘 다 내부적으로 <see cref="CommandIO.ProcessOne"/>을 호출해 동일 로직을 공유한다.
    ///
    /// 폴링을 쓰는 이유: FileSystemWatcher는 백그라운드 스레드 콜백이라 Unity API 호출이 막힘.
    /// 200ms 폴링은 체감 지연 낮고 CPU 무시할 수준.
    /// </summary>
    [InitializeOnLoad]
    public static class ClaudeBridgeServer
    {
        const string PrefKey_AutoStart = "ClaudeBridge.AutoStart";
        const float  PollInterval = 0.2f;

        static double _lastPoll;
        static bool   _running;

        public static bool IsRunning => _running;

        static ClaudeBridgeServer()
        {
            // 배치 모드에서는 상주 서버를 시동하지 않는다 (ClaudeBridgeBatch가 직접 처리).
            if (Application.isBatchMode) return;

            // 최초 기동 시에도 자동 시작되도록 default=true. Stop을 명시적으로 누르면 false로 저장되어 그 이후엔 뜨지 않는다.
            if (EditorPrefs.GetBool(PrefKey_AutoStart, true))
                Start();
        }

        public static void Start()
        {
            if (_running) return;
            CommandIO.EnsureFolders();
            EditorApplication.update += Tick;
            _running = true;
            EditorPrefs.SetBool(PrefKey_AutoStart, true);
            Debug.Log($"[ClaudeBridge] Started. inbox={CommandIO.InboxFolder} outbox={CommandIO.OutboxFolder}");
        }

        public static void Stop()
        {
            if (!_running) return;
            EditorApplication.update -= Tick;
            _running = false;
            EditorPrefs.SetBool(PrefKey_AutoStart, false);
            Debug.Log("[ClaudeBridge] Stopped.");
        }

        static void Tick()
        {
            if (EditorApplication.timeSinceStartup - _lastPoll < PollInterval) return;
            _lastPoll = EditorApplication.timeSinceStartup;

            var inbox = CommandIO.InboxPath;
            if (!Directory.Exists(inbox)) return;

            // 긴 작업이 에디터를 멈추지 않게 tick당 한 개씩만 처리.
            foreach (var file in Directory.GetFiles(inbox, "*.json"))
            {
                CommandIO.ProcessOne(file);
                break;
            }
        }
    }
}
