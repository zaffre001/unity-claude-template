using UnityEngine;
using Project.Core;

namespace Project.Rendering
{
    public class AquariumGenerator : MonoBehaviour
    {
        [Header("Terrain")]
        [SerializeField] private Sprite[] _terrainTopSprites;
        [SerializeField] private Sprite[] _terrainFillSprites;

        [Header("Rocks")]
        [SerializeField] private Sprite[] _rockSprites;
        [SerializeField] private int _rockCountMin = 3;
        [SerializeField] private int _rockCountMax = 6;

        [Header("Seaweed")]
        [SerializeField] private Sprite[] _seaweedSprites;
        [SerializeField] private int _seaweedCountMin = 8;
        [SerializeField] private int _seaweedCountMax = 15;

        [Header("Background Decorations")]
        [SerializeField] private Sprite[] _backgroundRockSprites;
        [SerializeField] private Sprite[] _backgroundSeaweedSprites;

        private const float HALF_WIDTH = GameConfig.AQUARIUM_WIDTH / 2f;
        private const float HALF_HEIGHT = GameConfig.AQUARIUM_HEIGHT / 2f;
        private const float WALL_THICKNESS = 1f;

        private void Start()
        {
            GenerateAquarium();
        }

        private void GenerateAquarium()
        {
            CreateWalls();
            CreateTerrain();
            CreateRocks();
            CreateSeaweed();
        }

        private void CreateWalls()
        {
            var wallsParent = new GameObject("Walls");
            wallsParent.transform.SetParent(transform);

            CreateWall(wallsParent.transform, "Wall_Bottom",
                new Vector2(0f, -HALF_HEIGHT - WALL_THICKNESS / 2f),
                new Vector2(GameConfig.AQUARIUM_WIDTH + WALL_THICKNESS * 2f, WALL_THICKNESS));

            CreateWall(wallsParent.transform, "Wall_Top",
                new Vector2(0f, HALF_HEIGHT + WALL_THICKNESS / 2f),
                new Vector2(GameConfig.AQUARIUM_WIDTH + WALL_THICKNESS * 2f, WALL_THICKNESS));

            CreateWall(wallsParent.transform, "Wall_Left",
                new Vector2(-HALF_WIDTH - WALL_THICKNESS / 2f, 0f),
                new Vector2(WALL_THICKNESS, GameConfig.AQUARIUM_HEIGHT + WALL_THICKNESS * 2f));

            CreateWall(wallsParent.transform, "Wall_Right",
                new Vector2(HALF_WIDTH + WALL_THICKNESS / 2f, 0f),
                new Vector2(WALL_THICKNESS, GameConfig.AQUARIUM_HEIGHT + WALL_THICKNESS * 2f));
        }

        private void CreateWall(Transform parent, string wallName, Vector2 position, Vector2 size)
        {
            var wall = new GameObject(wallName);
            wall.transform.SetParent(parent);
            wall.transform.localPosition = position;

            var collider = wall.AddComponent<BoxCollider2D>();
            collider.size = size;
            collider.isTrigger = false;
        }

        private void CreateTerrain()
        {
            if (_terrainTopSprites == null || _terrainTopSprites.Length == 0) return;

            var terrainParent = new GameObject("Terrain");
            terrainParent.transform.SetParent(transform);

            float bottomY = -HALF_HEIGHT;
            float tileWidth = 1f;
            if (_terrainTopSprites[0] != null)
                tileWidth = _terrainTopSprites[0].bounds.size.x;

            int tileCount = Mathf.CeilToInt(GameConfig.AQUARIUM_WIDTH / tileWidth) + 1;
            float startX = -HALF_WIDTH;

            for (int i = 0; i < tileCount; i++)
            {
                float x = startX + i * tileWidth;
                var topSprite = _terrainTopSprites[Random.Range(0, _terrainTopSprites.Length)];
                CreateSpriteObject(terrainParent.transform, $"TerrainTop_{i}",
                    new Vector3(x, bottomY, 0f), topSprite, 1);

                if (_terrainFillSprites != null && _terrainFillSprites.Length > 0)
                {
                    var fillSprite = _terrainFillSprites[Random.Range(0, _terrainFillSprites.Length)];
                    float fillHeight = fillSprite != null ? fillSprite.bounds.size.y : 1f;
                    CreateSpriteObject(terrainParent.transform, $"TerrainFill_{i}",
                        new Vector3(x, bottomY - fillHeight, 0f), fillSprite, 1);
                }
            }
        }

        private void CreateRocks()
        {
            if (_rockSprites == null || _rockSprites.Length == 0) return;

            var rocksParent = new GameObject("Rocks");
            rocksParent.transform.SetParent(transform);

            int rockCount = Random.Range(_rockCountMin, _rockCountMax + 1);
            float bottomY = -HALF_HEIGHT;

            for (int i = 0; i < rockCount; i++)
            {
                var sprite = _rockSprites[Random.Range(0, _rockSprites.Length)];
                float x = Random.Range(-HALF_WIDTH + 1f, HALF_WIDTH - 1f);
                float y = bottomY + Random.Range(0f, 0.5f);

                var rock = CreateSpriteObject(rocksParent.transform, $"Rock_{i}",
                    new Vector3(x, y, 0f), sprite, 2);

                float scale = Random.Range(0.8f, 1.4f);
                rock.transform.localScale = new Vector3(
                    Random.value > 0.5f ? scale : -scale, scale, 1f);
            }
        }

        private void CreateSeaweed()
        {
            if (_seaweedSprites == null || _seaweedSprites.Length == 0) return;

            var seaweedParent = new GameObject("Seaweed");
            seaweedParent.transform.SetParent(transform);

            int seaweedCount = Random.Range(_seaweedCountMin, _seaweedCountMax + 1);
            float bottomY = -HALF_HEIGHT;

            for (int i = 0; i < seaweedCount; i++)
            {
                var sprite = _seaweedSprites[Random.Range(0, _seaweedSprites.Length)];
                float x = Random.Range(-HALF_WIDTH + 0.5f, HALF_WIDTH - 0.5f);
                float y = bottomY + Random.Range(0f, 0.3f);

                var seaweed = CreateSpriteObject(seaweedParent.transform, $"Seaweed_{i}",
                    new Vector3(x, y, 0f), sprite, 3);

                float scale = Random.Range(0.7f, 1.3f);
                seaweed.transform.localScale = new Vector3(
                    Random.value > 0.5f ? scale : -scale, scale, 1f);
            }
        }

        private GameObject CreateSpriteObject(Transform parent, string objectName,
            Vector3 position, Sprite sprite, int sortingOrder)
        {
            var go = new GameObject(objectName);
            go.transform.SetParent(parent);
            go.transform.localPosition = position;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = sortingOrder;

            return go;
        }
    }
}
