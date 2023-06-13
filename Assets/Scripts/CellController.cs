using System;
using FSharpLib;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

public class CellController : MonoBehaviour
{
    [SerializeField] private SpriteRenderer cellBackground;
    [SerializeField] private SpriteRenderer cellIcon;
    [SerializeField] private TMP_Text numberText;

    [Title("Sprites")] [SerializeField] private Sprite undiscoveredBackground;
    [SerializeField] private Sprite discoveredBackground;
    [SerializeField] private Sprite flagIcon;
    [SerializeField] private Sprite bombIcon;
    [SerializeField] private Color[] bombCountcolors;

    private Vector2Int _coords;
    private Lib.Cell _currentState;

    private bool _flagged;

    private Action<Vector2Int> _onClickAction;


    public void OnMouseUpAsButton()
    {
        if (Input.GetMouseButtonUp(0) && !_currentState.discovered && !_flagged)
        {
            _onClickAction?.Invoke(_coords);
        }
        else if (Input.GetMouseButtonUp(1) && !_currentState.discovered)
        {
            _flagged = !_flagged;
            DrawCell();
        }
    }

    public void Initialize(Action<Vector2Int> onClick, Vector2Int coords)
    {
        _coords = coords;
        _onClickAction = onClick;
    }

    public void SetState(Lib.Cell newCell)
    {
        _currentState = newCell;
        if (_currentState.discovered)
            _flagged = false;

        DrawCell();
    }

    public void ShowBomb(Vector2Int blownUpCoords)
    {
        if (!_currentState.hasMine)
            return;
        if (blownUpCoords.x == _coords.x && blownUpCoords.y == _coords.y)
            cellBackground.color = Color.red;

        cellBackground.sprite = undiscoveredBackground;
        cellIcon.sprite = bombIcon;
    }

    private void DrawCell()
    {
        var background = _currentState.discovered ? discoveredBackground : undiscoveredBackground;
        var icon = _flagged ? flagIcon : null;

        cellBackground.sprite = background;
        cellBackground.color = Color.white;
        cellIcon.sprite = icon;

        if (!_currentState.discovered)
        {
            numberText.text = "";
            return;
        }

        var mines = _currentState.adjacentMines;

        switch (mines)
        {
            case 0:
                numberText.text = "";
                break;
            default:
                numberText.text = mines.ToString();
                numberText.color = bombCountcolors[mines - 1];
                break;
        }
    }
}