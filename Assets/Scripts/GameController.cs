using System;
using System.Collections;
using FSharpLib;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameController : MonoBehaviour
{
    private const float DefeatWatchBoardTime = 3;
    private const int DefaultNumberOfMines = 40;
    private static readonly Vector2Int DefaultGridSize = new(16, 16);

    [Title("UI")] [SceneObjectsOnly] [SerializeField]
    private TMP_InputField numberOfMinesField;

    [SceneObjectsOnly] [SerializeField] private GameObject uiScreenParent;
    [SceneObjectsOnly] [SerializeField] private TMP_Text gameResultLabel;
    [SceneObjectsOnly] [SerializeField] private TMP_InputField widthField;
    [SceneObjectsOnly] [SerializeField] private TMP_InputField heightField;
    [SceneObjectsOnly] [SerializeField] private Button startGameButton;
    [SceneObjectsOnly] [SerializeField] private Button exitButton;
    [SceneObjectsOnly] [SerializeField] private TMP_Text numberOfFlagsLabel;
    [SceneObjectsOnly] [SerializeField] private TMP_Text numberOfMinesLabel;

    [Space] [SceneObjectsOnly] [SerializeField]
    private Transform gridParent;

    [SerializeField] private float gridHeight;

    [Space] [AssetsOnly] [SerializeField] private GameObject cellPrefab;
    private CellController[,] _controllers;
    private CellController _heldCell;

    private bool _holdingOnCell;
    private Camera _mainCamera;

    private Lib.Cell[,] _map;

    private Vector2Int _mapDimensions = DefaultGridSize;
    private int _numberOfFlags;
    private int _numberOfMines = DefaultNumberOfMines;

    private bool _playing;
    private int _seed;

    private void Awake()
    {
        _mainCamera = Camera.main;
        numberOfMinesField.text = _numberOfMines.ToString();
        widthField.text = _mapDimensions.x.ToString();
        heightField.text = _mapDimensions.y.ToString();

        numberOfMinesField.onEndEdit.AddListener(val =>
        {
            if (!int.TryParse(val, out _numberOfMines))
                _numberOfMines = DefaultNumberOfMines;

            var temp = Math.Clamp(_numberOfMines, 1, _mapDimensions.x * _mapDimensions.y - 1);
            if (temp == _numberOfMines) return;

            _numberOfMines = temp;
            numberOfMinesField.text = temp.ToString();
        });
        widthField.onEndEdit.AddListener(val =>
        {
            if (!int.TryParse(val, out var w))
                w = DefaultGridSize.x;

            var temp = Math.Clamp(w, 3, int.MaxValue);
            if (temp != w)
            {
                w = temp;
                widthField.text = temp.ToString();
            }

            _mapDimensions.x = w;
        });
        heightField.onEndEdit.AddListener(val =>
        {
            if (!int.TryParse(val, out var h))
                h = DefaultGridSize.y;

            var temp = Math.Clamp(h, 3, int.MaxValue);
            if (temp != h)
            {
                h = temp;
                heightField.text = temp.ToString();
            }

            _mapDimensions.y = h;
        });

        startGameButton.onClick.AddListener(StartGame);
        exitButton.onClick.AddListener(Application.Quit);
    }

    private void Update()
    {
        if (!_playing) _holdingOnCell = false;

        CellController GetClickedCell()
        {
            var ray = _mainCamera.ScreenPointToRay(Input.mousePosition);

            return Physics.Raycast(ray, out var info)
                ? info.transform.GetComponent<CellController>()
                : null;
        }

        // This checks for a full click and release on the same cell
        if (Input.GetMouseButtonDown(0))
        {
            var cell = GetClickedCell();
            if (cell != null)
            {
                _holdingOnCell = true;
                _heldCell = cell;
            }
        }
        else if (Input.GetMouseButtonUp(0) && _holdingOnCell)
        {
            var cell = GetClickedCell();
            if (cell == _heldCell)
                cell.OnClick(0);

            _holdingOnCell = false;
        }

        if (Input.GetMouseButtonDown(1))
        {
            var cell = GetClickedCell();
            if (cell != null)
                cell.OnClick(1);
        }
    }

    private void StartGame()
    {
        uiScreenParent.SetActive(false);
        gridParent.gameObject.SetActive(true);
        _playing = true;
        _numberOfFlags = 0;

        numberOfMinesLabel.text = _numberOfMines.ToString();
        numberOfFlagsLabel.text = "0";

        _map = Game.startGame(_mapDimensions.y, _mapDimensions.x, _numberOfMines);

        Cleanup();
        CreateField();
        DrawField();
    }

    private void Cleanup()
    {
        if (_controllers != null)
            for (var i = 0; i < _controllers.GetLength(0); i++)
            for (var j = 0; j < _controllers.GetLength(1); j++)
                Destroy(_controllers[i, j].gameObject);

        _controllers = new CellController[_mapDimensions.x, _mapDimensions.y];
    }

    private void CreateField()
    {
        var cellSide = Mathf.Min(1, gridHeight / _mapDimensions.y);
        var totalWidth = cellSide * _mapDimensions.x;
        var totalHeight = cellSide * _mapDimensions.y;

        for (var x = 0; x < _mapDimensions.x; x++)
        for (var y = 0; y < _mapDimensions.y; y++)
        {
            var cell = Instantiate(cellPrefab, gridParent).GetComponent<CellController>();
            var cellTransform = cell.transform;
            cellTransform.localPosition = new Vector3(x * cellSide + cellSide / 2 - totalWidth / 2,
                y * cellSide + cellSide / 2 - totalHeight / 2);
            cellTransform.localScale = new Vector3(cellSide, cellSide);
            cell.Initialize(OnClick,
                () =>
                {
                    _numberOfFlags++;
                    UpdateFlagsText();
                },
                () =>
                {
                    _numberOfFlags--;
                    UpdateFlagsText();
                },
                new Vector2Int(x, y));

            _controllers[x, y] = cell;
        }
    }

    private void OnClick(Vector2Int cellCoords)
    {
        if (!_playing)
            return;

        var mapAfterClick = GameFunctions.leftClick(_map, cellCoords.y, cellCoords.x);

        if (mapAfterClick == null)
        {
            StartCoroutine(EndGame(false, cellCoords));
            return;
        }

        _map = mapAfterClick;

        if (CheckWin())
        {
            StartCoroutine(EndGame(true, cellCoords));
            return;
        }

        DrawField();
    }

    private void UpdateFlagsText()
    {
        numberOfFlagsLabel.text = _numberOfFlags.ToString();
    }

    private bool CheckWin()
    {
        for (var x = 0; x < _mapDimensions.x; x++)
        for (var y = 0; y < _mapDimensions.y; y++)
            if (!_map[y, x].discovered && !_map[y, x].hasMine)
                return false;

        return true;
    }

    private void DrawField()
    {
        for (var x = 0; x < _mapDimensions.x; x++)
        for (var y = 0; y < _mapDimensions.y; y++)
            _controllers[x, y].SetState(_map[y, x]);
    }

    private void ShowBombs(Vector2Int blownUp)
    {
        for (var x = 0; x < _mapDimensions.x; x++)
        for (var y = 0; y < _mapDimensions.y; y++)
            _controllers[x, y].ShowBomb(blownUp);
    }

    private IEnumerator EndGame(bool victory, Vector2Int endCell)
    {
        _playing = false;

        if (!victory)
        {
            ShowBombs(endCell);
            yield return new WaitForSeconds(DefeatWatchBoardTime);
        }

        gameResultLabel.text = victory ? "You Win!" : "You Lose :(";
        gridParent.gameObject.SetActive(false);
        uiScreenParent.SetActive(true);
    }
}