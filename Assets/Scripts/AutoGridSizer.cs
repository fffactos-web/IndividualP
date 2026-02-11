using UnityEngine;
using UnityEngine.UI;

// Упрощённый набор using'ов, главное — UnityEngine, UnityEngine.UI.

[RequireComponent(typeof(GridLayoutGroup))]
public class AdaptiveGridFitter : MonoBehaviour
{
    [Header("Grid")]
    public GridLayoutGroup grid; // если пусто, будет найден автоматически

    [Header("Cell size limits (pixels)")]
    public float minCellSize = 48f;   // минимальный желаемый размер
    public float maxCellSize = 128f;  // максимальный желаемый размер

    [Header("Behavior")]
    [Tooltip("Если true — ячейки будут квадратные (cell.x = cell.y).")]
    public bool squareCells = true;

    [Tooltip("Максимальное число столбцов, которое разрешаем (0 = без ограничения).")]
    public int maxColumnsLimit = 0;

    [Tooltip("Если >0 - используем этот count для расчётов; иначе используем grid.transform.childCount.")]
    public int itemCountOverride = 0;

    // internal
    RectTransform rect;
    int lastChildCount = -1;
    Vector2 lastRectSize = Vector2.zero;

    public void RebuildCells()
    {
        // дешевый чек на изменения размера / кол-ва детей — чтобы не выполнять лишних пересчётов
        int childCount = itemCountOverride > 0 ? itemCountOverride : grid.transform.childCount;
        Vector2 currentSize = rect != null ? rect.rect.size : Vector2.zero;

        if (childCount != lastChildCount || currentSize != lastRectSize)
        {
            ForceRebuild();
            lastChildCount = childCount;
            lastRectSize = currentSize;
        }
    }
    void Reset()
    {
        grid = GetComponent<GridLayoutGroup>();
    }

    void OnEnable()
    {
        if (grid == null) grid = GetComponent<GridLayoutGroup>();
        rect = grid.GetComponent<RectTransform>();
        ForceRebuild();
    }

    void OnValidate()
    {
        if (grid == null) grid = GetComponent<GridLayoutGroup>();
        if (rect == null && grid != null) rect = grid.GetComponent<RectTransform>();
        ForceRebuild();
    }


    void ForceRebuild()
    {
        if (grid == null || rect == null) return;

        int itemCount = itemCountOverride > 0 ? itemCountOverride : grid.transform.childCount;
        if (itemCount <= 0)
        {
            // если нет элементов — просто установим максимально допустимый размер
            ApplyCellSize(maxCellSize);
            return;
        }

        float width = rect.rect.width - grid.padding.left - grid.padding.right;
        float height = rect.rect.height - grid.padding.top - grid.padding.bottom;

        float spacingX = grid.spacing.x;
        float spacingY = grid.spacing.y;

        // Защитный минимум ширины
        width = Mathf.Max(1f, width);
        height = Mathf.Max(1f, height);

        // Вычислим максимально возмож колонок при минимальном cellSize
        int theoreticalMaxCols = Mathf.FloorToInt((width + spacingX) / (minCellSize + spacingX));
        theoreticalMaxCols = Mathf.Max(1, theoreticalMaxCols);

        if (maxColumnsLimit > 0)
            theoreticalMaxCols = Mathf.Min(theoreticalMaxCols, maxColumnsLimit);

        // будем перебирать все возможные варианты кол-во колонок от 1 до theoreticalMaxCols,
        // и выбирать тот, у которого cellSize (после подгонки по ширине) максимален, и при этом весь контент помещается по высоте.
        float bestCell = -1f;
        int bestCols = 1;

        for (int cols = 1; cols <= theoreticalMaxCols; cols++)
        {
            float totalSpacingX = spacingX * (cols - 1);
            float candidateCellX = (width - totalSpacingX) / cols;
            float candidateCell = candidateCellX;

            if (squareCells)
            {
                // candidateCell must also fit by height -> later we check with rows
            }

            // clamp horizontally-based candidate to maxCellSize (we prefer larger visual sizes but not exceed max)
            candidateCell = Mathf.Min(candidateCell, maxCellSize);

            // кол-во строк для данного cols
            int rows = Mathf.CeilToInt(itemCount / (float)cols);
            float totalSpacingY = spacingY * (rows - 1);

            // если square: высота = rows*candidateCell + spacings
            float totalHeightForCandidate = rows * candidateCell + totalSpacingY;

            // если не вмещается по высоте, попробуем уменьшить cellSize до того, что влезет по высоте
            if (totalHeightForCandidate > height)
            {
                // доступный cellSize по высоте:
                float availCellFromHeight = (height - totalSpacingY) / rows;
                // если squareCells — cell должен быть <= availCellFromHeight
                // если не квадрат — можно держать candidateCellX (ширина) и уменьшить высоту отдельно, но GridLayoutGroup использует единый cellSize.
                candidateCell = Mathf.Min(candidateCell, availCellFromHeight);
                totalHeightForCandidate = rows * candidateCell + totalSpacingY;
            }

            // теперь candidateCell — реальный cellSize при данном количестве колонок
            // требование: candidateCell не меньше какой-то разумной величины (мы позволяем быть меньше minCellSize только если иначе ничего не поместится).
            // но при выборе лучшего варианта — мы хотим максимизировать candidateCell, чтобы не оставаться всегда на minCellSize.
            // также candidateCell может быть очень маленьким -> отклоним варианты совсем крошечные.
            if (candidateCell <= 0f) continue;

            // Помещается ли итогово по высоте? Если да — кандидат валиден
            bool fitsVertically = totalHeightForCandidate <= height + 0.01f;

            // Если помещается — сохраняем, если cell больше текущего лучшего
            if (fitsVertically)
            {
                if (candidateCell > bestCell)
                {
                    bestCell = candidateCell;
                    bestCols = cols;
                }
            }
            else
            {
                // Если ни один вариант не вмещается (bestCell останется -1), мы позже выберем вариант с maximum cols и тогда снизим cellSize ниже min
                // но стоит запомнить наилучший (наибольший) candidateCell даже если не вмещается, чтобы потом минимально корректировать
                if (bestCell < candidateCell)
                {
                    bestCell = candidateCell;
                    bestCols = cols;
                }
            }
        }

        // Если bestCell всё ещё -1 (маловероятно), поставим fallback
        if (bestCell <= 0f)
        {
            bestCols = Mathf.Clamp(theoreticalMaxCols, 1, theoreticalMaxCols);
            float fallbackCell = (width - grid.spacing.x * (bestCols - 1)) / bestCols;
            bestCell = Mathf.Max(4f, fallbackCell); // минимально защитное значение
        }

        // Если лучшая конфигурация даёт cell < minCellSize и при этом иначе не влезало, можно:
        // - либо принудительно выставить minCellSize и позволить скролл/переполнение,
        // - либо уменьшить cell до того, чтобы точно всё уместилось (мы уже пытались это при подборе).
        // Поведение: если bestCell < minCellSize, но при этом конфигурация помещается по высоте, оставим minCellSize;
        // если помещаться не могло — используем bestCell (меньше min), чтобы уместить всё.
        int itemRowsForBest = Mathf.CeilToInt(itemCount / (float)bestCols);
        float totalSpacingYForBest = grid.spacing.y * (itemRowsForBest - 1);
        float heightNeededAtMin = itemRowsForBest * minCellSize + totalSpacingYForBest;

        float finalCellSize = bestCell;
        // Если best позволяет влезать и >= min -> ok
        if (bestCell >= minCellSize) finalCellSize = Mathf.Min(bestCell, maxCellSize);
        else
        {
            // bestCell < minCellSize
            if (heightNeededAtMin <= (rect.rect.height - grid.padding.top - grid.padding.bottom))
            {
                // при minCellSize всё влезет — ставим min, чтобы не быть крошечным
                finalCellSize = minCellSize;
            }
            else
            {
                // при minCellSize НЕ влезает — используем bestCell (меньше min), чтобы уместить
                finalCellSize = bestCell;
            }
        }

        // Накладка: собираемся применить finalCellSize (square или не square)
        ApplyCellSize(finalCellSize);

        // Принудительный rebuild layout, чтобы UI сразу обновился
        LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
    }

    void ApplyCellSize(float size)
    {
        if (grid == null) return;

        if (squareCells)
        {
            grid.cellSize = new Vector2(size, size);
        }
        else
        {
            // Если не квадрат, сохраняем ширину, а высоту подбираем по той же логике (можно улучшить)
            grid.cellSize = new Vector2(size, Mathf.Clamp(grid.cellSize.y, minCellSize, maxCellSize));
        }

        // выставим constraintCount в соответствии с текущ заданным cellSize (чтобы порядок ячеек был правильным)
        // пересчитаем число колонок из текущ размера: (ширина + spacing) / (cell + spacing)
        float width = rect.rect.width - grid.padding.left - grid.padding.right;
        int cols = Mathf.Max(1, Mathf.FloorToInt((width + grid.spacing.x) / (grid.cellSize.x + grid.spacing.x)));
        if (maxColumnsLimit > 0) cols = Mathf.Min(cols, maxColumnsLimit);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = cols;
    }
}
