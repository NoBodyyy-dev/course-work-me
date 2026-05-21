using System.Drawing.Drawing2D;

namespace TreeCoursework;

/// <summary>
/// Кастомный контрол для отрисовки дерева: узлы + направленные рёбра со стрелками.
/// </summary>
public sealed class TreeCanvasControl : Panel
{
    // Размеры узла и отступы (аналог Swift-констант)
    private const float NodeW = 120f;
    private const float NodeH = 70f;
    private const float LevelSpacing = 80f;
    private const float SiblingSpacing = 20f;
    private const float Pad = 40f;

    private static readonly Color Accent = Color.FromArgb(0, 122, 255);
    private static readonly Color CardBg = Color.White;
    private static readonly Color CanvasBg = Color.FromArgb(235, 235, 242);

    private record PositionedNode(TreeNode Node, PointF Center);
    private record Edge(int ParentId, int ChildId);

    private List<PositionedNode> _nodes = new();
    private List<Edge> _edges = new();
    private Dictionary<int, PointF> _centers = new();

    public TreeCanvasControl()
    {
        DoubleBuffered = true;
        AutoScroll = true;
        BackColor = CanvasBg;
    }

    public void SetTree(TreeNode? root, string orientation)
    {
        _nodes = new();
        _edges = new();
        _centers = new();

        if (root is null)
        {
            AutoScrollMinSize = Size.Empty;
            Invalidate();
            return;
        }

        // ── 1. Вычисляем «сырые» позиции (leaf-X, depth-Y) ──────────────────
        var rawPos = new Dictionary<int, PointF>();
        var rawNodes = new List<TreeNode>();
        float nextLeafX = 0;
        int maxDepth = 0;

        void Visit(TreeNode n, int depth)
        {
            rawNodes.Add(n);
            maxDepth = Math.Max(maxDepth, depth);

            if (n.Children.Count == 0)
            {
                rawPos[n.Id] = new PointF(nextLeafX++, depth);
                return;
            }

            float first = float.MaxValue, last = float.MinValue;
            foreach (var child in n.Children)
            {
                _edges.Add(new Edge(n.Id, child.Id));
                Visit(child, depth + 1);
                float cx = rawPos[child.Id].X;
                if (cx < first) first = cx;
                if (cx > last) last = cx;
            }
            rawPos[n.Id] = new PointF((first + last) / 2f, depth);
        }
        Visit(root, 0);

        // ── 2. Переводим в экранные координаты с учётом ориентации ───────────
        float hStep = NodeW + SiblingSpacing;
        float vStep = NodeH + LevelSpacing;
        float maxBreadth = rawPos.Values.Max(p => p.X);

        PointF Orient(PointF raw)
        {
            float x = Pad + raw.X * hStep + NodeW / 2f;
            float y = Pad + raw.Y * vStep + NodeH / 2f;
            return orientation == "horizontal" ? new PointF(y, x) : new PointF(x, y);
        }

        foreach (var (id, raw) in rawPos)
            _centers[id] = Orient(raw);

        _nodes = rawNodes
            .Where(n => _centers.ContainsKey(n.Id))
            .Select(n => new PositionedNode(n, _centers[n.Id]))
            .ToList();

        // ── 3. Размер холста ──────────────────────────────────────────────────
        float vW = Pad * 2 + (maxBreadth + 1) * hStep + NodeW;
        float vH = Pad * 2 + maxDepth * vStep + NodeH;
        var canvasSize = orientation == "horizontal"
            ? new SizeF(vH, vW)
            : new SizeF(vW, vH);

        AutoScrollMinSize = new Size((int)canvasSize.Width + 20, (int)canvasSize.Height + 20);
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
        g.TranslateTransform(AutoScrollPosition.X, AutoScrollPosition.Y);

        foreach (var edge in _edges)
        {
            if (_centers.TryGetValue(edge.ParentId, out var from) &&
                _centers.TryGetValue(edge.ChildId, out var to))
                DrawEdge(g, from, to);
        }

        foreach (var (node, center) in _nodes)
            DrawNode(g, node, center);
    }

    // ── Рисование ребра со стрелкой ──────────────────────────────────────────
    private static void DrawEdge(Graphics g, PointF from, PointF to)
    {
        const float margin = 22f;
        var (start, end) = Shorten(from, to, margin);

        float dx = end.X - start.X, dy = end.Y - start.Y;
        float dist = MathF.Sqrt(dx * dx + dy * dy);
        if (dist < 8) return;

        float ux = dx / dist, uy = dy / dist;
        const float arrowLen = 12f, arrowHalf = 7f;

        var arrowBase = new PointF(end.X - ux * arrowLen, end.Y - uy * arrowLen);
        var left  = new PointF(arrowBase.X - uy * arrowHalf,  arrowBase.Y + ux * arrowHalf);
        var right = new PointF(arrowBase.X + uy * arrowHalf,  arrowBase.Y - ux * arrowHalf);

        using var pen = new Pen(Color.FromArgb(140, Accent), 2.2f)
        {
            StartCap = LineCap.Round,
            EndCap   = LineCap.Round
        };
        g.DrawLine(pen, start, arrowBase);

        using var fill = new SolidBrush(Color.FromArgb(160, Accent));
        g.FillPolygon(fill, new[] { left, end, right });
    }

    private static (PointF, PointF) Shorten(PointF from, PointF to, float margin)
    {
        float dx = to.X - from.X, dy = to.Y - from.Y;
        float len = MathF.Sqrt(dx * dx + dy * dy);
        if (len <= margin * 2) return (from, to);
        float ux = dx / len, uy = dy / len;
        return (
            new PointF(from.X + ux * margin, from.Y + uy * margin),
            new PointF(to.X  - ux * margin, to.Y  - uy * margin)
        );
    }

    // ── Рисование узла ────────────────────────────────────────────────────────
    private static void DrawNode(Graphics g, TreeNode node, PointF center)
    {
        var rect = new RectangleF(center.X - NodeW / 2f, center.Y - NodeH / 2f, NodeW, NodeH);

        // Тень
        var shadow = RectangleF.Inflate(rect, 1, 2);
        shadow.Offset(0, 2);
        using var shadowBrush = new SolidBrush(Color.FromArgb(25, 0, 0, 0));
        FillRounded(g, shadowBrush, shadow, 8);

        // Фон
        using var bg = new SolidBrush(CardBg);
        FillRounded(g, bg, rect, 8);

        // Граница
        using var border = new Pen(Color.FromArgb(90, Accent), 1.5f);
        DrawRounded(g, border, rect, 8);

        // Текст: три строки
        using var fLabel   = new Font("Segoe UI", 10, FontStyle.Bold);
        using var fCaption = new Font("Segoe UI", 8);
        using var fSmall   = new Font("Segoe UI", 7);
        using var primary   = new SolidBrush(Color.FromArgb(25, 25, 25));
        using var secondary = new SolidBrush(Color.FromArgb(120, 120, 120));

        var sf = new StringFormat
        {
            Alignment     = StringAlignment.Center,
            LineAlignment = StringAlignment.Center,
            Trimming      = StringTrimming.EllipsisCharacter
        };

        float third = NodeH / 3f;
        g.DrawString(node.Label,           fLabel,   primary,   new RectangleF(rect.X, rect.Y,               NodeW, third), sf);
        g.DrawString(node.ChildNumbersText, fCaption, secondary, new RectangleF(rect.X, rect.Y + third,       NodeW, third), sf);
        g.DrawString($"Уровень {node.Depth}", fSmall, secondary, new RectangleF(rect.X, rect.Y + third * 2f, NodeW, third), sf);
    }

    // ── Вспомогательные методы для скруглённых прямоугольников ───────────────
    private static void FillRounded(Graphics g, Brush brush, RectangleF r, float radius)
    {
        using var path = RoundedPath(r, radius);
        g.FillPath(brush, path);
    }

    private static void DrawRounded(Graphics g, Pen pen, RectangleF r, float radius)
    {
        using var path = RoundedPath(r, radius);
        g.DrawPath(pen, path);
    }

    private static GraphicsPath RoundedPath(RectangleF r, float rad)
    {
        float d = rad * 2;
        var p = new GraphicsPath();
        p.AddArc(r.X,          r.Y,           d, d, 180, 90);
        p.AddArc(r.Right - d,  r.Y,           d, d, 270, 90);
        p.AddArc(r.Right - d,  r.Bottom - d,  d, d,   0, 90);
        p.AddArc(r.X,          r.Bottom - d,  d, d,  90, 90);
        p.CloseFigure();
        return p;
    }
}
