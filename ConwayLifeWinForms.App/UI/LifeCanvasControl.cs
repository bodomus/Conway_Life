using ConwayLifeWinForms.App.Core.Abstractions;

namespace ConwayLifeWinForms.App.UI;

/// <summary>
/// Визуальный контрол для отображения и редактирования поля «Жизнь».
/// Отвечает за отрисовку, масштабирование, панорамирование и редактирование клеток мышью.
/// </summary>
/// <param name="engine">Движок игры, предоставляющий состояние сетки и операции над ней.</param>
public sealed class LifeCanvasControl(ILifeEngine engine) : Control
{
    /// <summary>
    /// Минимально допустимый размер клетки в пикселях.
    /// </summary>
    public const int MinCellSize = 2;

    /// <summary>
    /// Максимально допустимый размер клетки в пикселях.
    /// </summary>
    public const int MaxCellSize = 40;

    /// <summary>
    /// Размер клетки по умолчанию в пикселях.
    /// </summary>
    public const int DefaultCellSize = 10;

    /// <summary>
    /// Экземпляр движка игры, с которым работает контрол.
    /// </summary>
    private readonly ILifeEngine _engine = engine;

    /// <summary>
    /// Кисть для заливки живых клеток.
    /// </summary>
    private readonly SolidBrush _aliveBrush = new(Color.FromArgb(34, 139, 34));

    /// <summary>
    /// Кисть для заливки фона (мертвых клеток).
    /// </summary>
    private readonly SolidBrush _deadBrush = new(Color.FromArgb(248, 250, 252));

    /// <summary>
    /// Перо для рисования линий сетки.
    /// </summary>
    private readonly Pen _gridPen = new(Color.FromArgb(220, 225, 230));

    /// <summary>
    /// Горизонтальное смещение viewport в пикселях относительно мировых координат.
    /// </summary>
    private double _viewOffsetX;

    /// <summary>
    /// Вертикальное смещение viewport в пикселях относительно мировых координат.
    /// </summary>
    private double _viewOffsetY;

    /// <summary>
    /// Флаг удержания клавиши Space для режима панорамирования через ЛКМ.
    /// </summary>
    private bool _spacePressed;

    /// <summary>
    /// Флаг активного режима панорамирования.
    /// </summary>
    private bool _isPanning;

    /// <summary>
    /// Стартовая точка мыши при начале панорамирования.
    /// </summary>
    private Point _panStartMouse;

    /// <summary>
    /// Исходное горизонтальное смещение в момент старта панорамирования.
    /// </summary>
    private double _panStartOffsetX;

    /// <summary>
    /// Исходное вертикальное смещение в момент старта панорамирования.
    /// </summary>
    private double _panStartOffsetY;

    /// <summary>
    /// Флаг активного режима «рисования» по сетке при зажатой кнопке мыши.
    /// </summary>
    private bool _isDraggingEdit;

    /// <summary>
    /// Режим редактирования в drag-операции: true — ставить живые, false — очищать клетки.
    /// </summary>
    private bool _dragSetAlive;

    /// <summary>
    /// Последняя отредактированная координата X во время drag-редактирования.
    /// </summary>
    private int _lastEditedX = -1;

    /// <summary>
    /// Последняя отредактированная координата Y во время drag-редактирования.
    /// </summary>
    private int _lastEditedY = -1;

    /// <summary>
    /// Текущий размер клетки в пикселях.
    /// </summary>
    public int CellSize { get; private set; } = DefaultCellSize;

    /// <summary>
    /// Событие: сетка была изменена пользователем.
    /// </summary>
    public event EventHandler? GridEdited;

    /// <summary>
    /// Событие: масштаб был изменен.
    /// </summary>
    public event EventHandler? ZoomChanged;

    /// <summary>
    /// Событие: viewport был сдвинут/изменен.
    /// </summary>
    public event EventHandler? ViewChanged;

    /// <summary>
    /// Устанавливает текущее состояние клавиши Space для управления панорамированием.
    /// </summary>
    /// <param name="pressed">true, если Space зажата; иначе false.</param>
    /// <remarks>Возвращаемое значение: отсутствует.</remarks>
    public void SetSpacePressed(bool pressed) => _spacePressed = pressed;

    /// <summary>
    /// Принудительно завершает панорамирование и редактирование перетаскиванием.
    /// </summary>
    /// <remarks>Возвращаемое значение: отсутствует.</remarks>
    public void CancelPan()
    {
        _isPanning = false;
        _isDraggingEdit = false;
        Cursor = Cursors.Default;
    }

    /// <summary>
    /// Изменяет масштаб на относительный шаг.
    /// </summary>
    /// <param name="delta">Смещение масштаба в пикселях размера клетки.</param>
    /// <param name="anchorScreen">Опорная экранная точка масштабирования; если null, используется центр контрола.</param>
    /// <remarks>Возвращаемое значение: отсутствует.</remarks>
    public void ChangeZoom(int delta, Point? anchorScreen = null)
    {
        // Целевой размер клетки после применения шага, ограниченный допустимым диапазоном.
        int next = Math.Clamp(CellSize + delta, MinCellSize, MaxCellSize);
        SetZoom(next, anchorScreen);
    }

    /// <summary>
    /// Устанавливает абсолютный масштаб и сохраняет визуальную привязку к опорной точке.
    /// </summary>
    /// <param name="zoom">Новый размер клетки в пикселях.</param>
    /// <param name="anchorScreen">Опорная экранная точка; если null, используется центр контрола.</param>
    /// <remarks>Возвращаемое значение: отсутствует.</remarks>
    public void SetZoom(int zoom, Point? anchorScreen = null)
    {
        // Масштаб, ограниченный техническими границами контрола.
        int clamped = Math.Clamp(zoom, MinCellSize, MaxCellSize);
        if (clamped == CellSize)
        {
            return;
        }

        // Экранная точка-якорь, относительно которой выполняется zoom.
        Point anchor = anchorScreen ?? new Point(ClientSize.Width / 2, ClientSize.Height / 2);

        // Координата мира по X под якорем до изменения масштаба.
        double worldX = (anchor.X + _viewOffsetX) / CellSize;

        // Координата мира по Y под якорем до изменения масштаба.
        double worldY = (anchor.Y + _viewOffsetY) / CellSize;

        CellSize = clamped;

        // Новый offset по X так, чтобы worldX остался под той же экранной точкой.
        _viewOffsetX = (worldX * CellSize) - anchor.X;

        // Новый offset по Y так, чтобы worldY остался под той же экранной точкой.
        _viewOffsetY = (worldY * CellSize) - anchor.Y;

        ClampOffsets();
        ZoomChanged?.Invoke(this, EventArgs.Empty);
        Invalidate();
    }

    /// <summary>
    /// Сбрасывает масштаб к значению по умолчанию.
    /// </summary>
    /// <remarks>Возвращаемое значение: отсутствует.</remarks>
    public void ResetZoom() => SetZoom(DefaultCellSize);

    /// <summary>
    /// Сдвигает viewport на указанное количество клеток.
    /// </summary>
    /// <param name="dxCells">Смещение по X в клетках.</param>
    /// <param name="dyCells">Смещение по Y в клетках.</param>
    /// <remarks>Возвращаемое значение: отсутствует.</remarks>
    public void NudgeView(int dxCells, int dyCells)
    {
        _viewOffsetX += dxCells * CellSize;
        _viewOffsetY += dyCells * CellSize;
        ClampOffsets();
        ViewChanged?.Invoke(this, EventArgs.Empty);
        Invalidate();
    }

    /// <summary>
    /// Возвращает центр видимой области в мировых координатах (клетки, дробные значения допустимы).
    /// </summary>
    /// <returns>Кортеж (X, Y) центра viewport в координатах сетки.</returns>
    public (double X, double Y) GetViewportCenterWorld()
    {
        // Центр viewport по X, пересчитанный в мировые координаты.
        double centerX = (_viewOffsetX + (ClientSize.Width / 2d)) / CellSize;

        // Центр viewport по Y, пересчитанный в мировые координаты.
        double centerY = (_viewOffsetY + (ClientSize.Height / 2d)) / CellSize;

        return (centerX, centerY);
    }

    /// <summary>
    /// Возвращает ближайшую целочисленную клетку, соответствующую центру viewport.
    /// </summary>
    /// <returns>Координаты клетки в центре видимой области.</returns>
    public Point GetViewportCenterCell()
    {
        // Центр viewport в мировых координатах с плавающей точкой.
        (double x, double y) = GetViewportCenterWorld();

        // Округление к ближайшей клетке.
        return new Point((int)Math.Round(x), (int)Math.Round(y));
    }

    /// <summary>
    /// Сохраняет положение viewport после изменения размера мира.
    /// </summary>
    /// <param name="newWidth">Новая ширина мира в клетках.</param>
    /// <param name="newHeight">Новая высота мира в клетках.</param>
    /// <param name="oldCenter">Центр viewport до изменения размера мира.</param>
    /// <remarks>Возвращаемое значение: отсутствует.</remarks>
    public void PreserveViewportAfterResize(int newWidth, int newHeight, (double X, double Y) oldCenter)
    {
        // Центр по X, ограниченный пределами нового мира.
        double centerX = Math.Clamp(oldCenter.X, 0d, Math.Max(0d, newWidth - 1d));

        // Центр по Y, ограниченный пределами нового мира.
        double centerY = Math.Clamp(oldCenter.Y, 0d, Math.Max(0d, newHeight - 1d));

        _viewOffsetX = (centerX * CellSize) - (ClientSize.Width / 2d);
        _viewOffsetY = (centerY * CellSize) - (ClientSize.Height / 2d);

        ClampOffsets();
        ViewChanged?.Invoke(this, EventArgs.Empty);
        Invalidate();
    }

    /// <summary>
    /// Отрисовывает видимую часть мира и сетку.
    /// </summary>
    /// <param name="e">Аргументы события отрисовки, содержащие Graphics.</param>
    /// <remarks>Возвращаемое значение: отсутствует.</remarks>
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        // Контекст графики текущего цикла отрисовки.
        Graphics g = e.Graphics;
        g.Clear(_deadBrush.Color);

        // Размер клетки в пикселях для текущего кадра.
        int cell = CellSize;

        // Ширина мира в клетках.
        int width = _engine.Width;

        // Высота мира в клетках.
        int height = _engine.Height;

        // Первая видимая колонка мира.
        int startX = Math.Max(0, (int)Math.Floor(_viewOffsetX / cell));

        // Первая видимая строка мира.
        int startY = Math.Max(0, (int)Math.Floor(_viewOffsetY / cell));

        // Последняя видимая колонка мира.
        int endX = Math.Min(width - 1, (int)Math.Ceiling((_viewOffsetX + ClientSize.Width) / cell));

        // Последняя видимая строка мира.
        int endY = Math.Min(height - 1, (int)Math.Ceiling((_viewOffsetY + ClientSize.Height) / cell));

        for (int y = startY; y <= endY; y++)
        {
            // Экранная координата Y для текущей строки клеток.
            int sy = (int)(y * cell - _viewOffsetY);
            for (int x = startX; x <= endX; x++)
            {
                if (_engine.GetCell(x, y))
                {
                    // Экранная координата X для текущей клетки.
                    int sx = (int)(x * cell - _viewOffsetX);
                    g.FillRectangle(_aliveBrush, sx, sy, cell, cell);
                }
            }
        }

        // Сетку рисуем только при достаточном размере клетки для читаемости.
        if (cell >= 6)
        {
            for (int x = startX; x <= endX + 1; x++)
            {
                // Экранная координата X для вертикальной линии сетки.
                int sx = (int)(x * cell - _viewOffsetX);
                g.DrawLine(_gridPen, sx, 0, sx, ClientSize.Height);
            }

            for (int y = startY; y <= endY + 1; y++)
            {
                // Экранная координата Y для горизонтальной линии сетки.
                int sy = (int)(y * cell - _viewOffsetY);
                g.DrawLine(_gridPen, 0, sy, ClientSize.Width, sy);
            }
        }
    }

    /// <summary>
    /// Обрабатывает колесо мыши: при Ctrl выполняет zoom относительно курсора.
    /// </summary>
    /// <param name="e">Аргументы события колесика мыши.</param>
    /// <remarks>Возвращаемое значение: отсутствует.</remarks>
    protected override void OnMouseWheel(MouseEventArgs e)
    {
        if ((ModifierKeys & Keys.Control) == Keys.Control)
        {
            // Шаг масштабирования: вверх +2, вниз -2.
            int step = e.Delta > 0 ? 2 : -2;
            ChangeZoom(step, e.Location);
            return;
        }

        base.OnMouseWheel(e);
    }

    /// <summary>
    /// Обрабатывает нажатие кнопки мыши: старт панорамирования или редактирования клеток.
    /// </summary>
    /// <param name="e">Аргументы события нажатия мыши.</param>
    /// <remarks>Возвращаемое значение: отсутствует.</remarks>
    protected override void OnMouseDown(MouseEventArgs e)
    {
        Focus();

        // Условие включения панорамирования: средняя кнопка или Space+ЛКМ.
        bool panGesture = e.Button == MouseButtons.Middle || (_spacePressed && e.Button == MouseButtons.Left);
        if (panGesture)
        {
            BeginPan(e.Location);
            return;
        }

        if (e.Button == MouseButtons.Left)
        {
            if (TryScreenToCell(e.Location, out int x, out int y))
            {
                // Текущее состояние клетки до переключения.
                bool current = _engine.GetCell(x, y);
                _engine.SetCell(x, y, !current);
                GridEdited?.Invoke(this, EventArgs.Empty);
                _isDraggingEdit = true;
                _dragSetAlive = true;
                _lastEditedX = x;
                _lastEditedY = y;
                Invalidate();
            }

            return;
        }

        if (e.Button == MouseButtons.Right)
        {
            _isDraggingEdit = true;
            _dragSetAlive = false;
            if (TryScreenToCell(e.Location, out int x, out int y))
            {
                _engine.SetCell(x, y, alive: false);
                _lastEditedX = x;
                _lastEditedY = y;
                GridEdited?.Invoke(this, EventArgs.Empty);
                Invalidate();
            }
        }
    }

    /// <summary>
    /// Обрабатывает перемещение мыши: панорамирует viewport или рисует клетки.
    /// </summary>
    /// <param name="e">Аргументы события перемещения мыши.</param>
    /// <remarks>Возвращаемое значение: отсутствует.</remarks>
    protected override void OnMouseMove(MouseEventArgs e)
    {
        if (_isPanning)
        {
            _viewOffsetX = _panStartOffsetX - (e.X - _panStartMouse.X);
            _viewOffsetY = _panStartOffsetY - (e.Y - _panStartMouse.Y);
            ClampOffsets();
            ViewChanged?.Invoke(this, EventArgs.Empty);
            Invalidate();
            return;
        }

        if (!_isDraggingEdit)
        {
            return;
        }

        if (!TryScreenToCell(e.Location, out int x, out int y))
        {
            return;
        }

        if (x == _lastEditedX && y == _lastEditedY)
        {
            return;
        }

        _lastEditedX = x;
        _lastEditedY = y;

        _engine.SetCell(x, y, _dragSetAlive);
        GridEdited?.Invoke(this, EventArgs.Empty);
        Invalidate();
    }

    /// <summary>
    /// Завершает панорамирование/редактирование при отпускании кнопки мыши.
    /// </summary>
    /// <param name="e">Аргументы события отпускания кнопки мыши.</param>
    /// <remarks>Возвращаемое значение: отсутствует.</remarks>
    protected override void OnMouseUp(MouseEventArgs e)
    {
        _isPanning = false;
        _isDraggingEdit = false;
        Cursor = Cursors.Default;
        base.OnMouseUp(e);
    }

    /// <summary>
    /// Обрабатывает изменение размера контрола и корректирует допустимые смещения viewport.
    /// </summary>
    /// <param name="e">Аргументы события изменения размера.</param>
    /// <remarks>Возвращаемое значение: отсутствует.</remarks>
    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        ClampOffsets();
        Invalidate();
    }

    /// <summary>
    /// Сообщает форме, что стрелки должны обрабатываться как клавиши ввода этим контролом.
    /// </summary>
    /// <param name="keyData">Комбинация клавиш.</param>
    /// <returns>true, если клавиша должна считаться input-key; иначе результат базовой реализации.</returns>
    protected override bool IsInputKey(Keys keyData)
    {
        if (keyData is Keys.Up or Keys.Down or Keys.Left or Keys.Right)
        {
            return true;
        }

        return base.IsInputKey(keyData);
    }

    /// <summary>
    /// Освобождает графические ресурсы контрола.
    /// </summary>
    /// <param name="disposing">true для освобождения управляемых ресурсов; иначе false.</param>
    /// <remarks>Возвращаемое значение: отсутствует.</remarks>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _aliveBrush.Dispose();
            _deadBrush.Dispose();
            _gridPen.Dispose();
        }

        base.Dispose(disposing);
    }

    /// <summary>
    /// Инициализирует режим панорамирования с сохранением стартовых координат.
    /// </summary>
    /// <param name="mouse">Экранная позиция мыши в момент начала панорамирования.</param>
    /// <remarks>Возвращаемое значение: отсутствует.</remarks>
    private void BeginPan(Point mouse)
    {
        _isPanning = true;
        _panStartMouse = mouse;
        _panStartOffsetX = _viewOffsetX;
        _panStartOffsetY = _viewOffsetY;
        Cursor = Cursors.SizeAll;
    }

    /// <summary>
    /// Преобразует экранные координаты в индексы клетки мира.
    /// </summary>
    /// <param name="point">Точка в координатах контрола.</param>
    /// <param name="x">Результирующая координата X клетки.</param>
    /// <param name="y">Результирующая координата Y клетки.</param>
    /// <returns>true, если точка попала в границы мира; иначе false.</returns>
    private bool TryScreenToCell(Point point, out int x, out int y)
    {
        x = (int)Math.Floor((point.X + _viewOffsetX) / CellSize);
        y = (int)Math.Floor((point.Y + _viewOffsetY) / CellSize);

        if (x < 0 || y < 0 || x >= _engine.Width || y >= _engine.Height)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Ограничивает смещения viewport допустимым диапазоном.
    /// Для мира меньше viewport используется фиксированное центрирование.
    /// </summary>
    /// <remarks>Возвращаемое значение: отсутствует.</remarks>
    private void ClampOffsets()
    {
        // Допустимые границы смещения по горизонтальной оси.
        (double minX, double maxX) = CalculateAxisBounds(_engine.Width, ClientSize.Width);

        // Допустимые границы смещения по вертикальной оси.
        (double minY, double maxY) = CalculateAxisBounds(_engine.Height, ClientSize.Height);

        _viewOffsetX = Math.Clamp(_viewOffsetX, minX, maxX);
        _viewOffsetY = Math.Clamp(_viewOffsetY, minY, maxY);
    }

    /// <summary>
    /// Вычисляет диапазон offset для одной оси viewport.
    /// </summary>
    /// <param name="cellsCount">Количество клеток мира по рассматриваемой оси.</param>
    /// <param name="viewportSize">Размер viewport по этой оси в пикселях.</param>
    /// <returns>
    /// Кортеж (Min, Max):
    /// - если мир меньше/равен viewport, Min == Max и соответствует центрированию;
    /// - если мир больше viewport, диапазон равен [0..maxOffset].
    /// </returns>
    private (double Min, double Max) CalculateAxisBounds(int cellsCount, int viewportSize)
    {
        // Размер мира по оси в пикселях с учетом текущего масштаба.
        double worldSize = cellsCount * CellSize;
        if (worldSize <= viewportSize)
        {
            // Смещение, при котором мир центрируется внутри viewport.
            double centeredOffset = -((viewportSize - worldSize) / 2d);
            return (centeredOffset, centeredOffset);
        }

        // Максимальное смещение для прокрутки по оси, если мир больше viewport.
        (double minX, double maxX) = CalculateAxisBounds(_engine.Width, ClientSize.Width);
        (double minY, double maxY) = CalculateAxisBounds(_engine.Height, ClientSize.Height);

        _viewOffsetX = Math.Clamp(_viewOffsetX, minX, maxX);
        _viewOffsetY = Math.Clamp(_viewOffsetY, minY, maxY);
    }

    private (double Min, double Max) CalculateAxisBounds(int cellsCount, int viewportSize)
    {
        double worldSize = cellsCount * CellSize;
        if (worldSize <= viewportSize)
        {
            double centeredOffset = -((viewportSize - worldSize) / 2d);
            return (centeredOffset, centeredOffset);
        }

        double maxOffset = worldSize - viewportSize;
        return (0d, maxOffset);
    }
}
