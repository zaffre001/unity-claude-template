using UnityEngine;
using Project.Core;

namespace Project.Rendering
{
    [RequireComponent(typeof(Camera))]
    public class CameraSetup : MonoBehaviour
    {
        [SerializeField] private Color _backgroundColor = new Color(0.1f, 0.3f, 0.6f, 1f);
        [SerializeField] private float _margin = 1f;

        private Camera _camera;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
        }

        private void Start()
        {
            SetupCamera();
        }

        private void SetupCamera()
        {
            _camera.orthographic = true;
            _camera.backgroundColor = _backgroundColor;
            _camera.clearFlags = CameraClearFlags.SolidColor;

            transform.position = new Vector3(0f, 0f, -10f);

            float totalWidth = GameConfig.AQUARIUM_WIDTH + _margin * 2f;
            float totalHeight = GameConfig.AQUARIUM_HEIGHT + _margin * 2f;

            float orthoSizeForHeight = totalHeight / 2f;
            float orthoSizeForWidth = totalWidth / (2f * _camera.aspect);

            _camera.orthographicSize = Mathf.Max(orthoSizeForHeight, orthoSizeForWidth);
        }
    }
}
