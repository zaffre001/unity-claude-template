using System;
using System.IO;
using Unity.VectorGraphics;
using UnityEditor;
using UnityEngine;

namespace Project.Editor.ClaudeBridge.Ops
{
    /// <summary>
    /// SVG → Texture2D → PNG → Sprite 임포트. 외부 바이너리(ImageMagick/rsvg-convert/qlmanage) 의존 제거.
    ///
    /// 파이프라인:
    ///   1) SVGParser.ImportSVG 로 SVG 문자열 파싱 → Scene
    ///   2) VectorUtils.TessellateScene 으로 지오메트리 생성
    ///   3) VectorUtils.FillMesh 로 Mesh 직접 조립 (Sprite 경로 우회 — Unity 6 에서
    ///      BuildSprite 내부의 Sprite.OverrideGeometry 가 빈 이름 sprite에서 거부됨)
    ///   4) RenderTexture + GL 직교 투영으로 Mesh 직접 렌더 (MSAA 지원, SVG Y-down 를 뒤집어 맞춤)
    ///   5) Texture2D.ReadPixels + EncodeToPNG() → 디스크 저장
    ///   6) AssetDatabase.ImportAsset + TextureImporter 로 Sprite 임포트 옵션 적용
    /// </summary>
    public static class SvgOps
    {
        public static string ImportFromSvg(string argsJson)
        {
            var a = JsonUtility.FromJson<SpriteImportSvgArgs>(argsJson);
            if (string.IsNullOrEmpty(a.pngPath)) throw new ArgumentException("pngPath required");
            if (!a.pngPath.Replace('\\', '/').StartsWith("Assets/"))
                throw new ArgumentException($"pngPath must be under Assets/: {a.pngPath}");

            string svgText = !string.IsNullOrEmpty(a.svgText) ? a.svgText : ReadSvgFromPath(a.svgPath);
            if (string.IsNullOrEmpty(svgText))
                throw new ArgumentException("Either svgText or svgPath is required");

            int width  = a.width  > 0 ? a.width  : 256;
            int height = a.height > 0 ? a.height : 256;
            float ppu  = a.pixelsPerUnit > 0 ? a.pixelsPerUnit : 100f;
            int aa     = a.antiAliasing > 0 ? a.antiAliasing : 4;

            // 1) Parse
            SVGParser.SceneInfo sceneInfo;
            using (var reader = new StringReader(svgText))
                sceneInfo = SVGParser.ImportSVG(reader);

            // 2) Tessellate — 아이콘용 기본 프로파일. 곡선 샘플링을 촘촘히 해 외곽선 부드럽게.
            var tessOpts = new VectorUtils.TessellationOptions
            {
                StepDistance        = 10.0f,
                MaxCordDeviation    = 0.5f,
                MaxTanAngleDeviation = 0.1f,
                SamplingStepSize    = 0.01f,
            };
            var geoms = VectorUtils.TessellateScene(sceneInfo.Scene, tessOpts);

            // 3) Mesh 조립 — BuildSprite 우회. FillMesh 는 1 unit = 1 SVG px 로 채움.
            var mesh = new Mesh { hideFlags = HideFlags.DontSave };
            VectorUtils.FillMesh(mesh, geoms, 1f);
            mesh.RecalculateBounds();

            // SVG 뷰포트를 우선 — author의 viewBox 의도와 일치. 없으면 mesh.bounds 폴백.
            Rect bounds = sceneInfo.SceneViewport;
            if (bounds.width <= 0f || bounds.height <= 0f)
            {
                var mb = mesh.bounds;
                bounds = new Rect(mb.min.x, mb.min.y, mb.size.x, mb.size.y);
            }
            if (bounds.width <= 0f || bounds.height <= 0f)
                throw new Exception("SVG has empty bounds — no drawable content");

            // 4) Render mesh → RenderTexture
            var shader = Shader.Find("Unlit/VectorGradient") ?? Shader.Find("Unlit/Vector") ?? Shader.Find("Sprites/Default");
            if (shader == null) throw new Exception("Vector graphics shader not found. Is com.unity.vectorgraphics installed?");
            var mat = new Material(shader) { hideFlags = HideFlags.DontSave };

            var rt = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB, aa);
            Texture2D tex;
            var prevActive = RenderTexture.active;
            try
            {
                RenderTexture.active = rt;
                GL.Clear(true, true, new Color(0, 0, 0, 0));

                GL.PushMatrix();
                // SVG 좌표계는 Y-down. 직교 투영에서 top=bounds.yMin, bottom=bounds.yMax 로 둬서 위아래 뒤집음.
                GL.LoadProjectionMatrix(Matrix4x4.Ortho(
                    bounds.xMin, bounds.xMax,
                    bounds.yMax, bounds.yMin,
                    -1f, 1f));
                mat.SetPass(0);
                Graphics.DrawMeshNow(mesh, Matrix4x4.identity);
                GL.PopMatrix();

                tex = new Texture2D(width, height, TextureFormat.RGBA32, false, false);
                tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                tex.Apply();
            }
            finally
            {
                RenderTexture.active = prevActive;
                RenderTexture.ReleaseTemporary(rt);
                UnityEngine.Object.DestroyImmediate(mesh);
                UnityEngine.Object.DestroyImmediate(mat);
            }

            // 5) Encode + write PNG
            byte[] pngBytes;
            try { pngBytes = tex.EncodeToPNG(); }
            finally { UnityEngine.Object.DestroyImmediate(tex); }

            string absPng = AbsFromProjectRelative(a.pngPath);
            var pngDir = Path.GetDirectoryName(absPng);
            if (!string.IsNullOrEmpty(pngDir)) Directory.CreateDirectory(pngDir);
            File.WriteAllBytes(absPng, pngBytes);

            // 선택적으로 SVG 원본도 함께 저장 (svgText 로 받은 경우에만 의미 있음)
            string savedSvgRel = null;
            if (a.saveSvgSource && !string.IsNullOrEmpty(a.svgText))
            {
                savedSvgRel = Path.ChangeExtension(a.pngPath, ".svg").Replace('\\', '/');
                File.WriteAllText(AbsFromProjectRelative(savedSvgRel), svgText);
            }

            // 6) Import as Sprite
            AssetDatabase.ImportAsset(a.pngPath, ImportAssetOptions.ForceSynchronousImport);
            var importer = AssetImporter.GetAtPath(a.pngPath) as TextureImporter
                ?? throw new Exception($"TextureImporter not found at {a.pngPath}");

            importer.textureType        = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = ppu;
            importer.filterMode         = ParseFilter(a.filterMode);
            importer.textureCompression = ParseCompression(a.compression);
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled      = false;
            importer.SaveAndReimport();

            if (savedSvgRel != null)
                AssetDatabase.ImportAsset(savedSvgRel, ImportAssetOptions.ForceSynchronousImport);

            return JsonUtility.ToJson(new SpriteImportSvgResult
            {
                pngPath       = a.pngPath,
                svgPath       = savedSvgRel,
                width         = width,
                height        = height,
                pixelsPerUnit = ppu,
            });
        }

        static string ReadSvgFromPath(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;
            var abs = AbsFromProjectRelative(path);
            if (!File.Exists(abs)) throw new FileNotFoundException($"SVG not found: {path}");
            return File.ReadAllText(abs);
        }

        static string AbsFromProjectRelative(string rel)
        {
            if (Path.IsPathRooted(rel)) return rel;
            var projectRoot = Path.GetDirectoryName(Application.dataPath); // .../Project
            return Path.GetFullPath(Path.Combine(projectRoot, rel));
        }

        static FilterMode ParseFilter(string s)
        {
            if (string.IsNullOrEmpty(s)) return FilterMode.Bilinear;
            return (FilterMode)Enum.Parse(typeof(FilterMode), s, true);
        }

        static TextureImporterCompression ParseCompression(string s)
        {
            if (string.IsNullOrEmpty(s)) return TextureImporterCompression.Uncompressed;
            switch (s.ToLowerInvariant())
            {
                case "none": return TextureImporterCompression.Uncompressed;
                case "lowquality": return TextureImporterCompression.CompressedLQ;
                case "normalquality": return TextureImporterCompression.Compressed;
                case "highquality": return TextureImporterCompression.CompressedHQ;
                default: throw new ArgumentException($"Unknown compression: {s}");
            }
        }
    }
}
