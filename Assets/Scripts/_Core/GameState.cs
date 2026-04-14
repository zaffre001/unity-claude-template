using System;
using UnityEngine;

namespace Project.Core
{
    public class GameState
    {
        private static GameState _instance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _instance = null;
        }

        public static GameState Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new GameState();
                return _instance;
            }
        }

        public int FishEaten { get; private set; }
        public bool IsGameOver { get; private set; }
        public bool IsGameWon { get; private set; }

        public event Action<int> OnFishEaten;
        public event Action OnGameWon;
        public event Action OnGameOver;

        private GameState() { }

        public void Reset()
        {
            FishEaten = 0;
            IsGameOver = false;
            IsGameWon = false;
            OnFishEaten = null;
            OnGameWon = null;
            OnGameOver = null;
        }

        public void EatFish()
        {
            if (IsGameOver || IsGameWon) return;

            FishEaten++;
            OnFishEaten?.Invoke(FishEaten);

            if (FishEaten >= GameConfig.FISH_TO_WIN)
            {
                IsGameWon = true;
                OnGameWon?.Invoke();
            }
        }

        public void GameOver()
        {
            if (IsGameOver) return;

            IsGameOver = true;
            OnGameOver?.Invoke();
        }
    }
}
