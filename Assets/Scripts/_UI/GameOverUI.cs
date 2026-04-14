using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Project.Core;

namespace Project.UI
{
    public class GameOverUI : MonoBehaviour
    {
        private GameObject _panel;
        private Text _messageText;
        private Button _restartButton;

        public void Initialize(GameObject panel, Text messageText, Button restartButton)
        {
            _panel = panel;
            _messageText = messageText;
            _restartButton = restartButton;

            _panel.SetActive(false);

            GameState.Instance.OnGameWon += HandleGameWon;
            GameState.Instance.OnGameOver += HandleGameOver;
            _restartButton.onClick.AddListener(HandleRestart);
        }

        private void HandleGameWon()
        {
            if (_messageText != null)
            {
                _messageText.text = $"You Win! Ate {GameConfig.FISH_TO_WIN} fish!";
            }
            if (_panel != null)
            {
                _panel.SetActive(true);
            }
        }

        private void HandleGameOver()
        {
            if (_messageText != null)
            {
                _messageText.text = "Game Over! Eaten by a bigger fish!";
            }
            if (_panel != null)
            {
                _panel.SetActive(true);
            }
        }

        private void HandleRestart()
        {
            GameState.Instance.Reset();
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private void OnDestroy()
        {
            if (GameState.Instance != null)
            {
                GameState.Instance.OnGameWon -= HandleGameWon;
                GameState.Instance.OnGameOver -= HandleGameOver;
            }
            if (_restartButton != null)
            {
                _restartButton.onClick.RemoveListener(HandleRestart);
            }
        }
    }
}
