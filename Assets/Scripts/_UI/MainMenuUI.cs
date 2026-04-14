using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace Project.UI
{
    public class MainMenuUI : MonoBehaviour
    {
        private GameObject _panel;
        private Button _startButton;

        public void Initialize(GameObject panel, Button startButton)
        {
            _panel = panel;
            _startButton = startButton;

            _panel.SetActive(true);
            _startButton.onClick.AddListener(HandleStartGame);
        }

        private void HandleStartGame()
        {
            _panel.SetActive(false);

            if (SceneManager.sceneCountInBuildSettings > 1)
            {
                SceneManager.LoadScene(1);
            }
            else
            {
                _panel.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            if (_startButton != null)
            {
                _startButton.onClick.RemoveListener(HandleStartGame);
            }
        }
    }
}
