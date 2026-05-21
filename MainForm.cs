namespace TreeCoursework;

/// <summary>
/// Главное окно приложения (аналог ContentView в SwiftUI).
/// Левая панель: настройки, генерация, список деревьев, лог запросов.
/// Правая панель: предпросмотр текущего дерева.
/// </summary>
public sealed class MainForm : Form
{
    private readonly ApiClient _api = new();

    // ── Состояние ─────────────────────────────────────────────────────────────
    private TreeRecord? _currentTree;
    private int? _selectedTreeId;

    // ── Controls: Laravel API ─────────────────────────────────────────────────
    private readonly TextBox _txtUrl = new()
    {
        Text = "http://127.0.0.1:8000/api",
        Width = 260,
        Font = new Font("Consolas", 9)
    };

    // ── Controls: Генерация ───────────────────────────────────────────────────
    private readonly TextBox      _txtTitle   = new() { Width = 260, PlaceholderText = "Название дерева (необязательно)" };
    private readonly RadioButton  _rbVert     = new() { Text = "Вертикально",   AutoSize = true, Checked = true };
    private readonly RadioButton  _rbHoriz    = new() { Text = "Горизонтально", AutoSize = true };
    private readonly NumericUpDown _nudDepth  = new() { Minimum = 1, Maximum = 7, Value = 4, Width = 60 };
    private readonly TrackBar     _trkProb    = new() { Minimum = 25, Maximum = 90, Value = 60, TickFrequency = 5, Width = 200, SmallChange = 5, LargeChange = 5 };
    private readonly Label        _lblProbVal = new() { AutoSize = true, Text = "0.60" };
    private readonly NumericUpDown _nudSeed   = new() { Minimum = 1, Maximum = 999_999, Value = 42, Width = 100 };
    private readonly Button       _btnRandSeed= Btn("Случайно");
    private readonly Button       _btnGenerate= Btn("Сгенерировать", primary: true);
    private readonly Button       _btnLatest  = Btn("Последнее");
    private readonly Button       _btnRefresh = Btn("Обновить данные");

    // ── Controls: Список деревьев ─────────────────────────────────────────────
    private readonly ListView _lvTrees = new()
    {
        View        = View.Details,
        FullRowSelect = true,
        GridLines   = true,
        Height      = 160,
        Font        = new Font("Segoe UI", 9)
    };

    // ── Controls: Лог запросов ────────────────────────────────────────────────
    private readonly ListView _lvLogs = new()
    {
        View        = View.Details,
        FullRowSelect = true,
        GridLines   = true,
        Height      = 150,
        Font        = new Font("Segoe UI", 9)
    };

    // ── Canvas ────────────────────────────────────────────────────────────────
    private readonly TreeCanvasControl _canvas = new() { Dock = DockStyle.Fill };

    // ── Status ────────────────────────────────────────────────────────────────
    private readonly Label _lblStatus = new()
    {
        Dock = DockStyle.Bottom,
        Height = 24,
        ForeColor = Color.Gray,
        Font = new Font("Segoe UI", 9),
        Padding = new Padding(4, 4, 0, 0)
    };

    // ── Constructor ───────────────────────────────────────────────────────────
    public MainForm()
    {
        Text = "Курсовая — Визуализация дерева";
        Size = new Size(1100, 780);
        MinimumSize = new Size(800, 600);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(242, 242, 247);

        BuildLayout();
        WireEvents();

        Load += async (_, _) => await ReloadAllAsync(loadLatest: true);
    }

    // ── Layout ────────────────────────────────────────────────────────────────
    private void BuildLayout()
    {
        // Колонки списка деревьев
        _lvTrees.Columns.Add("Название",     160);
        _lvTrees.Columns.Add("Ориентация",    90);
        _lvTrees.Columns.Add("Глубина",       60);
        _lvTrees.Columns.Add("Узлы",          55);
        _lvTrees.Columns.Add("Дата",         130);

        // Колонки лога
        _lvLogs.Columns.Add("Метод",  55);
        _lvLogs.Columns.Add("Эндпоинт", 160);
        _lvLogs.Columns.Add("Статус",  55);
        _lvLogs.Columns.Add("Дерево", 110);
        _lvLogs.Columns.Add("Время",  120);

        // ── Левая панель (прокручиваемая) ─────────────────────────────────────
        var leftScroll = new Panel
        {
            Dock = DockStyle.Left,
            Width = 360,
            AutoScroll = true,
            BackColor = Color.FromArgb(242, 242, 247),
            Padding = new Padding(10, 10, 4, 10)
        };

        var stack = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoSize = true,
            Dock = DockStyle.Top,
            Padding = new Padding(0)
        };

        stack.Controls.Add(MakeGroup("Laravel API", BuildApiPanel()));
        stack.Controls.Add(MakeGroup("Генерация дерева", BuildGenerationPanel()));
        stack.Controls.Add(MakeGroup("Список деревьев", _lvTrees));
        stack.Controls.Add(MakeGroup("История запросов", _lvLogs));

        leftScroll.Controls.Add(stack);

        // ── Разделитель ───────────────────────────────────────────────────────
        var splitter = new Splitter { Dock = DockStyle.Left, Width = 5, BackColor = Color.FromArgb(210, 210, 220) };

        // ── Правая панель (canvas) ────────────────────────────────────────────
        var rightPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(235, 235, 242) };
        rightPanel.Controls.Add(_canvas);

        Controls.Add(rightPanel);
        Controls.Add(splitter);
        Controls.Add(leftScroll);
        Controls.Add(_lblStatus);
    }

    private Panel BuildApiPanel()
    {
        var p = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, AutoSize = true, WrapContents = false };
        p.Controls.Add(new Label { Text = "URL бэкенда:", AutoSize = true });
        p.Controls.Add(_txtUrl);
        p.Controls.Add(new Label { Text = "Симулятор: 127.0.0.1. iPhone: IP Mac в сети Wi-Fi.", AutoSize = true, ForeColor = Color.Gray, Font = new Font("Segoe UI", 8), Padding = new Padding(0, 2, 0, 4) });
        p.Controls.Add(_btnRefresh);
        return p;
    }

    private Panel BuildGenerationPanel()
    {
        var p = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, AutoSize = true, WrapContents = false, Padding = new Padding(0) };

        p.Controls.Add(WithLabel("Название:", _txtTitle));

        var orientRow = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.LeftToRight };
        orientRow.Controls.Add(new Label { Text = "Ориентация:", AutoSize = true, Padding = new Padding(0, 3, 4, 0) });
        orientRow.Controls.Add(_rbVert);
        orientRow.Controls.Add(_rbHoriz);
        p.Controls.Add(orientRow);

        p.Controls.Add(WithLabel("Глубина:", _nudDepth));

        var probRow = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.LeftToRight, Padding = new Padding(0, 4, 0, 0) };
        probRow.Controls.Add(new Label { Text = "Вероятность дочернего узла:", AutoSize = true, Padding = new Padding(0, 6, 4, 0) });
        probRow.Controls.Add(_lblProbVal);
        p.Controls.Add(probRow);
        p.Controls.Add(_trkProb);

        var seedRow = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.LeftToRight };
        seedRow.Controls.Add(new Label { Text = "Seed:", AutoSize = true, Padding = new Padding(0, 6, 4, 0) });
        seedRow.Controls.Add(_nudSeed);
        seedRow.Controls.Add(_btnRandSeed);
        p.Controls.Add(seedRow);

        var btnRow = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.LeftToRight, Padding = new Padding(0, 4, 0, 0) };
        btnRow.Controls.Add(_btnGenerate);
        btnRow.Controls.Add(_btnLatest);
        p.Controls.Add(btnRow);

        return p;
    }

    // ── Подписка на события ───────────────────────────────────────────────────
    private void WireEvents()
    {
        _trkProb.ValueChanged += (_, _) => _lblProbVal.Text = (_trkProb.Value / 100.0).ToString("0.00");

        _btnRefresh.Click  += async (_, _) => await ReloadAllAsync(loadLatest: true);
        _btnGenerate.Click += async (_, _) => await GenerateAsync();
        _btnLatest.Click   += async (_, _) => await LoadLatestAsync();
        _btnRandSeed.Click += (_, _) => _nudSeed.Value = Random.Shared.Next(1, 999_999);

        _lvTrees.DoubleClick += (_, _) =>
        {
            if (_lvTrees.SelectedItems.Count == 0) return;
            var id = (int)_lvTrees.SelectedItems[0].Tag!;
            new TreeGraphForm(_api, _txtUrl.Text, id).Show(this);
        };

        _lvTrees.SelectedIndexChanged += async (_, _) =>
        {
            if (_lvTrees.SelectedItems.Count == 0) return;
            var id = (int)_lvTrees.SelectedItems[0].Tag!;
            if (id == _selectedTreeId) return;
            await LoadTreePreviewAsync(id);
        };
    }

    // ── Бизнес-логика ─────────────────────────────────────────────────────────
    private async Task ReloadAllAsync(bool loadLatest)
    {
        SetStatus("Обновление…");
        SetBusy(true);
        try
        {
            var treesTask = _api.FetchTreesAsync(_txtUrl.Text);
            var logsTask  = _api.FetchLogsAsync(_txtUrl.Text);
            await Task.WhenAll(treesTask, logsTask);

            FillTrees(treesTask.Result);
            FillLogs(logsTask.Result);

            if (loadLatest)
            {
                try
                {
                    var latest = await _api.FetchLatestTreeAsync(_txtUrl.Text);
                    ApplyTree(latest);
                    SetStatus("Последнее дерево загружено.");
                }
                catch (ApiException ex) when (ex.StatusCode == 404)
                {
                    _currentTree = null;
                    _canvas.SetTree(null, "vertical");
                    SetStatus("Сохранённых деревьев пока нет.");
                }
            }
            else
            {
                SetStatus("Данные обновлены.");
            }
        }
        catch (Exception ex)
        {
            SetStatus($"Ошибка: {ex.Message}");
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async Task GenerateAsync()
    {
        SetStatus("Генерация…");
        SetBusy(true);
        try
        {
            var req = new GenerateTreeRequest
            {
                Title            = string.IsNullOrWhiteSpace(_txtTitle.Text) ? null : _txtTitle.Text.Trim(),
                Orientation      = _rbVert.Checked ? "vertical" : "horizontal",
                MaxDepth         = (int)_nudDepth.Value,
                ChildProbability = _trkProb.Value / 100.0,
                Seed             = (int)_nudSeed.Value
            };

            var tree = await _api.GenerateTreeAsync(_txtUrl.Text, req);
            ApplyTree(tree);

            var treesTask = _api.FetchTreesAsync(_txtUrl.Text);
            var logsTask  = _api.FetchLogsAsync(_txtUrl.Text);
            await Task.WhenAll(treesTask, logsTask);
            FillTrees(treesTask.Result);
            FillLogs(logsTask.Result);

            SetStatus($"Дерево сгенерировано и сохранено. ID={tree.Id}");
        }
        catch (Exception ex)
        {
            SetStatus($"Ошибка: {ex.Message}");
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async Task LoadLatestAsync()
    {
        SetStatus("Загрузка последнего дерева…");
        SetBusy(true);
        try
        {
            var tree = await _api.FetchLatestTreeAsync(_txtUrl.Text);
            ApplyTree(tree);
            SetStatus($"Загружено последнее дерево. ID={tree.Id}");
        }
        catch (ApiException ex) when (ex.StatusCode == 404)
        {
            SetStatus("Сохранённых деревьев пока нет.");
        }
        catch (Exception ex)
        {
            SetStatus($"Ошибка: {ex.Message}");
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async Task LoadTreePreviewAsync(int id)
    {
        SetStatus($"Загрузка дерева #{id}…");
        try
        {
            var tree = await _api.FetchTreeAsync(_txtUrl.Text, id);
            ApplyTree(tree);
            SetStatus($"Дерево #{id} загружено. Двойной клик — полный граф.");
        }
        catch (Exception ex)
        {
            SetStatus($"Ошибка: {ex.Message}");
        }
    }

    // ── Обновление UI ─────────────────────────────────────────────────────────
    private void ApplyTree(TreeRecord tree)
    {
        _currentTree    = tree;
        _selectedTreeId = tree.Id;

        // Синхронизируем поля ввода
        _txtTitle.Text     = tree.Title ?? "";
        _nudDepth.Value    = Math.Clamp(tree.MaxDepth, 1, 7);
        _trkProb.Value     = (int)(tree.ChildProbability * 100);
        _nudSeed.Value     = Math.Clamp(tree.Seed, 1, 999_999);
        _rbVert.Checked    = tree.Orientation == "vertical";
        _rbHoriz.Checked   = tree.Orientation == "horizontal";

        // Рисуем превью
        _canvas.SetTree(tree.Root, tree.Orientation);

        // Подсветка выбранной строки
        HighlightTreeRow(tree.Id);
    }

    private void FillTrees(List<TreeSummary> trees)
    {
        _lvTrees.Items.Clear();
        foreach (var t in trees)
        {
            var item = new ListViewItem(t.DisplayTitle) { Tag = t.Id };
            item.SubItems.Add(t.Orientation == "vertical" ? "Верт." : "Гориз.");
            item.SubItems.Add(t.MaxDepth.ToString());
            item.SubItems.Add(t.NodeCount.ToString());
            item.SubItems.Add(t.GeneratedAt?.ToString("dd.MM.yyyy HH:mm") ?? "—");
            _lvTrees.Items.Add(item);
        }

        if (_selectedTreeId.HasValue) HighlightTreeRow(_selectedTreeId.Value);
    }

    private void FillLogs(List<RequestLogEntry> logs)
    {
        _lvLogs.Items.Clear();
        foreach (var l in logs)
        {
            var item = new ListViewItem(l.Method);
            item.SubItems.Add(l.Endpoint);
            item.SubItems.Add(l.StatusCode.ToString());
            item.ForeColor = l.StatusCode >= 400 ? Color.Red : Color.FromArgb(0, 140, 0);
            item.SubItems.Add(l.Tree?.DisplayTitle ?? l.Action);
            item.SubItems.Add(l.CreatedAt?.ToString("dd.MM.yyyy HH:mm:ss") ?? "—");
            _lvLogs.Items.Add(item);
        }
    }

    private void HighlightTreeRow(int id)
    {
        foreach (ListViewItem item in _lvTrees.Items)
            item.BackColor = (int)item.Tag! == id
                ? Color.FromArgb(210, 230, 255)
                : SystemColors.Window;
    }

    private void SetStatus(string text) => _lblStatus.Text = text;

    private void SetBusy(bool busy)
    {
        _btnGenerate.Enabled = !busy;
        _btnLatest.Enabled   = !busy;
        _btnRefresh.Enabled  = !busy;
        Cursor = busy ? Cursors.WaitCursor : Cursors.Default;
    }

    // ── Вспомогательные фабрики ───────────────────────────────────────────────
    private static Button Btn(string text, bool primary = false)
    {
        var b = new Button
        {
            Text      = text,
            AutoSize  = true,
            FlatStyle = primary ? FlatStyle.Flat : FlatStyle.Standard,
            Margin    = new Padding(0, 0, 6, 0)
        };
        if (primary)
        {
            b.BackColor = Color.FromArgb(0, 122, 255);
            b.ForeColor = Color.White;
            b.FlatAppearance.BorderSize = 0;
        }
        return b;
    }

    private static GroupBox MakeGroup(string title, Control content)
    {
        content.Dock = DockStyle.Fill;
        var gb = new GroupBox
        {
            Text        = title,
            Font        = new Font("Segoe UI", 9, FontStyle.Bold),
            AutoSize    = true,
            Width       = 336,
            Margin      = new Padding(0, 0, 0, 8),
            Padding     = new Padding(8, 16, 8, 8)
        };
        gb.Controls.Add(content);
        return gb;
    }

    private static Panel WithLabel(string labelText, Control ctrl)
    {
        var row = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.LeftToRight,
            AutoSize      = true,
            Margin        = new Padding(0, 2, 0, 2)
        };
        row.Controls.Add(new Label { Text = labelText, AutoSize = true, Padding = new Padding(0, 6, 4, 0) });
        row.Controls.Add(ctrl);
        return row;
    }
}
