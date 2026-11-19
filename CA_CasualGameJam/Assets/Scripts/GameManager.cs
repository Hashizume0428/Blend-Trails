using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public enum GameState { Idle, Drawing, Resolving, Cleared }

public class GameManager : MonoBehaviour
{
    public List<TileController> TilesList = new List<TileController>();
    [SerializeField] private Vector2Int _boardSize = new Vector2Int(4, 4);
    [SerializeField] private GameHud _hud;
    [SerializeField] private StagePaletteVisuals _paletteVisuals;
    [SerializeField] private BoardLineRenderer _lineVisuals;
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip _tileSound;
    [SerializeField] private AudioClip _clearSound;
    [SerializeField] private AudioClip _buttonSound;
    [SerializeField] private AudioClip _incorrectSound;
    private readonly Dictionary<Vector2Int, TileController> _grid = new Dictionary<Vector2Int, TileController>();
    private readonly Dictionary<BaseColor, TileController> _starts = new Dictionary<BaseColor, TileController>();
    private readonly HashSet<BaseColor> _committedColors = new HashSet<BaseColor>();
    private readonly List<int> _preview = new List<int>();
    private readonly HashSet<int> _drawingPaintedTiles = new HashSet<int>();
    private readonly List<int> _beforeDrawingPath = new List<int>();
    private bool _resumingLine;
    private BaseColor _activeColor;
    private CancellationTokenSource _operation;
    private bool _paletteResolved;

    public event Action Changed;
    public GameState State { get; private set; }
    public bool IsInitialized { get; private set; }
    public StageColorPalette Palette { get; private set; } = StageColorPalette.ForStage(1);
    public StagePaletteVisuals PaletteVisuals => _paletteVisuals;
    public Vector2Int BoardSize => _boardSize;
    public BoardLineRenderer LineVisuals => _lineVisuals;
    public IReadOnlyList<int> PreviewPath => _preview;
    public int CorrectTileCount
    {
        get { int count = 0; foreach (TileController tile in TilesList) if (tile != null && tile.IsTileCorrect()) count++; return count; }
    }
    private void Start() => Initialize();
    public bool Initialize()
    {
        IsInitialized = false;
        if (!_paletteResolved)
        {
            var navigation = GetComponent<StageClearNavigation>();
            Palette = StageColorPalette.ForStage(navigation != null ? navigation.StageNumber : 1);
            _paletteResolved = true;
        }
        _grid.Clear(); _starts.Clear();
        if (_boardSize.x <= 0 || _boardSize.y <= 0 || TilesList.Count != _boardSize.x * _boardSize.y)
            return InvalidBoard("The board must contain one tile for every cell.");
        for (int i = 0; i < TilesList.Count; i++)
        {
            TileController tile = TilesList[i];
            if (tile == null || tile.Coordinate.x < 0 || tile.Coordinate.y < 0 ||
                tile.Coordinate.x >= _boardSize.x || tile.Coordinate.y >= _boardSize.y || _grid.ContainsKey(tile.Coordinate))
                return InvalidBoard("Missing, duplicate or out-of-bounds tile.");
            tile.SetTileIndex(i);
            tile.ApplyPalette(Palette, _paletteVisuals);
            _grid.Add(tile.Coordinate, tile);
            if (!tile.IsStartTile) continue;
            if (tile.Line == null || !Palette.Contains(tile.StartColor) || _starts.ContainsKey(tile.StartColor))
                return InvalidBoard("Each color needs a unique start and line.");
            _starts.Add(tile.StartColor, tile);
        }
        if (_starts.Count == 0) return InvalidBoard("The board needs at least one start tile.");
        if (_lineVisuals == null)
        {
            var visualObject = new GameObject("LineVisuals");
            visualObject.transform.SetParent(transform, false);
            _lineVisuals = visualObject.AddComponent<BoardLineRenderer>();
        }
        foreach (TileController start in _starts.Values)
        {
            _lineVisuals.CopyStyleFrom(start.Line.GetComponent<LineRenderer>());
            break;
        }
        foreach (TileController start in _starts.Values) start.Line.SetComposited(true);
        IsInitialized = true;
        RestartStage();
        return true;
    }
    private bool InvalidBoard(string message)
    {
        Debug.LogError("Puzzle board: " + message, this);
        Changed?.Invoke();
        return false;
    }
    public TileController GetTile(Vector2Int coordinate) => _grid.TryGetValue(coordinate, out TileController tile) ? tile : null;
    public bool HasCommittedLine(BaseColor color) => _committedColors.Contains(color);
    public bool CanReloadColor(BaseColor color) => IsInitialized && State == GameState.Idle && HasCommittedLine(color);
    public bool CanReloadAll => IsInitialized && State == GameState.Idle;
    private LineController ActiveLine => _starts[_activeColor].Line;

    public TileColor GetLineStartColors(int tileIndex)
    {
        if (!IsInitialized || State != GameState.Idle || tileIndex < 0 || tileIndex >= TilesList.Count) return TileColor.None;
        TileController tile = TilesList[tileIndex];
        // An unused start keeps its original meaning, even when another line ends here.
        if (tile.IsStartTile && !HasCommittedLine(tile.StartColor))
            return tile.CanAddColor(tile.StartColor) ? PuzzleRules.ToTileColor(tile.StartColor) : TileColor.None;
        TileColor colors = TileColor.None;
        foreach (var pair in _starts)
        {
            IReadOnlyList<int> path = pair.Value.Line.Path;
            if (HasCommittedLine(pair.Key) && path.Count > 0 && path[path.Count - 1] == tileIndex)
                colors |= PuzzleRules.ToTileColor(pair.Key);
        }
        return colors;
    }
    public bool TryBeginLine(int tileIndex)
    {
        TileColor colors = GetLineStartColors(tileIndex);
        BaseColor? candidate = null;
        foreach (BaseColor color in _starts.Keys)
        {
            if ((colors & PuzzleRules.ToTileColor(color)) == 0) continue;
            if (candidate.HasValue) return false; // Shared endpoints require an explicit choice.
            candidate = color;
        }
        return candidate.HasValue && TryBeginLine(tileIndex, candidate.Value);
    }
    public bool TryBeginLine(int tileIndex, BaseColor color)
    {
        if (!_starts.ContainsKey(color) || (GetLineStartColors(tileIndex) & PuzzleRules.ToTileColor(color)) == 0) return false;
        _activeColor = color;
        _resumingLine = HasCommittedLine(color);
        _preview.Clear(); _drawingPaintedTiles.Clear(); _beforeDrawingPath.Clear();
        if (_resumingLine)
        {
            foreach (int index in ActiveLine.Path)
            {
                _beforeDrawingPath.Add(index); _preview.Add(index); _drawingPaintedTiles.Add(index);
            }
        }
        else _preview.Add(tileIndex);
        State = GameState.Drawing;
        if (!_resumingLine) PaintDrawingTile(TilesList[tileIndex]);
        ActiveLine.ShowPath(_preview, TilesList);
        RefreshLines();
        Changed?.Invoke();
        return true;
    }
    public bool TryExtendLine(int tileIndex)
    {
        if (State != GameState.Drawing || tileIndex < 0 || tileIndex >= TilesList.Count) return false;
        Vector2Int from = TilesList[_preview[_preview.Count - 1]].Coordinate;
        Vector2Int target = TilesList[tileIndex].Coordinate;
        if (!PuzzleRules.TryGetAxisStep(from, target, out Vector2Int step)) return false;
        bool changed = false;
        while (from != target)
        {
            Vector2Int next = from + step;
            TileController tile = GetTile(next);
            if (tile == null) break;
            PathStep action = PuzzleRules.ClassifyStep(_preview, tile.TileIndex, from, next);
            if (action == PathStep.Reject || (action == PathStep.Append && !tile.CanAddColor(_activeColor))) break;
            if (action == PathStep.Backtrack)
            {
                UndoDrawingTile(_preview[_preview.Count - 1]);
                _preview.RemoveAt(_preview.Count - 1);
            }
            else
            {
                _preview.Add(tile.TileIndex);
                PaintDrawingTile(tile);
            }
            from = next; changed = true;
        }
        if (changed)
        {
            ActiveLine.ShowPath(_preview, TilesList);
            RefreshLines();
            Changed?.Invoke();
        }
        return changed;
    }
    public async UniTask CommitLineAsync()
    {
        if (State != GameState.Drawing) return;
        if (_resumingLine && DrawingPathUnchanged())
        {
            ClearDrawingState();
            State = GameState.Idle;
            Changed?.Invoke();
            return;
        }
        State = GameState.Resolving;
        CancellationToken token = _operation.Token;
        BaseColor color = _activeColor;
        ClearDrawingState();
        Changed?.Invoke();
        try
        {
            // Faces already follow the pointer; release only finalizes the line and assessment.
            await UniTask.Delay(120, cancellationToken: token);
            token.ThrowIfCancellationRequested();
            _committedColors.Add(color);
            bool cleared = StageClearCheck();
            if (_committedColors.Count == _starts.Count && !cleared)
            {
                foreach (TileController tile in TilesList) tile.PlayErrorBlink();
                PlaySound(_incorrectSound);
                TileHaptics.ErrorTap();
            }
            if (cleared)
            {
                var navigation = GetComponent<StageClearNavigation>();
                if (navigation != null) StageProgress.RecordClear(navigation.StageNumber);
                PlaySound(_clearSound);
                if (_hud != null) await _hud.PlayClearAsync(token);
                token.ThrowIfCancellationRequested();
                State = GameState.Cleared;
            }
            else State = GameState.Idle;
            Changed?.Invoke();
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { }
    }
    public void CancelLine()
    {
        if (State != GameState.Drawing) return;
        foreach (int index in _preview) UndoDrawingTile(index);
        // Restore only this line's ingredient; crossing colors belong to their own lines.
        foreach (int index in _beforeDrawingPath) TilesList[index].AddColor(_activeColor);
        if (_beforeDrawingPath.Count > 0) ActiveLine.ShowPath(_beforeDrawingPath, TilesList);
        else ActiveLine.ResetToStart(_starts[_activeColor].transform.position);
        RefreshLines();
        ClearDrawingState(); State = GameState.Idle;
        Changed?.Invoke();
    }
    private bool DrawingPathUnchanged()
    {
        if (_beforeDrawingPath.Count != _preview.Count) return false;
        for (int i = 0; i < _preview.Count; i++) if (_preview[i] != _beforeDrawingPath[i]) return false;
        return true;
    }
    private void ClearDrawingState()
    {
        _preview.Clear(); _drawingPaintedTiles.Clear(); _beforeDrawingPath.Clear(); _resumingLine = false;
    }
    private void PaintDrawingTile(TileController tile)
    {
        if (!tile.AddColor(_activeColor)) return;
        _drawingPaintedTiles.Add(tile.TileIndex);
        TileHaptics.LightTap();
        PlaySound(_tileSound);
        tile.PlayFeedback();
    }
    private void UndoDrawingTile(int index)
    {
        if (!_drawingPaintedTiles.Remove(index)) return;
        TileController tile = TilesList[index];
        tile.StopFeedback();
        tile.RemoveColor(_activeColor);
    }
    public bool ReloadColor(BaseColor color)
    {
        if (!CanReloadColor(color)) return false;
        PlaySound(_buttonSound);
        foreach (TileController tile in TilesList)
        {
            tile.StopErrorBlink();
            tile.RemoveColor(color);
        }
        _committedColors.Remove(color);
        _starts[color].Line.ResetToStart(_starts[color].transform.position);
        RefreshLines();
        Changed?.Invoke();
        return true;
    }
    public bool ReloadAll()
    {
        if (!CanReloadAll) return false;
        PlaySound(_buttonSound);
        RestartStage();
        return true;
    }
    public void RestartStage()
    {
        _operation?.Cancel(); _operation?.Dispose();
        _operation = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
        ClearDrawingState(); _committedColors.Clear();
        foreach (TileController tile in TilesList) if (tile != null) tile.ResetRuntimeState();
        foreach (var pair in _starts)
        {
            pair.Value.Line.SetColor(pair.Key);
            pair.Value.Line.ResetToStart(pair.Value.transform.position);
        }
        State = GameState.Idle;
        RefreshLines();
        Changed?.Invoke();
    }
    private void RefreshLines()
    {
        if (_lineVisuals != null) _lineVisuals.Rebuild(TilesList, _starts.Values);
    }
    public bool StageClearCheck()
    {
        if (!IsInitialized || TilesList.Count == 0) return false;
        foreach (TileController tile in TilesList) if (tile == null || !tile.IsTileCorrect()) return false;
        return true;
    }
    private void PlaySound(AudioClip clip)
    {
        if (_audioSource != null && clip != null) _audioSource.PlayOneShot(clip, 0.45f);
    }
    private void OnDestroy() { _operation?.Cancel(); _operation?.Dispose(); }
}
