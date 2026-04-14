using UnityEngine;
using UnityEngine.UI;
using Project.Core;

namespace Project.UI
{
    public class ScoreUI : MonoBehaviour
    {
        private Text _scoreText;

        public void Initialize(Text scoreText)
        {
            _scoreText = scoreText;
            UpdateScore(GameState.Instance.FishEaten);
            GameState.Instance.OnFishEaten += UpdateScore;
        }

        private void UpdateScore(int fishEaten)
        {
            if (_scoreText != null)
            {
                _scoreText.text = $"Fish: {fishEaten} / {GameConfig.FISH_TO_WIN}";
            }
        }

        private void OnDestroy()
        {
            if (GameState.Instance != null)
            {
                GameState.Instance.OnFishEaten -= UpdateScore;
            }
        }
    }
}
