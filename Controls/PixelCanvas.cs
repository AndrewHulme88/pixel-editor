using System;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using pixel_editor.Rendering;
using PixelEditor.Core.Documents;

namespace pixel_editor.Controls;

public sealed class PixelCanvas : Control
{
    public static readonly StyledProperty<PixelDocument?> DocumentProperty =
        AvaloniaProperty.Register<PixelCanvas, PixelDocument?>(nameof(Document));

    private const double CheckerSize = 12;
    private static readonly IBrush CheckerLight = new SolidColorBrush(Color.FromRgb(214, 214, 214));
    private static readonly IBrush CheckerDark = new SolidColorBrush(Color.FromRgb(174, 174, 174));
    private static readonly IPen CanvasBorder = new Pen(new SolidColorBrush(Color.FromRgb(96, 96, 96)));

    private WriteableBitmap? _bitmap;

    public PixelCanvas()
    {
        ClipToBounds = true;
        RenderOptions.SetBitmapInterpolationMode(this, BitmapInterpolationMode.None);
    }

    public PixelDocument? Document
    {
        get => GetValue(DocumentProperty);
        set => SetValue(DocumentProperty, value);
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        if (Document is null || _bitmap is null)
        {
            return;
        }

        var layout = CanvasLayout.Calculate(Document.Width, Document.Height, Bounds.Size);
        DrawTransparencyBackground(context, layout.Destination, new Rect(Bounds.Size));

        var source = new Rect(0, 0, Document.Width, Document.Height);
        context.DrawImage(_bitmap, source, layout.Destination);
        context.DrawRectangle(null, CanvasBorder, layout.Destination);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == DocumentProperty)
        {
            RebuildBitmap();
        }
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        if (_bitmap is null && Document is not null)
        {
            RebuildBitmap();
        }
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        DisposeBitmap();
        base.OnDetachedFromVisualTree(e);
    }

    private void RebuildBitmap()
    {
        DisposeBitmap();

        if (Document is null)
        {
            InvalidateVisual();
            return;
        }

        var buffer = PixelBufferBuilder.CreatePremultipliedBgra(Document);
        var bitmap = new WriteableBitmap(
            new PixelSize(Document.Width, Document.Height),
            new Vector(96, 96),
            PixelFormat.Bgra8888,
            AlphaFormat.Premul);

        using (var framebuffer = bitmap.Lock())
        {
            var sourceRowBytes = Document.Width * PixelBufferBuilder.BytesPerPixel;

            for (var y = 0; y < Document.Height; y++)
            {
                var sourceOffset = y * sourceRowBytes;
                var destination = IntPtr.Add(framebuffer.Address, y * framebuffer.RowBytes);
                Marshal.Copy(buffer, sourceOffset, destination, sourceRowBytes);
            }
        }

        _bitmap = bitmap;
        InvalidateVisual();
    }

    private static void DrawTransparencyBackground(
        DrawingContext context,
        Rect destination,
        Rect canvasBounds)
    {
        using var clip = context.PushClip(destination);
        context.FillRectangle(CheckerLight, destination);

        var visible = destination.Intersect(canvasBounds);
        var firstColumn = Math.Max(0, (int)Math.Floor((visible.X - destination.X) / CheckerSize));
        var firstRow = Math.Max(0, (int)Math.Floor((visible.Y - destination.Y) / CheckerSize));
        var lastColumn = (int)Math.Ceiling((visible.Right - destination.X) / CheckerSize);
        var lastRow = (int)Math.Ceiling((visible.Bottom - destination.Y) / CheckerSize);

        for (var row = firstRow; row < lastRow; row++)
        {
            for (var column = firstColumn; column < lastColumn; column++)
            {
                if ((row + column) % 2 == 0)
                {
                    continue;
                }

                var tile = new Rect(
                    destination.X + (column * CheckerSize),
                    destination.Y + (row * CheckerSize),
                    CheckerSize,
                    CheckerSize);

                context.FillRectangle(CheckerDark, tile);
            }
        }
    }

    private void DisposeBitmap()
    {
        _bitmap?.Dispose();
        _bitmap = null;
    }
}
