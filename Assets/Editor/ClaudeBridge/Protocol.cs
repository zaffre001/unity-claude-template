using System;

namespace Project.Editor.ClaudeBridge
{
    /// <summary>
    /// Inbox 커맨드 봉투. Claude가 <c>.claude-bridge/inbox/&lt;id&gt;.json</c>에 이 포맷으로 파일을 드롭한다.
    /// <c>argsJson</c>은 op별 전용 [Serializable] 타입의 JsonUtility 직렬화 결과.
    /// </summary>
    [Serializable]
    public class Command
    {
        public string id;       // 요청 고유 ID. 응답 파일명에 그대로 사용.
        public string op;       // "Scene.New", "GameObject.Create", ... (Dispatcher 라우팅 키)
        public string argsJson; // op별 args 구조체의 JsonUtility 직렬화 문자열
    }

    /// <summary>
    /// Outbox 응답. Unity가 <c>.claude-bridge/outbox/&lt;id&gt;.json</c>에 드롭한다.
    /// Claude는 같은 id로 폴링/읽기.
    /// </summary>
    [Serializable]
    public class Result
    {
        public string id;
        public bool ok;
        public string dataJson; // op별 result 구조체 직렬화 (성공 시)
        public string error;    // 실패 시 메시지 + 타입명
        public string stack;    // 실패 시 스택트레이스 (디버깅용)
    }

    // ---------- op별 args/result 구조체 ----------
    //
    // JsonUtility는 다형성·딕셔너리를 못 다루므로 각 op마다 전용 [Serializable] 클래스를 둔다.
    // 새 op을 추가할 때는 여기에 args/result를 선언하고 Dispatcher에 핸들러를 등록한다.

    // === Scene ===
    [Serializable] public class SceneNewArgs     { public string path; }            // 예: "Assets/Scenes/Solitaire.unity"
    [Serializable] public class SceneNewResult   { public string scenePath; }

    [Serializable] public class SceneOpenArgs    { public string path; }
    [Serializable] public class SceneOpenResult  { public string scenePath; }

    [Serializable] public class SceneSaveArgs    { public string path; }            // 빈 문자열이면 현재 씬 저장
    [Serializable] public class SceneSaveResult  { public string scenePath; }

    // === GameObject ===
    [Serializable] public class GoCreateArgs     { public string name; public string parentPath; } // parentPath: null/빈=루트
    [Serializable] public class GoCreateResult   { public string path; public int instanceId; }

    [Serializable] public class GoFindArgs       { public string path; }
    [Serializable] public class GoFindResult     { public bool found; public int instanceId; }

    [Serializable] public class GoDeleteArgs     { public string path; }
    [Serializable] public class GoDeleteResult   { public bool deleted; }

    [Serializable]
    public class GoSetTransformArgs
    {
        public string path;
        public float[] position; // 길이 3, null 가능 (변경 안함)
        public float[] rotation; // 길이 3 (euler), null 가능
        public float[] scale;    // 길이 3, null 가능
    }
    [Serializable] public class GoSetTransformResult { public string path; }

    // === Component ===
    [Serializable] public class CompAddArgs      { public string path; public string type; } // type: 풀네임 (예: "UnityEngine.UI.Canvas")
    [Serializable] public class CompAddResult    { public string type; }

    [Serializable] public class CompSetFieldArgs { public string path; public string type; public string field; public string valueJson; public string valueType; }
    [Serializable] public class CompSetFieldResult { public string field; }

    [Serializable] public class CompGetFieldArgs { public string path; public string type; public string field; }
    [Serializable] public class CompGetFieldResult { public string valueJson; public string valueType; }

    // === Reflection (탈출구) ===
    [Serializable]
    public class ReflectionInvokeArgs
    {
        public string typeName;        // 풀네임 (예: "UnityEditor.AssetDatabase")
        public string methodName;
        public string targetInstanceId; // 인스턴스 대상. 정적 메서드면 비움
        public string[] argTypes;       // 각 인자의 풀네임
        public string[] argsJson;       // 각 인자의 JsonUtility 직렬화 or 원시 문자열/숫자
    }
    [Serializable] public class ReflectionInvokeResult { public string returnJson; public string returnType; }

    // === Asset ===
    [Serializable] public class AssetRefreshArgs   { }
    [Serializable] public class AssetRefreshResult { public bool refreshed; }

    [Serializable] public class AssetCreatePrefabArgs   { public string goPath; public string prefabPath; }
    [Serializable] public class AssetCreatePrefabResult { public string prefabPath; }

    // === Prefab (stage-mode + nested instance) ===
    [Serializable] public class PrefabOpenArgs        { public string path; }
    [Serializable] public class PrefabOpenResult      { public string rootPath; public int rootInstanceId; public string prefabPath; }

    [Serializable] public class PrefabSaveArgs        { }
    [Serializable] public class PrefabSaveResult      { public string prefabPath; }

    [Serializable] public class PrefabCloseArgs       { public bool save; }
    [Serializable] public class PrefabCloseResult     { public bool closed; public bool saved; }

    [Serializable] public class PrefabGetCurrentArgs  { }
    [Serializable] public class PrefabGetCurrentResult { public bool isOpen; public string prefabPath; public string rootPath; public int rootInstanceId; }

    /// <summary>nested 프리팹: 원본 링크 유지한 채 자식으로 인스턴스화.</summary>
    [Serializable] public class PrefabInstantiateArgs   { public string prefabPath; public string parentPath; public string name; }
    [Serializable] public class PrefabInstantiateResult { public string path; public int instanceId; }

    [Serializable] public class PrefabApplyArgs       { public string path; } // 경로는 prefab instance 루트
    [Serializable] public class PrefabApplyResult     { public bool applied; }

    [Serializable] public class PrefabUnpackArgs      { public string path; public bool completely; }
    [Serializable] public class PrefabUnpackResult    { public bool unpacked; }

    /// <summary>
    /// Variant 프리팹 생성. 소스 프리팹을 상속받는 새 프리팹이 만들어지며
    /// 오버라이드만 변이하고 나머지는 원본 변경 시 자동 반영된다.
    /// </summary>
    [Serializable] public class PrefabCreateVariantArgs   { public string sourcePath; public string variantPath; }
    [Serializable] public class PrefabCreateVariantResult { public string variantPath; }

    // === Sprite (SVG → Texture2D → PNG 임포트) ===
    // Unity Vector Graphics 패키지(com.unity.vectorgraphics)로 SVG를 파싱·테셀레이트해
    // Texture2D로 렌더링 후 PNG로 저장, 결과 파일을 Sprite 로 임포트한다. 외부 바이너리(magick/rsvg) 불필요.
    [Serializable]
    public class SpriteImportSvgArgs
    {
        public string svgText;          // 인라인 SVG 문자열 (svgPath보다 우선)
        public string svgPath;          // 프로젝트 루트 기준 .svg 경로 (없으면 svgText 사용)
        public string pngPath;          // 출력 PNG 경로. "Assets/..." 아래여야 임포트됨. 필수.
        public int    width;            // 힌트. 출력은 정방형 POT 로 강제 (NextPowerOfTwo(max(w,h))). 0이면 256.
        public int    height;           // 힌트. 위와 동일. 0이면 256.
        public float  pixelsPerUnit;    // Sprite PPU. 0이면 100.
        public string filterMode;       // "Point"|"Bilinear"|"Trilinear". 빈 문자열이면 Bilinear.
        public string compression;      // "None"|"LowQuality"|"NormalQuality"|"HighQuality". 빈 문자열이면 None.
        public int    antiAliasing;     // MSAA 샘플 (1/2/4/8). 0이면 4.
        public bool   saveSvgSource;    // true면 PNG 옆에 .svg 원본 동시 저장 (svgText 사용 시).
    }

    [Serializable]
    public class SpriteImportSvgResult
    {
        public string pngPath;
        public string svgPath;          // saveSvgSource=true 이고 svgText를 썼을 때만 채워짐
        public int    width;
        public int    height;
        public float  pixelsPerUnit;
    }

    // === Component: RectTransform 편의 ===
    // UGUI 씬/프리팹 조립 시 anchor/pivot/size를 한 호출에 세팅.
    // null/길이2 아닌 배열은 "변경 안함". offsetMin/Max는 anchor 기반 좌표, 동시 지정 시 size/anchoredPosition보다 나중에 적용됨.
    [Serializable]
    public class CompSetRectTransformArgs
    {
        public string path;
        public float[] anchorMin;        // (x, y)
        public float[] anchorMax;        // (x, y)
        public float[] pivot;            // (x, y)
        public float[] anchoredPosition; // (x, y)
        public float[] sizeDelta;        // (w, h)
        public float[] offsetMin;        // (x, y)
        public float[] offsetMax;        // (x, y)
    }
    [Serializable] public class CompSetRectTransformResult { public string path; }
}
