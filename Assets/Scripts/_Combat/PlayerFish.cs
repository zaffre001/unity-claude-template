using UnityEngine;
using Project.Core;

namespace Project.Combat
{
    [RequireComponent(typeof(CircleCollider2D))]
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerFish : MonoBehaviour
    {
        public float currentSize = 1.0f;

        private SpriteRenderer _spriteRenderer;

        private void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();

            var col = GetComponent<CircleCollider2D>();
            col.isTrigger = true;

            var rb = GetComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        private void Update()
        {
            float h = Input.GetAxis("Horizontal");
            float v = Input.GetAxis("Vertical");

            Vector2 dir = new Vector2(h, v);
            if (dir.sqrMagnitude > 1f)
                dir.Normalize();

            Vector3 pos = transform.position;
            pos += (Vector3)(dir * GameConfig.PLAYER_BASE_SPEED * Time.deltaTime);

            float halfW = GameConfig.AQUARIUM_WIDTH * 0.5f;
            float halfH = GameConfig.AQUARIUM_HEIGHT * 0.5f;
            pos.x = Mathf.Clamp(pos.x, -halfW, halfW);
            pos.y = Mathf.Clamp(pos.y, -halfH, halfH);

            transform.position = pos;

            if (_spriteRenderer != null && Mathf.Abs(h) > 0.01f)
            {
                _spriteRenderer.flipX = h < 0f;
            }
        }

        public void Grow()
        {
            currentSize += GameConfig.GROW_PER_EAT;
            transform.localScale = Vector3.one * currentSize;
        }
    }
}
