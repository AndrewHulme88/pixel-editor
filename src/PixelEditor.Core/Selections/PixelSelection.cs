using System.Numerics;
using PixelEditor.Core.Documents;

namespace PixelEditor.Core.Selections;

public sealed class PixelSelection
{
    private ulong[] _bits;

    public PixelSelection(int width, int height)
    {
        PixelDocumentLimits.ValidateDimensions(width, height);

        Width = width;
        Height = height;
        _bits = CreateStorage(width, height);
    }

    public event EventHandler? Changed;

    public int Width { get; private set; }

    public int Height { get; private set; }

    public int SelectedPixelCount { get; private set; }

    public int StorageByteCount => checked(_bits.Length * sizeof(ulong));

    public PixelSelectionBounds? Bounds { get; private set; }

    public bool HasSelection => SelectedPixelCount != 0;

    public bool Contains(int x, int y)
    {
        if ((uint)x >= (uint)Width || (uint)y >= (uint)Height)
        {
            return false;
        }

        var bitIndex = GetBitIndex(x, y);
        return (_bits[bitIndex >> 6] & (1UL << (bitIndex & 63))) != 0;
    }

    public bool ReplaceRectangleFromInclusiveCorners(
        int startX,
        int startY,
        int endX,
        int endY) => ReplaceRectangle(
            PixelSelectionBounds.FromInclusiveCorners(
                startX,
                startY,
                endX,
                endY,
                Width,
                Height));

    public bool ApplyRectangle(
        PixelSelectionBounds bounds,
        SelectionCombineMode mode) => mode switch
        {
            SelectionCombineMode.Replace => ReplaceRectangle(bounds),
            SelectionCombineMode.Add => AddRectangle(bounds),
            SelectionCombineMode.Subtract => SubtractRectangle(bounds),
            SelectionCombineMode.Intersect => IntersectRectangle(bounds),
            _ => throw new ArgumentOutOfRangeException(nameof(mode))
        };

    public IReadOnlyList<PixelSpan> CreateSpans()
    {
        if (Bounds is not { } bounds)
        {
            return Array.Empty<PixelSpan>();
        }

        var rectangularPixelCount = bounds.Width * bounds.Height;
        var estimatedSpanCount = SelectedPixelCount == rectangularPixelCount
            ? bounds.Height
            : Math.Min(SelectedPixelCount, checked(bounds.Height * 2));
        var spans = new List<PixelSpan>(estimatedSpanCount);

        for (var y = bounds.Y; y < bounds.Bottom; y++)
        {
            AppendRowSpans(spans, y, bounds.X, bounds.Right);
        }

        return spans;
    }

    public bool ReplaceRectangle(PixelSelectionBounds bounds)
    {
        ValidateBounds(bounds);

        if (Bounds == bounds && SelectedPixelCount == bounds.Width * bounds.Height)
        {
            return false;
        }

        Array.Clear(_bits);
        SelectedPixelCount = SetRectangle(bounds);
        Bounds = bounds;
        OnChanged();
        return true;
    }

    public bool AddRectangle(PixelSelectionBounds bounds)
    {
        ValidateBounds(bounds);

        var addedCount = SetRectangle(bounds);

        if (addedCount == 0)
        {
            return false;
        }

        SelectedPixelCount += addedCount;
        Bounds = Bounds is { } existing
            ? Union(existing, bounds)
            : bounds;
        OnChanged();
        return true;
    }

    public bool SubtractRectangle(PixelSelectionBounds bounds)
    {
        ValidateBounds(bounds);

        if (Bounds is not { } existing ||
            Intersect(existing, bounds) is not { } overlap)
        {
            return false;
        }

        var removedCount = ClearRectangle(overlap);

        if (removedCount == 0)
        {
            return false;
        }

        SelectedPixelCount -= removedCount;
        RecalculateBounds(existing);
        OnChanged();
        return true;
    }

    public bool IntersectRectangle(PixelSelectionBounds bounds)
    {
        ValidateBounds(bounds);

        if (Bounds is not { } existing || Contains(bounds, existing))
        {
            return false;
        }

        var overlap = Intersect(existing, bounds);
        var removedCount = 0;

        if (overlap is null)
        {
            removedCount = SelectedPixelCount;
            Array.Clear(_bits);
        }
        else
        {
            var retained = overlap.Value;

            for (var y = existing.Y; y < existing.Bottom; y++)
            {
                if (y < retained.Y || y >= retained.Bottom)
                {
                    removedCount += ClearRange(
                        GetBitIndex(existing.X, y),
                        existing.Width);
                    continue;
                }

                removedCount += ClearRange(
                    GetBitIndex(existing.X, y),
                    retained.X - existing.X);
                removedCount += ClearRange(
                    GetBitIndex(retained.Right, y),
                    existing.Right - retained.Right);
            }
        }

        if (removedCount == 0)
        {
            return false;
        }

        SelectedPixelCount -= removedCount;
        RecalculateBounds(existing);
        OnChanged();
        return true;
    }

    public bool Clear()
    {
        if (!HasSelection)
        {
            return false;
        }

        Array.Clear(_bits);
        SelectedPixelCount = 0;
        Bounds = null;
        OnChanged();
        return true;
    }

    public bool ResetCanvas(int width, int height)
    {
        PixelDocumentLimits.ValidateDimensions(width, height);

        if (Width == width && Height == height)
        {
            return Clear();
        }

        Width = width;
        Height = height;
        _bits = CreateStorage(width, height);
        SelectedPixelCount = 0;
        Bounds = null;
        OnChanged();
        return true;
    }

    private static ulong[] CreateStorage(int width, int height)
    {
        var bitCount = checked((long)width * height);
        return new ulong[checked((int)((bitCount + 63) / 64))];
    }

    private int SetRectangle(PixelSelectionBounds bounds)
    {
        var addedCount = 0;

        for (var y = bounds.Y; y < bounds.Bottom; y++)
        {
            addedCount += SetRange(GetBitIndex(bounds.X, y), bounds.Width);
        }

        return addedCount;
    }

    private int ClearRectangle(PixelSelectionBounds bounds)
    {
        var removedCount = 0;

        for (var y = bounds.Y; y < bounds.Bottom; y++)
        {
            removedCount += ClearRange(GetBitIndex(bounds.X, y), bounds.Width);
        }

        return removedCount;
    }

    private void AppendRowSpans(
        List<PixelSpan> spans,
        int y,
        int startX,
        int endX)
    {
        var activeStart = -1;
        var x = startX;

        while (x < endX)
        {
            var bitCount = Math.Min(64, endX - x);
            var selected = ReadBits(GetBitIndex(x, y), bitCount);
            var offset = 0;

            while (offset < bitCount)
            {
                var remainingBits = selected >> offset;

                if (remainingBits == 0)
                {
                    if (activeStart >= 0)
                    {
                        spans.Add(new PixelSpan(activeStart, y, x + offset - activeStart));
                        activeStart = -1;
                    }

                    break;
                }

                var zeroCount = Math.Min(
                    BitOperations.TrailingZeroCount(remainingBits),
                    bitCount - offset);

                if (zeroCount > 0)
                {
                    if (activeStart >= 0)
                    {
                        spans.Add(new PixelSpan(activeStart, y, x + offset - activeStart));
                        activeStart = -1;
                    }

                    offset += zeroCount;
                }

                if (offset >= bitCount)
                {
                    break;
                }

                remainingBits = selected >> offset;
                var oneCount = Math.Min(
                    BitOperations.TrailingZeroCount(~remainingBits),
                    bitCount - offset);
                activeStart = activeStart < 0 ? x + offset : activeStart;
                offset += oneCount;

                if (offset < bitCount)
                {
                    spans.Add(new PixelSpan(activeStart, y, x + offset - activeStart));
                    activeStart = -1;
                }
            }

            x += bitCount;
        }

        if (activeStart >= 0)
        {
            spans.Add(new PixelSpan(activeStart, y, endX - activeStart));
        }
    }

    private ulong ReadBits(int startBitIndex, int bitCount)
    {
        var wordIndex = startBitIndex >> 6;
        var bitOffset = startBitIndex & 63;
        var value = _bits[wordIndex] >> bitOffset;

        if (bitOffset != 0 && wordIndex + 1 < _bits.Length)
        {
            value |= _bits[wordIndex + 1] << (64 - bitOffset);
        }

        return bitCount == 64
            ? value
            : value & ((1UL << bitCount) - 1);
    }

    private int SetRange(int startBitIndex, int length)
    {
        var changedCount = 0;
        var bitIndex = startBitIndex;
        var remaining = length;

        while (remaining > 0)
        {
            var wordIndex = bitIndex >> 6;
            var bitOffset = bitIndex & 63;
            var bitCount = Math.Min(64 - bitOffset, remaining);
            var mask = CreateRangeMask(bitOffset, bitCount);
            var previous = _bits[wordIndex];
            var changed = mask & ~previous;
            _bits[wordIndex] = previous | mask;
            changedCount += BitOperations.PopCount(changed);
            bitIndex += bitCount;
            remaining -= bitCount;
        }

        return changedCount;
    }

    private int ClearRange(int startBitIndex, int length)
    {
        var changedCount = 0;
        var bitIndex = startBitIndex;
        var remaining = length;

        while (remaining > 0)
        {
            var wordIndex = bitIndex >> 6;
            var bitOffset = bitIndex & 63;
            var bitCount = Math.Min(64 - bitOffset, remaining);
            var mask = CreateRangeMask(bitOffset, bitCount);
            var previous = _bits[wordIndex];
            var changed = previous & mask;
            _bits[wordIndex] = previous & ~mask;
            changedCount += BitOperations.PopCount(changed);
            bitIndex += bitCount;
            remaining -= bitCount;
        }

        return changedCount;
    }

    private static ulong CreateRangeMask(int bitOffset, int bitCount) =>
        bitCount == 64
            ? ulong.MaxValue
            : ((1UL << bitCount) - 1) << bitOffset;

    private void RecalculateBounds(PixelSelectionBounds previousBounds)
    {
        if (!HasSelection)
        {
            Bounds = null;
            return;
        }

        var left = Width;
        var top = Height;
        var right = -1;
        var bottom = -1;

        for (var y = previousBounds.Y; y < previousBounds.Bottom; y++)
        {
            if (!TryGetSelectedRangeBounds(
                    y,
                    previousBounds.X,
                    previousBounds.Right,
                    out var rowLeft,
                    out var rowRight))
            {
                continue;
            }

            left = Math.Min(left, rowLeft);
            top = Math.Min(top, y);
            right = Math.Max(right, rowRight);
            bottom = y;
        }

        Bounds = new PixelSelectionBounds(
            left,
            top,
            (right - left) + 1,
            (bottom - top) + 1);
    }

    private bool TryGetSelectedRangeBounds(
        int y,
        int startX,
        int endX,
        out int left,
        out int right)
    {
        var rowLeft = Width;
        var rowRight = -1;
        var bitIndex = GetBitIndex(startX, y);
        var rowStartBitIndex = y * Width;
        var remaining = endX - startX;

        while (remaining > 0)
        {
            var wordIndex = bitIndex >> 6;
            var bitOffset = bitIndex & 63;
            var bitCount = Math.Min(64 - bitOffset, remaining);
            var mask = CreateRangeMask(bitOffset, bitCount);
            var selected = _bits[wordIndex] & mask;

            if (selected != 0)
            {
                var firstBitIndex = (wordIndex << 6) + BitOperations.TrailingZeroCount(selected);
                var lastBitIndex = (wordIndex << 6) + 63 - BitOperations.LeadingZeroCount(selected);
                rowLeft = Math.Min(rowLeft, firstBitIndex - rowStartBitIndex);
                rowRight = Math.Max(rowRight, lastBitIndex - rowStartBitIndex);
            }

            bitIndex += bitCount;
            remaining -= bitCount;
        }

        left = rowLeft;
        right = rowRight;
        return rowRight >= 0;
    }

    private int GetBitIndex(int x, int y) => checked((y * Width) + x);

    private void ValidateBounds(PixelSelectionBounds bounds)
    {
        if ((long)bounds.X + bounds.Width > Width ||
            (long)bounds.Y + bounds.Height > Height)
        {
            throw new ArgumentOutOfRangeException(
                nameof(bounds),
                "Selection bounds must be contained by the canvas.");
        }
    }

    private static PixelSelectionBounds Union(
        PixelSelectionBounds first,
        PixelSelectionBounds second)
    {
        var left = Math.Min(first.X, second.X);
        var top = Math.Min(first.Y, second.Y);
        var right = Math.Max(first.Right, second.Right);
        var bottom = Math.Max(first.Bottom, second.Bottom);
        return new PixelSelectionBounds(left, top, right - left, bottom - top);
    }

    private static PixelSelectionBounds? Intersect(
        PixelSelectionBounds first,
        PixelSelectionBounds second)
    {
        var left = Math.Max(first.X, second.X);
        var top = Math.Max(first.Y, second.Y);
        var right = Math.Min(first.Right, second.Right);
        var bottom = Math.Min(first.Bottom, second.Bottom);

        return left < right && top < bottom
            ? new PixelSelectionBounds(left, top, right - left, bottom - top)
            : null;
    }

    private static bool Contains(
        PixelSelectionBounds outer,
        PixelSelectionBounds inner) =>
        outer.X <= inner.X &&
        outer.Y <= inner.Y &&
        outer.Right >= inner.Right &&
        outer.Bottom >= inner.Bottom;

    private void OnChanged() => Changed?.Invoke(this, EventArgs.Empty);
}
