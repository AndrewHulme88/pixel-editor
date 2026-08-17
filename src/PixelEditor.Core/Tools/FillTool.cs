using PixelEditor.Core.Documents;

namespace PixelEditor.Core.Tools;

public static class FillTool
{
    public static int Fill(
        PixelDocument document,
        int startX,
        int startY,
        PixelColor color)
    {
        ArgumentNullException.ThrowIfNull(document);

        var targetColor = document.GetPixel(startX, startY);

        if (targetColor == color)
        {
            return 0;
        }

        var pendingSeeds = new Stack<int>();
        pendingSeeds.Push((startY * document.Width) + startX);
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

            var hasSeedAbove = false;
            var hasSeedBelow = false;

            for (var fillX = left;
                 fillX < document.Width && document.GetPixel(fillX, y) == targetColor;
                 fillX++)
            {
                document.SetPixel(fillX, y, color);
                filledPixelCount++;

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

        return filledPixelCount;
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
