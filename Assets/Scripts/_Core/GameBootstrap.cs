using UnityEngine;
using Project.Core;

namespace Project.Core
{
    public class GameBootstrap : MonoBehaviour
    {
        [Header("Fish Sprites")]
        [SerializeField] private Sprite[] _fishSprites;

        [Header("Player Settings")]
        [SerializeField] private Sprite _playerSprite;

        private void Start()
        {
            GameState.Instance.Reset();
            SpawnPlayerFish();
            SpawnEnemyFish();
        }

        private void SpawnPlayerFish()
        {
            var playerObj = new GameObject("PlayerFish");
            
            var sr = playerObj.AddComponent<SpriteRenderer>();
            sr.sprite = _playerSprite;
            sr.sortingOrder = 10;

            playerObj.transform.position = Vector3.zero;

            // PlayerFish and EatSystem are in Project.Combat assembly
            // We add them via AddComponent by type name since this script is in Core
            var playerFishType = System.Type.GetType("Project.Combat.PlayerFish, Project.Combat");
            var eatSystemType = System.Type.GetType("Project.Combat.EatSystem, Project.Combat");
            
            if (playerFishType != null) playerObj.AddComponent(playerFishType);
            if (eatSystemType != null) playerObj.AddComponent(eatSystemType);
        }

        private void SpawnEnemyFish()
        {
            float halfW = GameConfig.AQUARIUM_WIDTH * 0.5f - 1f;
            float halfH = GameConfig.AQUARIUM_HEIGHT * 0.5f - 1f;

            var aiFishType = System.Type.GetType("Project.Combat.AIFish, Project.Combat");

            for (int i = 0; i < GameConfig.ENEMY_FISH_COUNT; i++)
            {
                var fishObj = new GameObject($"EnemyFish_{i}");

                var sr = fishObj.AddComponent<SpriteRenderer>();
                if (_fishSprites != null && _fishSprites.Length > 0)
                    sr.sprite = _fishSprites[Random.Range(0, _fishSprites.Length)];
                sr.sortingOrder = 9;

                fishObj.transform.position = new Vector3(
                    Random.Range(-halfW, halfW),
                    Random.Range(-halfH, halfH),
                    0f
                );

                if (aiFishType != null)
                {
                    var aiComp = fishObj.AddComponent(aiFishType);
                    // Call Initialize via reflection
                    float size = Random.Range(0.5f, 1.5f);
                    var initMethod = aiFishType.GetMethod("Initialize");
                    if (initMethod != null)
                        initMethod.Invoke(aiComp, new object[] { size });
                }
            }
        }
    }
}
