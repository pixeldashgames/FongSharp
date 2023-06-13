using System;
using System.Collections;
using FSharpLib;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameController : MonoBehaviour
{
    private const float DefeatWatchBoardTime = 2;
    private const int DefaultNumberOfMines = 6;
    private static readonly Vector2Int DefaultGridSize = new(6, 6);

    [Title("UI")] [SceneObjectsOnly] [SerializeField]
    private TMP_InputField numberOfMinesField;

    [SceneObjectsOnly] [SerializeField] private GameObject uiScreenParent;
    [SceneObjectsOnly] [SerializeField] private TMP_Text gameResultLabel;
    [SceneObjectsOnly] [SerializeField] private TMP_InputField widthField;
    [SceneObjectsOnly] [SerializeField] private TMP_InputField heightField;
    [SceneObjectsOnly] [SerializeField] private Button startGameButton;

    [Space] [SceneObjectsOnly] [SerializeField]
    private Transform gridParent;

    [SerializeField] private float gridHeight;

    [Space] [AssetsOnly] [SerializeField] private GameObject cellPrefab;
    private CellController[,] _controllers;

    private Lib.Cell[,] _map;

    private Vector2Int _mapDimensions = DefaultGridSize;
    private int _numberOfMines = 6;
    private int _seed;

    private void Awake()
    {
        numberOfMinesField.text = _numberOfMines.ToString();
        widthField.text = _mapDimensions.x.ToString();
        heightField.text = _mapDimensions.y.ToString();
    }

    private void Start()
    {
        numberOfMinesField.onValueChanged.AddListener(val =>
        {
            if (!int.TryParse(val, out _numberOfMines))
                _numberOfMines = DefaultNumberOfMines;

            var temp = Math.Clamp(_numberOfMines, 1, _mapDimensions.x * _mapDimensions.y - 1);
            if (temp == _numberOfMines) return;

            _numberOfMines = temp;
            numberOfMinesField.text = temp.ToString();
        });
        widthField.onValueChanged.AddListener(val =>
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
        heightField.onValueChanged.AddListener(val =>
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
    }


    private void StartGame()
    {
        uiScreenParent.SetActive(false);

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
            cellTransform.localPosition = new Vector3(x * cellSide - totalWidth / 2, y * cellSide - totalHeight / 2);
            cellTransform.localScale = new Vector3(cellSide, cellSide);
            cell.Initialize(OnClick, new Vector2Int(x, y));

            _controllers[x, y] = cell;
        }
    }

    private void OnClick(Vector2Int cellCoords)
    {
        var mapAfterClick = GameFunctions.leftClick(_map, cellCoords.y, cellCoords.x);

        if (mapAfterClick == null)
        {
            StartCoroutine(EndGame(false, cellCoords));
            return;
        }

        _map = mapAfterClick;

        if (!CheckWin())
        {
            StartCoroutine(EndGame(true, cellCoords));
            return;
        }

        DrawField();
    }

    private bool CheckWin()
    {
        for (var x = 0; x < _mapDimensions.x; x++)
        for (var y = 0; y < _mapDimensions.y; y++)
            if (!_map[y, x].discovered && _map[y, x].hasMine)
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
        if (!victory)
        {
            ShowBombs(endCell);
            yield return new WaitForSeconds(DefeatWatchBoardTime);
        }

        gameResultLabel.text = victory ? "You Win!" : "You Lose :(";
        uiScreenParent.SetActive(true);
    }
}