using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Board size picker: an N×N grid of cells where the player presses on one cell and drags to
/// another, selecting the rectangle between them. Releasing a valid selection raises
/// <see cref="SelectionConfirmed"/>; an invalid one stays highlighted with the reason shown.
/// Put this on a square-ish RectTransform with a raycast-target Graphic so it receives pointer events.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public sealed class GridSizePicker : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [SerializeField] private Sprite cellSprite;
    [SerializeField] private TMP_Text messageText;
    [Tooltip("Gap between cells as a fraction of the cell size.")]
    [Range(0f, 0.5f)] [SerializeField] private float cellGap = 0.12f;
    [SerializeField] private string hint = "Drag across the grid to choose your board";

    private Color validColor = new Color32(0x12, 0x85, 0x80, 0xFF);
    private Color invalidColor = new Color(0.75f, 0.27f, 0.27f, 1f);
    private Color idleColor = new Color(1f, 1f, 1f, 0.16f);

    private RectTransform rectTransform;
    private Image[] cells = Array.Empty<Image>();
    private int size;
    private int designCount;
    private bool dragging;
    private int pointerId;
    private Vector2Int startCell;
    private Vector2Int currentCell;
    private bool showingSelection;

    /// <summary>Raised with (columns, rows) when the player releases a valid selection.</summary>
    public event Action<int, int> SelectionConfirmed;

    public int Size => size;

    private void Awake()
    {
        rectTransform = (RectTransform)transform;
    }

    private void OnDisable()
    {
        ResetSelection();
    }

    private void OnRectTransformDimensionsChange()
    {
        if(cells.Length > 0) LayoutCells();
    }

    /// <summary>Creates a <paramref name="gridSize"/>×<paramref name="gridSize"/> grid for boards using up to <paramref name="designs"/> pairs.</summary>
    public void Build(int gridSize, int designs)
    {
        if(rectTransform == null) rectTransform = (RectTransform)transform;
        foreach(Image cell in cells) if(cell != null) Destroy(cell.gameObject);

        size = Mathf.Max(0, gridSize);
        designCount = designs;
        cells = new Image[size * size];

        for(int i = 0; i < cells.Length; i++)
        {
            GameObject go = new GameObject($"Cell {i % Mathf.Max(1, size)},{i / Mathf.Max(1, size)}", typeof(RectTransform));
            go.layer = gameObject.layer;
            go.transform.SetParent(transform, false);
            Image image = go.AddComponent<Image>();
            image.sprite = cellSprite;
            image.type = cellSprite != null && cellSprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
            image.raycastTarget = false;
            cells[i] = image;
        }

        LayoutCells();
        ResetSelection();
    }

    public void SetColors(Color valid, Color invalid, Color idle)
    {
        validColor = valid;
        invalidColor = invalid;
        idleColor = idle;
        RefreshCells();
    }

    public void ResetSelection()
    {
        dragging = false;
        showingSelection = false;
        RefreshCells();
        SetMessage(hint, false);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if(dragging || size == 0) return;
        if(!TryGetCell(eventData, false, out Vector2Int cell)) return;

        dragging = true;
        pointerId = eventData.pointerId;
        startCell = cell;
        currentCell = cell;
        showingSelection = true;
        RefreshSelection();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if(!dragging || eventData.pointerId != pointerId) return;
        if(!TryGetCell(eventData, true, out Vector2Int cell) || cell == currentCell) return;
        currentCell = cell;
        RefreshSelection();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if(!dragging || eventData.pointerId != pointerId) return;
        dragging = false;
        if(TryGetCell(eventData, true, out Vector2Int cell)) currentCell = cell;

        GetSelection(out int columns, out int rows);
        if(BoardSelection.Validate(columns, rows, designCount) == BoardSelectionResult.Valid)
        {
            showingSelection = false;
            RefreshCells();
            SetMessage(hint, false);
            SelectionConfirmed?.Invoke(columns, rows);
            return;
        }

        // Keep the rejected selection visible with its reason until the next press.
        RefreshSelection();
    }

    private void RefreshSelection()
    {
        GetSelection(out int columns, out int rows);
        BoardSelectionResult result = BoardSelection.Validate(columns, rows, designCount);
        bool valid = result == BoardSelectionResult.Valid;
        RefreshCells();
        SetMessage(valid ? $"{columns} × {rows}  ·  {columns * rows / 2} pairs" : BoardSelection.Describe(result), !valid);
    }

    private void RefreshCells()
    {
        GetSelection(out int columns, out int rows);
        bool valid = BoardSelection.Validate(columns, rows, designCount) == BoardSelectionResult.Valid;
        Color selected = valid ? validColor : invalidColor;
        int minX = Mathf.Min(startCell.x, currentCell.x), maxX = Mathf.Max(startCell.x, currentCell.x);
        int minY = Mathf.Min(startCell.y, currentCell.y), maxY = Mathf.Max(startCell.y, currentCell.y);

        for(int i = 0; i < cells.Length; i++)
        {
            if(cells[i] == null) continue;
            int x = i % size, y = i / size;
            bool inside = showingSelection && x >= minX && x <= maxX && y >= minY && y <= maxY;
            cells[i].color = inside ? selected : idleColor;
        }
    }

    private void GetSelection(out int columns, out int rows)
    {
        columns = Mathf.Abs(currentCell.x - startCell.x) + 1;
        rows = Mathf.Abs(currentCell.y - startCell.y) + 1;
    }

    private void SetMessage(string text, bool isError)
    {
        if(messageText == null) return;
        messageText.text = text;
        messageText.color = isError ? invalidColor : Color.white;
    }

    /// <summary>Square grid area centred in this rect, in local coordinates.</summary>
    private Rect GridRect()
    {
        Rect rect = rectTransform.rect;
        float side = Mathf.Min(rect.width, rect.height);
        return new Rect(rect.center.x - side * 0.5f, rect.center.y - side * 0.5f, side, side);
    }

    private void LayoutCells()
    {
        if(size == 0) return;
        Rect grid = GridRect();
        float step = grid.width / size;
        float gap = step * cellGap;
        float cellSize = step - gap;

        for(int i = 0; i < cells.Length; i++)
        {
            if(cells[i] == null) continue;
            int x = i % size, y = i / size;
            RectTransform cell = cells[i].rectTransform;
            cell.anchorMin = cell.anchorMax = new Vector2(0.5f, 0.5f);
            cell.pivot = new Vector2(0.5f, 0.5f);
            cell.sizeDelta = new Vector2(cellSize, cellSize);
            // Row 0 is the top row.
            cell.anchoredPosition = new Vector2(
                grid.xMin + step * (x + 0.5f) - rectTransform.rect.center.x,
                grid.yMax - step * (y + 0.5f) - rectTransform.rect.center.y);
        }
    }

    private bool TryGetCell(PointerEventData eventData, bool clamp, out Vector2Int cell)
    {
        cell = default;
        Camera eventCamera = eventData.pressEventCamera != null ? eventData.pressEventCamera : eventData.enterEventCamera;
        if(!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, eventData.position, eventCamera, out Vector2 local)) return false;

        Rect grid = GridRect();
        float step = grid.width / size;
        int x = Mathf.FloorToInt((local.x - grid.xMin) / step);
        int y = Mathf.FloorToInt((grid.yMax - local.y) / step);

        if(!clamp && (x < 0 || y < 0 || x >= size || y >= size)) return false;
        cell = new Vector2Int(Mathf.Clamp(x, 0, size - 1), Mathf.Clamp(y, 0, size - 1));
        return true;
    }
}
