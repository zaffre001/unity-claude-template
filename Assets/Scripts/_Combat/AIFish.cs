using UnityEngine;
using Project.Core;

namespace Project.Combat
{
    [RequireComponent(typeof(CircleCollider2D))]
    [RequireComponent(typeof(Rigidbody2D))]
    public class AIFish : MonoBehaviour
    {
        public float currentSize = 1.0f;

        private float _speed;
        private Vector2 _targetPoint;
        private SpriteRenderer _spriteRenderer;

        private void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();

            var col = GetComponent<CircleCollider2D>();
            col.isTrigger = true;

            var rb = GetComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;

            _speed = GameConfig.ENEMY_BASE_SPEED * Random.Range(0.7f, 1.3f);
            PickNewTarget();
        }

        public void Initialize(float size)
        {
            currentSize = size;
            transform.localScale = Vector3.one * size;
        }

        private void Update()
        {
            Vector2 pos = transform.position;
            Vector2 dir = (_targetPoint - pos);
            float dist = dir.magnitude;

            if (dist < 0.2f)
            {
                PickNewTarget();
                return;
            }

            dir /= dist;
            pos += dir * _speed * Time.deltaTime;

            float halfW = GameConfig.AQUARIUM_WIDTH * 0.5f;
            float halfH = GameConfig.AQUARIUM_HEIGHT * 0.5f;
            pos.x = Mathf.Clamp(pos.x, -halfW, halfW);
            pos.y = Mathf.Clamp(pos.y, -halfH, halfH);

            transform.position = new Vector3(pos.x, pos.y, transform.position.z);

            if (_spriteRenderer != null && Mathf.Abs(dir.x) > 0.01f)
            {
                _spriteRenderer.flipX = dir.x < 0f;
            }
        }

        private void PickNewTarget()
        {
            float halfW = GameConfig.AQUARIUM_WIDTH * 0.5f;
            float halfH = GameConfig.AQUARIUM_HEIGHT * 0.5f;
            _targetPoint = new Vector2(
                Random.Range(-halfW, halfW),
                Random.Range(-halfH, halfH)
            );
        }
    }
}
