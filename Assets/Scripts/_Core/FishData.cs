using UnityEngine;

namespace Project.Core
{
    public enum FishType
    {
        Blue,
        Brown,
        Green,
        Grey,
        GreyLongA,
        GreyLongB,
        Orange,
        Pink,
        Red
    }

    [CreateAssetMenu(fileName = "NewFishData", menuName = "Project/Fish Data")]
    public class FishData : ScriptableObject
    {
        [SerializeField] private FishType _fishType;
        [SerializeField] private float _moveSpeed;
        [SerializeField] private float _size = 1f;
        [SerializeField] private Sprite _sprite;
        [SerializeField] private bool _isPlayerControllable;

        public FishType FishType => _fishType;
        public float MoveSpeed => _moveSpeed;
        public float Size => _size;
        public Sprite Sprite => _sprite;
        public bool IsPlayerControllable => _isPlayerControllable;
    }
}
