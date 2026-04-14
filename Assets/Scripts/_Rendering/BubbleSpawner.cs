using UnityEngine;
using Project.Core;

namespace Project.Rendering
{
    public class BubbleSpawner : MonoBehaviour
    {
        [SerializeField] private Sprite[] _bubbleSprites;
        [SerializeField] private float _spawnIntervalMin = 0.5f;
        [SerializeField] private float _spawnIntervalMax = 2f;
        [SerializeField] private float _floatSpeedMin = 1f;
        [SerializeField] private float _floatSpeedMax = 2.5f;
        [SerializeField] private float _wobbleAmplitude = 0.3f;
        [SerializeField] private float _wobbleFrequency = 2f;
        [SerializeField] private int _maxActiveBubbles = 20;

        private static int _activeBubbleCount;

        private float _spawnTimer;
        private float _nextSpawnInterval;

        private const float HALF_WIDTH = GameConfig.AQUARIUM_WIDTH / 2f;
        private const float HALF_HEIGHT = GameConfig.AQUARIUM_HEIGHT / 2f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _activeBubbleCount = 0;
        }

        private void Start()
        {
            _nextSpawnInterval = Random.Range(_spawnIntervalMin, _spawnIntervalMax);
        }

        private void Update()
        {
            _spawnTimer += Time.deltaTime;

            if (_spawnTimer >= _nextSpawnInterval)
            {
                _spawnTimer = 0f;
                _nextSpawnInterval = Random.Range(_spawnIntervalMin, _spawnIntervalMax);

                if (_activeBubbleCount < _maxActiveBubbles)
                    SpawnBubble();
            }
        }

        private void SpawnBubble()
        {
            if (_bubbleSprites == null || _bubbleSprites.Length == 0) return;

            var sprite = _bubbleSprites[Random.Range(0, _bubbleSprites.Length)];
            float x = Random.Range(-HALF_WIDTH + 0.5f, HALF_WIDTH - 0.5f);
            float y = -HALF_HEIGHT;

            var go = new GameObject("Bubble");
            go.transform.SetParent(transform);
            go.transform.position = new Vector3(x, y, 0f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = 5;

            float scale = Random.Range(0.3f, 1f);
            go.transform.localScale = new Vector3(scale, scale, 1f);

            var mover = go.AddComponent<BubbleMover>();
            mover.Initialize(
                Random.Range(_floatSpeedMin, _floatSpeedMax),
                _wobbleAmplitude, _wobbleFrequency, HALF_HEIGHT, x);

            _activeBubbleCount++;
        }

        public static void OnBubbleDestroyed()
        {
            _activeBubbleCount = Mathf.Max(0, _activeBubbleCount - 1);
        }
    }

    public class BubbleMover : MonoBehaviour
    {
        private float _floatSpeed;
        private float _wobbleAmplitude;
        private float _wobbleFrequency;
        private float _topBoundary;
        private float _originX;
        private float _elapsedTime;

        public void Initialize(float floatSpeed, float wobbleAmplitude,
            float wobbleFrequency, float topBoundary, float originX)
        {
            _floatSpeed = floatSpeed;
            _wobbleAmplitude = wobbleAmplitude;
            _wobbleFrequency = wobbleFrequency;
            _topBoundary = topBoundary;
            _originX = originX;
        }

        private void Update()
        {
            _elapsedTime += Time.deltaTime;

            float x = _originX + Mathf.Sin(_elapsedTime * _wobbleFrequency) * _wobbleAmplitude;
            float y = transform.position.y + _floatSpeed * Time.deltaTime;

            transform.position = new Vector3(x, y, transform.position.z);

            if (y >= _topBoundary)
            {
                BubbleSpawner.OnBubbleDestroyed();
                Destroy(gameObject);
            }
        }
    }
}
