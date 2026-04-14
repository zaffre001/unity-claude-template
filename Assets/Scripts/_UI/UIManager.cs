using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Project.UI
{
    public class UIManager : MonoBehaviour
    {
        private static UIManager _instance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _instance = null;
        }

        private Canvas _canvas;
        private ScoreUI _scoreUI;
        private GameOverUI _gameOverUI;
        private MainMenuUI _mainMenuUI;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;

            EnsureEventSystem();
            CreateCanvas();
            CreateScoreUI();
            CreateGameOverUI();
            CreateMainMenuUI();
        }

        private void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() == null)
            {
                var eventSystemObj = new GameObject("EventSystem");
                eventSystemObj.transform.SetParent(transform);
                eventSystemObj.AddComponent<EventSystem>();
                eventSystemObj.AddComponent<StandaloneInputModule>();
            }
        }

        private void CreateCanvas()
        {
            var canvasObj = new GameObject("UICanvas");
            canvasObj.transform.SetParent(transform);

            _canvas = canvasObj.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 100;

            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();
        }

        private void CreateScoreUI()
        {
            var scoreObj = new GameObject("ScoreUI");
            scoreObj.transform.SetParent(_canvas.transform, false);

            var scoreText = scoreObj.AddComponent<Text>();
            scoreText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            scoreText.fontSize = 32;
            scoreText.color = Color.white;
            scoreText.alignment = TextAnchor.UpperLeft;
            scoreText.horizontalOverflow = HorizontalWrapMode.Overflow;

            var outline = scoreObj.AddComponent<Outline>();
            outline.effectColor = Color.black;
            outline.effectDistance = new Vector2(2, -2);

            var rectTransform = scoreObj.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0, 1);
            rectTransform.anchorMax = new Vector2(0, 1);
            rectTransform.pivot = new Vector2(0, 1);
            rectTransform.anchoredPosition = new Vector2(20, -20);
            rectTransform.sizeDelta = new Vector2(300, 50);

            _scoreUI = scoreObj.AddComponent<ScoreUI>();
            _scoreUI.Initialize(scoreText);
        }

        private void CreateGameOverUI()
        {
            // Panel background
            var panelObj = new GameObject("GameOverPanel");
            panelObj.transform.SetParent(_canvas.transform, false);

            var panelImage = panelObj.AddComponent<Image>();
            panelImage.color = new Color(0, 0, 0, 0.75f);

            var panelRect = panelObj.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            // Message text
            var messageObj = new GameObject("MessageText");
            messageObj.transform.SetParent(panelObj.transform, false);

            var messageText = messageObj.AddComponent<Text>();
            messageText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            messageText.fontSize = 48;
            messageText.color = Color.white;
            messageText.alignment = TextAnchor.MiddleCenter;
            messageText.horizontalOverflow = HorizontalWrapMode.Overflow;

            var messageRect = messageObj.GetComponent<RectTransform>();
            messageRect.anchorMin = new Vector2(0.5f, 0.6f);
            messageRect.anchorMax = new Vector2(0.5f, 0.6f);
            messageRect.pivot = new Vector2(0.5f, 0.5f);
            messageRect.anchoredPosition = Vector2.zero;
            messageRect.sizeDelta = new Vector2(800, 80);

            // Restart button
            var buttonObj = new GameObject("RestartButton");
            buttonObj.transform.SetParent(panelObj.transform, false);

            var buttonImage = buttonObj.AddComponent<Image>();
            buttonImage.color = new Color(0.2f, 0.6f, 0.9f, 1f);

            var button = buttonObj.AddComponent<Button>();
            var buttonColors = button.colors;
            buttonColors.highlightedColor = new Color(0.3f, 0.7f, 1f, 1f);
            buttonColors.pressedColor = new Color(0.15f, 0.45f, 0.7f, 1f);
            button.colors = buttonColors;

            var buttonRect = buttonObj.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0.5f, 0.35f);
            buttonRect.anchorMax = new Vector2(0.5f, 0.35f);
            buttonRect.pivot = new Vector2(0.5f, 0.5f);
            buttonRect.anchoredPosition = Vector2.zero;
            buttonRect.sizeDelta = new Vector2(250, 60);

            var buttonTextObj = new GameObject("ButtonText");
            buttonTextObj.transform.SetParent(buttonObj.transform, false);

            var buttonText = buttonTextObj.AddComponent<Text>();
            buttonText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            buttonText.fontSize = 30;
            buttonText.color = Color.white;
            buttonText.alignment = TextAnchor.MiddleCenter;
            buttonText.text = "Restart";

            var buttonTextRect = buttonTextObj.GetComponent<RectTransform>();
            buttonTextRect.anchorMin = Vector2.zero;
            buttonTextRect.anchorMax = Vector2.one;
            buttonTextRect.offsetMin = Vector2.zero;
            buttonTextRect.offsetMax = Vector2.zero;

            _gameOverUI = panelObj.AddComponent<GameOverUI>();
            _gameOverUI.Initialize(panelObj, messageText, button);
        }

        private void CreateMainMenuUI()
        {
            // Panel background
            var panelObj = new GameObject("MainMenuPanel");
            panelObj.transform.SetParent(_canvas.transform, false);

            var panelImage = panelObj.AddComponent<Image>();
            panelImage.color = new Color(0.05f, 0.15f, 0.3f, 0.95f);

            var panelRect = panelObj.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            // Title text
            var titleObj = new GameObject("TitleText");
            titleObj.transform.SetParent(panelObj.transform, false);

            var titleText = titleObj.AddComponent<Text>();
            titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            titleText.fontSize = 72;
            titleText.color = Color.white;
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.text = "Fish.io";
            titleText.fontStyle = FontStyle.Bold;

            var titleRect = titleObj.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 0.65f);
            titleRect.anchorMax = new Vector2(0.5f, 0.65f);
            titleRect.pivot = new Vector2(0.5f, 0.5f);
            titleRect.anchoredPosition = Vector2.zero;
            titleRect.sizeDelta = new Vector2(600, 100);

            // Start button
            var buttonObj = new GameObject("StartButton");
            buttonObj.transform.SetParent(panelObj.transform, false);

            var buttonImage = buttonObj.AddComponent<Image>();
            buttonImage.color = new Color(0.1f, 0.7f, 0.3f, 1f);

            var button = buttonObj.AddComponent<Button>();
            var buttonColors = button.colors;
            buttonColors.highlightedColor = new Color(0.15f, 0.8f, 0.4f, 1f);
            buttonColors.pressedColor = new Color(0.08f, 0.55f, 0.25f, 1f);
            button.colors = buttonColors;

            var buttonRect = buttonObj.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0.5f, 0.4f);
            buttonRect.anchorMax = new Vector2(0.5f, 0.4f);
            buttonRect.pivot = new Vector2(0.5f, 0.5f);
            buttonRect.anchoredPosition = Vector2.zero;
            buttonRect.sizeDelta = new Vector2(300, 70);

            var buttonTextObj = new GameObject("ButtonText");
            buttonTextObj.transform.SetParent(buttonObj.transform, false);

            var buttonText = buttonTextObj.AddComponent<Text>();
            buttonText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            buttonText.fontSize = 36;
            buttonText.color = Color.white;
            buttonText.alignment = TextAnchor.MiddleCenter;
            buttonText.text = "Start Game";

            var buttonTextRect = buttonTextObj.GetComponent<RectTransform>();
            buttonTextRect.anchorMin = Vector2.zero;
            buttonTextRect.anchorMax = Vector2.one;
            buttonTextRect.offsetMin = Vector2.zero;
            buttonTextRect.offsetMax = Vector2.zero;

            _mainMenuUI = panelObj.AddComponent<MainMenuUI>();
            _mainMenuUI.Initialize(panelObj, button);
        }
    }
}
