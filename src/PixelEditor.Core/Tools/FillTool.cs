using PixelEditor.Core.Documents;

namespace PixelEditor.Core.Tools;

public static class FillTool
{
    public static FillResult Fill(
        PixelDocument document,
        int startX,
        int startY,
        PixelColor color)
    {
        ArgumentNullException.ThrowIfNull(document);

        var targetColor = document.GetPixel(startX, startY);

        if (targetColor == color)
        {
            return new FillResult([], targetColor, color, 0);
        }

        var pendingSeeds = new Stack<int>();
        pendingSeeds.Push((startY * document.Width) + startX);
        var filledSpans = new List<PixelSpan>();
        var filledPixelCount = 0;

        // Filling horizontal spans keeps the pending stack small on large solid regions.
        while (pendingSeeds.Count > 0)
        {
            var seed = pendingSeeds.Pop();
            var y = seed / document.Width;
            var x = seed - (y * document.Width);

            if (document.GetPixel(x, y) != targetColor)
            {
                continue;
            }

            var left = x;

            while (left > 0 && document.GetPixel(left - 1, y) == targetColor)
            {
                left--;
            }

            var right = left;

            while (right < document.Width && document.GetPixel(right, y) == targetColor)
            {
                right++;
            }

            var span = new PixelSpan(left, y, right - left);
            document.SetPixelSpanWithoutNotification(span, color);
            filledSpans.Add(span);
            filledPixelCount += span.Length;

            var hasSeedAbove = false;
            var hasSeedBelow = false;

            for (var fillX = left; fillX < right; fillX++)
            {
                AddAdjacentSeed(
                    document,
                    pendingSeeds,
                    fillX,
                    y - 1,
                    targetColor,
                    ref hasSeedAbove);
                AddAdjacentSeed(
                    document,
                    pendingSeeds,
                    fillX,
                    y + 1,
                    targetColor,
                    ref hasSeedBelow);
            }
        }

        var spans = filledSpans.ToArray();
        document.NotifyPixelSpansChanged(spans, color);
        return new FillResult(spans, targetColor, color, filledPixelCount);
    }

    private static void AddAdjacentSeed(
        PixelDocument document,
        Stack<int> pendingSeeds,
        int x,
        int y,
        PixelColor targetColor,
        ref bool hasSeed)
    {
        if ((uint)y >= (uint)document.Height || document.GetPixel(x, y) != targetColor)
        {
            hasSeed = false;
            return;
        }

        if (hasSeed)
        {
            return;
        }

        pendingSeeds.Push((y * document.Width) + x);
        hasSeed = true;
    }
}
