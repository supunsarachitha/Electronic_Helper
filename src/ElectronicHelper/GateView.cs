using Android.Content;
using Android.Graphics;
using Android.Runtime;
using Android.Util;
using Android.Views;
using AndroidX.Core.Content;
using ElectronicHelper.Core;
using Path = Android.Graphics.Path;

namespace ElectronicHelper;

/// <summary>Draws a logic gate's schematic symbol, with each wire coloured by its logic level.</summary>
[Register("lk.stechbuzz.electronic.GateView")]
public sealed class GateView : View
{
    private readonly Paint _wire = new(PaintFlags.AntiAlias);
    private readonly Paint _bodyFill = new(PaintFlags.AntiAlias);
    private readonly Paint _bodyStroke = new(PaintFlags.AntiAlias);
    private readonly Paint _dot = new(PaintFlags.AntiAlias);
    private readonly Paint _label = new(PaintFlags.AntiAlias);
    private readonly Path _path = new();
    private readonly Color _high;
    private readonly Color _low;

    private GateKind _kind = GateKind.And;
    private bool _a;
    private bool _b;

    public GateView(Context context, IAttributeSet? attrs) : base(context, attrs)
    {
        _high = new Color(ContextCompat.GetColor(context, Resource.Color.logic_high));
        _low = new Color(ContextCompat.GetColor(context, Resource.Color.logic_low));

        _wire.SetStyle(Paint.Style.Stroke);
        _wire.StrokeWidth = context.Dp(4);
        _wire.StrokeCap = Paint.Cap.Round;

        _bodyFill.Color = new Color(context.ThemeColor(Resource.Attribute.colorSurfaceContainerHigh));
        _bodyStroke.SetStyle(Paint.Style.Stroke);
        _bodyStroke.StrokeWidth = context.Dp(3);
        _bodyStroke.StrokeJoin = Paint.Join.Round;
        _bodyStroke.Color = new Color(context.ThemeColor(Resource.Attribute.colorOnSurface));

        _label.Color = new Color(context.ThemeColor(Resource.Attribute.colorOnSurface));
        _label.TextSize = TypedValue.ApplyDimension(ComplexUnitType.Sp, 16, context.Resources!.DisplayMetrics);
        _label.TextAlign = Paint.Align.Center;
        _label.FakeBoldText = true;
    }

    public void Set(GateKind kind, bool a, bool b)
    {
        (_kind, _a, _b) = (kind, a, b);
        var output = LogicGates.Evaluate(kind, a, b);
        ContentDescription = Context!.Format(
            Resource.String.gate_view_description, LogicGates.Info(kind).Name, output ? "1" : "0");
        Invalidate();
    }

    protected override void OnDraw(Canvas canvas)
    {
        base.OnDraw(canvas);
        var context = Context!;

        float width = Width, height = Height, middle = height / 2;
        var bodyHeight = Math.Min(height * 0.62f, context.Dp(104));
        var bodyWidth = bodyHeight * 1.2f;
        var x0 = (width - bodyWidth) / 2;
        var x1 = x0 + bodyWidth;
        var y0 = middle - bodyHeight / 2;
        var y1 = middle + bodyHeight / 2;

        var wireLength = Math.Min(context.Dp(84), x0 - context.Dp(40));
        var bubble = context.Dp(7);
        var gap = context.Dp(10);
        var output = LogicGates.Evaluate(_kind, _a, _b);

        var curvedBack = _kind is GateKind.Or or GateKind.Nor or GateKind.Xor or GateKind.Xnor;
        var exclusive = _kind is GateKind.Xor or GateKind.Xnor;
        var inverted = _kind is GateKind.Not or GateKind.Nand or GateKind.Nor or GateKind.Xnor;
        var bulge = bodyWidth * 0.3f;

        // Inputs. On a curved back the wire stops where it meets the curve rather than at the left edge.
        var inputs = _kind == GateKind.Not
            ? new[] { (Offset: 0.5f, Level: _a, Name: "A") }
            : [(Offset: 0.28f, Level: _a, Name: "A"), (Offset: 0.72f, Level: _b, Name: "B")];
        foreach (var input in inputs)
        {
            var y = y0 + bodyHeight * input.Offset;
            var inset = curvedBack ? 2 * input.Offset * (1 - input.Offset) * bulge : 0;
            var end = x0 + inset - (exclusive ? gap : 0);
            DrawWire(canvas, x0 - wireLength, y, end, y, input.Level);
            DrawTerminal(canvas, x0 - wireLength, y, input.Level, input.Name, -context.Dp(22));
        }

        // Output
        var outputStart = x1 + (inverted ? bubble * 2 : 0);
        DrawWire(canvas, outputStart, middle, x1 + wireLength, middle, output);
        DrawTerminal(canvas, x1 + wireLength, middle, output, "Q", context.Dp(22));

        // Body
        _path.Reset();
        if (_kind == GateKind.Not)
        {
            _path.MoveTo(x0, y0);
            _path.LineTo(x1, middle);
            _path.LineTo(x0, y1);
            _path.Close();
        }
        else if (curvedBack)
        {
            _path.MoveTo(x0, y0);
            _path.QuadTo(x0 + bodyWidth * 0.62f, y0, x1, middle);
            _path.QuadTo(x0 + bodyWidth * 0.62f, y1, x0, y1);
            _path.QuadTo(x0 + bulge, middle, x0, y0);
            _path.Close();
        }
        else
        {
            _path.MoveTo(x0, y0);
            _path.LineTo(x1 - bodyHeight / 2, y0);
            _path.ArcTo(new RectF(x1 - bodyHeight, y0, x1, y1), -90, 180);
            _path.LineTo(x0, y1);
            _path.Close();
        }
        canvas.DrawPath(_path, _bodyFill);
        canvas.DrawPath(_path, _bodyStroke);

        if (exclusive)
        {
            _path.Reset();
            _path.MoveTo(x0 - gap, y0);
            _path.QuadTo(x0 - gap + bulge, middle, x0 - gap, y1);
            canvas.DrawPath(_path, _bodyStroke);
        }

        if (inverted)
        {
            canvas.DrawCircle(x1 + bubble, middle, bubble, _bodyFill);
            canvas.DrawCircle(x1 + bubble, middle, bubble, _bodyStroke);
        }
    }

    private void DrawWire(Canvas canvas, float fromX, float fromY, float toX, float toY, bool level)
    {
        _wire.Color = level ? _high : _low;
        canvas.DrawLine(fromX, fromY, toX, toY, _wire);
    }

    /// <summary>The dot at a wire's free end, with its name and level beside it.</summary>
    private void DrawTerminal(Canvas canvas, float x, float y, bool level, string name, float labelOffset)
    {
        _dot.Color = level ? _high : _low;
        canvas.DrawCircle(x, y, Context!.Dp(6), _dot);

        var baseline = y - (_label.Descent() + _label.Ascent()) / 2;
        canvas.DrawText($"{name}={(level ? 1 : 0)}", x + labelOffset * 1.1f, baseline, _label);
    }
}
