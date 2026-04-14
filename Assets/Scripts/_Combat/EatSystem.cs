using System.Collections;
using UnityEngine;
using Project.Core;

namespace Project.Combat
{
    [RequireComponent(typeof(PlayerFish))]
    public class EatSystem : MonoBehaviour
    {
        private PlayerFish _player;
        private bool _isEatAnimating;

        private void Awake()
        {
            _player = GetComponent<PlayerFish>();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (GameState.Instance.IsGameOver || GameState.Instance.IsGameWon)
                return;

            var aiFish = other.GetComponent<AIFish>();
            if (aiFish == null)
                return;

            if (aiFish.currentSize < _player.currentSize * GameConfig.EAT_SIZE_RATIO)
            {
                aiFish.gameObject.SetActive(false);
                _player.Grow();
                GameState.Instance.EatFish();

                if (!_isEatAnimating)
                {
                    StartCoroutine(EatBumpCoroutine());
                }
            }
            else
            {
                GameState.Instance.GameOver();
                gameObject.SetActive(false);
            }
        }

        private IEnumerator EatBumpCoroutine()
        {
            _isEatAnimating = true;

            Vector3 baseScale = transform.localScale;
            transform.localScale = baseScale * 1.2f;

            yield return new WaitForSeconds(0.1f);

            transform.localScale = baseScale;
            _isEatAnimating = false;
        }
    }
}
