using System;
using System.IO;
using UnityEngine;

namespace Project.Editor.ClaudeBridge
{
    /// <summary>
    /// inbox 파일 한 개 읽기 → Dispatcher 실행 → outbox 쓰기 → inbox 파일 삭제.
    /// Editor 상주 서버(폴링)와 헤드리스 배치(CLI) 둘 다 이 로직을 공유한다.
    ///
    /// 모든 Unity API 호출은 메인 스레드 가정. 서버는 EditorApplication.update로 호출하고,
    /// 배치는 -executeMethod 컨텍스트에서 메인 스레드로 실행된다.
    /// </summary>
    public static class CommandIO
    {
        public const string InboxFolder  = ".claude-bridge/inbox";
        public const string OutboxFolder = ".claude-bridge/outbox";

        public static string ProjectRoot => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        public static string InboxPath   => Path.Combine(ProjectRoot, InboxFolder);
        public static string OutboxPath  => Path.Combine(ProjectRoot, OutboxFolder);

        public static void EnsureFolders()
        {
            Directory.CreateDirectory(InboxPath);
            Directory.CreateDirectory(OutboxPath);
        }

        /// <summary>
        /// inbox 파일 하나를 처리. 성공/실패 무관하게 파일은 inbox에서 제거하고 결과는 outbox에 쓴다.
        /// </summary>
        /// <returns>성공 여부</returns>
        public static bool ProcessOne(string inboxFilePath)
        {
            Command cmd = null;
            var res = new Result { ok = false };

            try
            {
                var json = File.ReadAllText(inboxFilePath);
                cmd = JsonUtility.FromJson<Command>(json);
                res.id = cmd?.id ?? Path.GetFileNameWithoutExtension(inboxFilePath);

                if (cmd == null || string.IsNullOrEmpty(cmd.op))
                    throw new Exception("Invalid command: missing op");

                res.dataJson = Dispatcher.Dispatch(cmd.op, cmd.argsJson ?? "{}");
                res.ok = true;
            }
            catch (Exception e)
            {
                res.ok = false;
                res.error = $"{e.GetType().Name}: {e.Message}";
                res.stack = e.StackTrace;
                Debug.LogWarning($"[ClaudeBridge] op={(cmd?.op ?? "?")} error: {e.Message}");
            }
            finally
            {
                WriteResult(res);
                TryDelete(inboxFilePath);
            }

            return res.ok;
        }

        static void WriteResult(Result res)
        {
            try
            {
                var outPath = Path.Combine(OutboxPath, $"{res.id}.json");
                // tmp→move로 부분 작성된 파일을 Claude가 읽는 것 방지.
                var tmp = outPath + ".tmp";
                File.WriteAllText(tmp, JsonUtility.ToJson(res, prettyPrint: true));
                if (File.Exists(outPath)) File.Delete(outPath);
                File.Move(tmp, outPath);
            }
            catch (Exception e)
            {
                Debug.LogError($"[ClaudeBridge] failed to write result: {e.Message}");
            }
        }

        static void TryDelete(string path)
        {
            try { File.Delete(path); }
            catch (Exception e) { Debug.LogWarning($"[ClaudeBridge] inbox delete failed: {e.Message}"); }
        }
    }
}
