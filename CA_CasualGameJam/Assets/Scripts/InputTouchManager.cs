using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;

public class InputTouchManager : MonoBehaviour
{
    [SerializeField] private Camera _camera;
    [SerializeField] private GameManager _gameManager;
    [SerializeField] private LayerMask _tileLayerMask = ~0;
    private int _activeFingerId = -1;
    private bool _mouseActive;
    private bool _touchWasPresent;
    private readonly List<RaycastResult> _uiHits = new List<RaycastResult>();
    private void Update()
    {
        if (_gameManager == null || _camera == null) return;
        if (Input.touchCount > 0 || _activeFingerId >= 0)
        {
            _touchWasPresent = true;
            HandleTouches(); return;
        }
        if (_touchWasPresent) { _touchWasPresent = false; return; }
        if (Input.GetMouseButtonDown(0)) _mouseActive = BeginPointer(Input.mousePosition, -1);
        if (!_mouseActive) return;
        MovePointer(Input.mousePosition);
        if (Input.GetMouseButtonUp(0)) { _mouseActive = false; _gameManager.CommitLineAsync().Forget(); }
    }
    private void HandleTouches()
    {
        if (_activeFingerId < 0)
        {
            if (Input.touchCount != 1) return;
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began && BeginPointer(touch.position, touch.fingerId))
                _activeFingerId = touch.fingerId;
            return;
        }
        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch touch = Input.GetTouch(i);
            if (touch.fingerId != _activeFingerId) continue;
            if (touch.phase == TouchPhase.Canceled) { CancelPointer(); return; }
            MovePointer(touch.position);
            if (touch.phase == TouchPhase.Ended)
            {
                _activeFingerId = -1;
                _gameManager.CommitLineAsync().Forget();
            }
            return;
        }
        CancelPointer();
    }
    public bool BeginPointer(Vector2 position, int pointerId)
    {
        if (_gameManager == null || _camera == null) return false;
        if (EventSystem.current != null)
        {
            var pointer = new PointerEventData(EventSystem.current) { position = position, pointerId = pointerId };
            _uiHits.Clear();
            EventSystem.current.RaycastAll(pointer, _uiHits);
            if (_uiHits.Count > 0) return false;
        }
        TileController tile = HitTile(position);
        return tile != null && _gameManager.TryBeginLine(tile.TileIndex);
    }
    public void MovePointer(Vector2 position)
    {
        if (_gameManager == null || _gameManager.State != GameState.Drawing) return;
        TileController tile = HitTile(position);
        if (tile != null) _gameManager.TryExtendLine(tile.TileIndex);
    }
    private TileController HitTile(Vector2 position)
    {
        if (_camera == null) return null;
        Ray ray = _camera.ScreenPointToRay(position);
        var plane = new Plane(Vector3.forward, Vector3.zero);
        if (!plane.Raycast(ray, out float distance)) return null;
        Collider2D hit = Physics2D.OverlapPoint(ray.GetPoint(distance), _tileLayerMask);
        return hit != null ? hit.GetComponent<TileController>() : null;
    }
    public void CancelPointer()
    {
        _activeFingerId = -1; _mouseActive = false;
        if (_gameManager != null) _gameManager.CancelLine();
    }
    private void OnApplicationFocus(bool focused) { if (!focused) CancelPointer(); }
    private void OnApplicationPause(bool paused) { if (paused) CancelPointer(); }
    private void OnDisable() => CancelPointer();
}
