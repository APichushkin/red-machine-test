using Connection;
using Player;
using Player.ActionHandlers;
using UnityEngine;
using UnityEngine.EventSystems;
using Utils.Scenes;
using Utils.Singleton;

namespace Camera
{
    public class CameraMover : DontDestroyMonoBehaviourSingleton<CameraMover>
    {
        [SerializeField] private float dragSpeed = 2f;
        [SerializeField] private float smoothSpeed = 0.125f;
        [SerializeField] private float boundNodePadding = 2f;

        private UnityEngine.Camera _mainCamera;
        private ClickHandler _clickHandler;

        private Vector3 _dragOrigin;
        private Vector3 _dragTargetPosition;

        private bool _isDragging = false;
        private bool _isMoving = false;

        private Vector3 _minBounds;
        private Vector3 _maxBounds;

        private Vector3 _baseMinBounds;
        private Vector3 _baseMaxBounds;

        private void Start()
        {
            _mainCamera = CameraHolder.Instance.MainCamera;
            _clickHandler = ClickHandler.Instance;
            _clickHandler.AddDragEventHandlers(OnDragStart, OnDragEnd);
            ScenesChanger.SceneLoadedEvent += OnSceneLoaded;

            _dragTargetPosition = _mainCamera.transform.position;
            SetBaseCameraBounds();
        }

        private void OnDestroy()
        {
            _clickHandler.ClearEvents();
            ScenesChanger.SceneLoadedEvent -= OnSceneLoaded;
        }

        private void Update()
        {
            OnDrag();
            MoveCamera();
        }

        private void OnDragStart(Vector3 startPosition)
        {
            if (PlayerController.PlayerState != PlayerState.None || EventSystem.current.IsPointerOverGameObject())
                return;

            _dragOrigin = startPosition;
            _isDragging = true;
            _isMoving = true;
        }

        private void OnDrag()
        {
            if (_isDragging)
                CalculateCameraPosition();
        }

        private void OnDragEnd(Vector3 finishPosition)
        {
            if (_isDragging)
                _isDragging = false;
        }

        private void CalculateCameraPosition()
        {
            var difference = _mainCamera.transform.position + _dragOrigin - _mainCamera.ScreenToWorldPoint(Input.mousePosition);
            _dragTargetPosition = new Vector3(difference.x, difference.y, _mainCamera.transform.position.z);
            _dragTargetPosition = ClampCameraPosition(_dragTargetPosition);
        }

        private void MoveCamera()
        {
            if (_isMoving)
            {
                _mainCamera.transform.position = Vector3.Lerp(_mainCamera.transform.position, _dragTargetPosition, smoothSpeed);
                if (Vector3.Distance(_mainCamera.transform.position, _dragTargetPosition) < 0.01f && !_isDragging)
                    _isMoving = false;
            }
        }

        private void OnSceneLoaded()
        {
            _mainCamera.transform.position = new Vector3(0, 0, _mainCamera.transform.position.z);
        }

        private void SetBaseCameraBounds()
        {
            float camHalfHeight = _mainCamera.orthographicSize;
            float camHalfWidth = camHalfHeight * _mainCamera.aspect;

            Vector3 camPosition = _mainCamera.transform.position;

            _baseMinBounds = new Vector3(camPosition.x - camHalfWidth, camPosition.y - camHalfHeight, camPosition.z);
            _baseMaxBounds = new Vector3(camPosition.x + camHalfWidth, camPosition.y + camHalfHeight, camPosition.z);
        }

        public void SetCameraBounds(ColorNode[] nodes)
        {
            _minBounds = nodes[0].transform.position;
            _maxBounds = nodes[0].transform.position;

            foreach (var node in nodes)
            {
                Vector3 position = node.transform.position;

                _minBounds = Vector3.Min(_minBounds, position);
                _maxBounds = Vector3.Max(_maxBounds, position);
            }
            _minBounds -= new Vector3(boundNodePadding, boundNodePadding, 0);
            _maxBounds += new Vector3(boundNodePadding, boundNodePadding, 0);

            _minBounds.x = Mathf.Min(_minBounds.x, _baseMinBounds.x);
            _minBounds.y = Mathf.Min(_minBounds.y, _baseMinBounds.y);

            _maxBounds.x = Mathf.Max(_maxBounds.x, _baseMaxBounds.x);
            _maxBounds.y = Mathf.Max(_maxBounds.y, _baseMaxBounds.y);
        }

        private Vector3 ClampCameraPosition(Vector3 targetPosition)
        {
            float camHalfHeight = _mainCamera.orthographicSize;
            float camHalfWidth = camHalfHeight * _mainCamera.aspect;

            float clampedX = Mathf.Clamp(targetPosition.x, _minBounds.x + camHalfWidth, _maxBounds.x - camHalfWidth);
            float clampedY = Mathf.Clamp(targetPosition.y, _minBounds.y + camHalfHeight, _maxBounds.y - camHalfHeight);

            return new Vector3(clampedX, clampedY, targetPosition.z);
        }
    }
}
