using Android.Content;
using Android.Graphics;
using Android.Runtime;
using Android.Util;
using Android.Views;
using AndroidX.Core.Content;
using ElectronicHelper.Core;
using Path = Android.Graphics.Path;

namespace ElectronicHelper;

/// <summary>Draws a through-hole resistor with its colour bands.</summary>
[Register("lk.stechbuzz.electronic.ResistorView")]
public sealed class ResistorView : View
{
    private readonly Paint _fill = new(PaintFlags.AntiAlias);
    private readonly Paint _outline = new(PaintFlags.AntiAlias);
    private readonly Path _body = new();
    private IReadOnlyList<BandColor> _bands = [];

    public ResistorView(Context context, IAttributeSet? attrs) : base(context, attrs)
    {
        _outline.SetStyle(Paint.Style.Stroke);
        _outline.StrokeWidth = context.Dp(1.5f);
        _outline.Color = new Color(context.ThemeColor(Resource.Attribute.colorOutline));
    }

    public void SetBands(IReadOnlyList<BandColor> bands)
    {
        _bands = bands;
        var names = string.Join(", ", bands.Select(band => ResistorColorCode.Info(band).Name));
        ContentDescription = Context!.Format(Resource.String.resistor_view_description, names);
        Invalidate();
    }

    protected override void OnDraw(Canvas canvas)
    {
        base.OnDraw(canvas);

        float width = Width, height = Height, middle = height / 2;
        var bodyHeight = Math.Min(height * 0.64f, Context!.Dp(76));
        var bodyWidth = Math.Min(width * 0.74f, bodyHeight * 3.8f);
        var left = (width - bodyWidth) / 2;
        var top = middle - bodyHeight / 2;
        var radius = bodyHeight * 0.28f;

        // Leads
        _fill.Color = new Color(ContextCompat.GetColor(Context, Resource.Color.resistor_lead));
        var lead = Context!.Dp(5);
        canvas.DrawRoundRect(Context!.Dp(8), middle - lead / 2f, width - Context!.Dp(8), middle + lead / 2f, lead / 2f, lead / 2f, _fill);

        // Body
        _body.Reset();
        _body.AddRoundRect(left, top, left + bodyWidth, top + bodyHeight, radius, radius, Path.Direction.Cw!);
        _fill.Color = new Color(ContextCompat.GetColor(Context, Resource.Color.resistor_body));
        canvas.DrawPath(_body, _fill);

        // Bands, clipped to the body's rounded shape
        canvas.Save();
        canvas.ClipPath(_body);
        var bandWidth = bodyWidth * 0.075f;
        var positions = BandPositions(_bands.Count);
        for (var i = 0; i < _bands.Count; i++)
        {
            var centre = left + bodyWidth * positions[i];
            _fill.Color = Color.ParseColor(ResistorColorCode.Info(_bands[i]).Hex);
            canvas.DrawRect(centre - bandWidth / 2, top, centre + bandWidth / 2, top + bodyHeight, _fill);
            // A hairline keeps white and beige-adjacent bands visible against the body.
            canvas.DrawRect(centre - bandWidth / 2, top, centre + bandWidth / 2, top + bodyHeight, _outline);
        }
        canvas.Restore();

        canvas.DrawPath(_body, _outline);
    }

    /// <summary>
    /// Where each band sits along the body, from 0 to 1. Value bands group on the left;
    /// tolerance (and temperature coefficient) stand apart on the right, as on a real part.
    /// </summary>
    private static float[] BandPositions(int count) => count switch
    {
        3 => [0.18f, 0.32f, 0.46f],
        4 => [0.16f, 0.30f, 0.44f, 0.84f],
        5 => [0.14f, 0.27f, 0.40f, 0.53f, 0.86f],
        6 => [0.12f, 0.24f, 0.36f, 0.48f, 0.74f, 0.88f],
        _ => [],
    };
}
