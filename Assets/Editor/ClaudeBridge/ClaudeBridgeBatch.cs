using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Project.Editor.ClaudeBridge
{
    /// <summary>
    /// 헤드리스(배치) 진입점. Unity CLI에서:
    /// <code>
    /// Unity -batchmode -nographics -projectPath &lt;dir&gt; \
    ///       -executeMethod Project.Editor.ClaudeBridge.ClaudeBridgeBatch.Run \
    ///       -quit
    /// </code>
    /// 으로 호출하면 `.claude-bridge/inbox/` 아래 모든 커맨드를 알파벳 순(타임스탬프 파일명 전제)
    /// 으로 동기 실행하고 outbox에 결과를 남긴 뒤 Unity를 종료한다.
    ///
    /// 종료 코드:
    ///   0 = 모든 커맨드 성공
    ///   1 = 일부/전부 실패 (outbox의 각 result.ok 확인)
    /// </summary>
    public static class ClaudeBridgeBatch
    {
        public static void Run()
        {
            CommandIO.EnsureFolders();

            var files = Directory.Exists(CommandIO.InboxPath)
                ? Directory.GetFiles(CommandIO.InboxPath, "*.json").OrderBy(f => f, System.StringComparer.Ordinal).ToArray()
                : System.Array.Empty<string>();

            int ok = 0, fail = 0;
            foreach (var file in files)
            {
                if (CommandIO.ProcessOne(file)) ok++;
                else fail++;
            }

            Debug.Log($"[ClaudeBridge.Batch] Processed {files.Length} commands. ok={ok} fail={fail}");
            EditorApplication.Exit(fail == 0 ? 0 : 1);
        }
    }
}
