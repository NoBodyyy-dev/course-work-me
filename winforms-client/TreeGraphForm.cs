namespace TreeCoursework;

/// <summary>
/// Окно полной визуализации выбранного дерева (аналог TreeGraphDetailView).
/// </summary>
public sealed class TreeGraphForm : Form
{
    private readonly ApiClient _api;
    private readonly string _baseUrl;
    private readonly int _treeId;

    private TreeRecord? _tree;

    // ── Controls ──────────────────────────────────────────────────────────────
    private readonly Label _lblTitle       = new() { Font = new Font("Segoe UI", 14, FontStyle.Bold), AutoSize = true };
    private readonly Label _lblDate        = new() { ForeColor = Color.Gray, AutoSize = true };
    private readonly Label _lblNodes       = MakeStat("Узлы");
    private readonly Label _lblDepth       = MakeStat("Глубина");
    private readonly Label _lblProb        = MakeStat("Вероятность");
    private readonly Label _lblSeed        = MakeStat("Seed");
    private readonly RadioButton _rbVert   = new() { Text = "Вертикально",   AutoSize = true, Checked = true };
    private readonly RadioButton _rbHoriz  = new() { Text = "Горизонтально", AutoSize = true };
    private readonly TreeCanvasControl _canvas = new() { Dock = DockStyle.Fill };
    private readonly Label _lblStatus      = new() { Dock = DockStyle.Bottom, Height = 24, ForeColor = Color.Gray };

    public TreeGraphForm(ApiClient api, string baseUrl, int treeId)
    {
        _api = api;
        _baseUrl = baseUrl;
        _treeId = treeId;

        Text = $"Граф дерева #{treeId}";
        Size = new Size(1000, 720);
        MinimumSize = new Size(600, 400);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Color.FromArgb(242, 242, 247);

        BuildLayout();

        _rbVert.CheckedChanged  += (_, _) => ApplyOrientation();
        _rbHoriz.CheckedChanged += (_, _) => ApplyOrientation();

        Load += async (_, _) => await LoadTreeAsync();
    }

    // ── Layout ────────────────────────────────────────────────────────────────
    private void BuildLayout()
    {
        // Верхняя панель с информацией
        var header = new Panel
        {
            Dock = DockStyle.Top,
            Height = 150,
            Padding = new Padding(14),
            BackColor = Color.White
        };

        // Строка с заголовком и датой
        var colInfo = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown,
            AutoSize = true,
            Location = new Point(14, 10)
        };
        colInfo.Controls.Add(_lblTitle);
        colInfo.Controls.Add(_lblDate);

        // Статы (2×2)
        var statsTable = new TableLayoutPanel
        {
            ColumnCount = 4,
            RowCount = 1,
            AutoSize = true,
            Location = new Point(14, 60)
        };
        foreach (var (caption, valueLabel) in new[]
        {
            ("Узлы",       _lblNodes),
            ("Глубина",    _lblDepth),
            ("Вероятность",_lblProb),
            ("Seed",       _lblSeed)
        })
        {
            var box = new Panel { Width = 110, Height = 52, Margin = new Padding(0, 0, 8, 0) };
            box.Paint += (_, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using var pen = new Pen(Color.FromArgb(200, 200, 210));
                e.Graphics.DrawRectangle(pen, 0, 0, box.Width - 1, box.Height - 1);
            };
            var lCap = new Label { Text = caption, Font = new Font("Segoe UI", 8), ForeColor = Color.Gray, Location = new Point(6, 6), AutoSize = true };
            valueLabel.Location = new Point(6, 24);
            box.Controls.Add(lCap);
            box.Controls.Add(valueLabel);
            statsTable.Controls.Add(box);
        }

        // Переключатель ориентации
        var orientPanel = new FlowLayoutPanel
        {
            AutoSize = true,
            Location = new Point(14, 118)
        };
        orientPanel.Controls.Add(new Label { Text = "Ориентация:", AutoSize = true, Padding = new Padding(0, 3, 4, 0) });
        orientPanel.Controls.Add(_rbVert);
        orientPanel.Controls.Add(_rbHoriz);

        header.Controls.Add(colInfo);
        header.Controls.Add(statsTable);
        header.Controls.Add(orientPanel);

        Controls.Add(_canvas);
        Controls.Add(header);
        Controls.Add(_lblStatus);
    }

    // ── Загрузка дерева ───────────────────────────────────────────────────────
    private async Task LoadTreeAsync()
    {
        _lblStatus.Text = "Загрузка…";
        try
        {
            _tree = await _api.FetchTreeAsync(_baseUrl, _treeId);
            PopulateInfo();
            ApplyOrientation();
            _lblStatus.Text = "";
        }
        catch (Exception ex)
        {
            _lblStatus.Text = $"Ошибка: {ex.Message}";
        }
    }

    private void PopulateInfo()
    {
        if (_tree is null) return;
        _lblTitle.Text = _tree.DisplayTitle;
        _lblDate.Text  = _tree.GeneratedAt?.ToString("dd.MM.yyyy HH:mm") ?? "Время не указано";
        _lblNodes.Text = _tree.NodeCount.ToString();
        _lblDepth.Text = _tree.MaxDepth.ToString();
        _lblProb.Text  = _tree.ChildProbability.ToString("0.00");
        _lblSeed.Text  = _tree.Seed.ToString();

        // Установить переключатель ориентации без лишнего события
        _rbVert.CheckedChanged  -= OnOrientChanged;
        _rbHoriz.CheckedChanged -= OnOrientChanged;
        _rbVert.Checked  = _tree.Orientation == "vertical";
        _rbHoriz.Checked = _tree.Orientation == "horizontal";
        _rbVert.CheckedChanged  += OnOrientChanged;
        _rbHoriz.CheckedChanged += OnOrientChanged;
    }

    private void OnOrientChanged(object? s, EventArgs e) => ApplyOrientation();

    private void ApplyOrientation()
    {
        if (_tree?.Root is null) return;
        var orient = _rbVert.Checked ? "vertical" : "horizontal";
        _canvas.SetTree(_tree.Root, orient);
    }

    // ── Фабрика лейбла-значения статы ────────────────────────────────────────
    private static Label MakeStat(string _) =>
        new() { Font = new Font("Segoe UI", 12, FontStyle.Bold), AutoSize = true };
}
