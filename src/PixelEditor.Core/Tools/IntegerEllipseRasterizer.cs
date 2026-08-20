namespace PixelEditor.Core.Tools;

internal static class IntegerEllipseRasterizer
{
    public static void Rasterize(
        int left,
        int top,
        int right,
        int bottom,
        Action<int, int> includePoint)
    {
        // Integer error terms keep the outline connected without floating-point drift.
        long x0 = left;
        long y0 = top;
        long x1 = right;
        long y1 = bottom;
        var horizontalDiameter = x1 - x0;
        var verticalDiameter = y1 - y0;
        var verticalParity = verticalDiameter & 1;
        var horizontalSquared = horizontalDiameter * horizontalDiameter;
        var verticalSquared = verticalDiameter * verticalDiameter;
        var horizontalDelta = 4 * (1 - horizontalDiameter) * verticalSquared;
        var verticalDelta = 4 * (verticalParity + 1) * horizontalSquared;
        var error = horizontalDelta + verticalDelta + (verticalParity * horizontalSquared);

        y0 += (verticalDiameter + 1) / 2;
        y1 = y0 - verticalParity;
        horizontalSquared *= 8;
        verticalSquared *= 8;

        do
        {
            includePoint((int)x1, (int)y0);
            includePoint((int)x0, (int)y0);
            includePoint((int)x0, (int)y1);
            includePoint((int)x1, (int)y1);

            var doubledError = 2 * error;

            if (doubledError <= verticalDelta)
            {
                y0++;
                y1--;
                error += verticalDelta += horizontalSquared;
            }

            if (doubledError >= horizontalDelta || (2 * error) > verticalDelta)
            {
                x0++;
                x1--;
                error += horizontalDelta += verticalSquared;
            }
        }
        while (x0 <= x1);

        while (y0 - y1 < verticalDiameter)
        {
            includePoint((int)(x0 - 1), (int)y0);
            includePoint((int)(x1 + 1), (int)y0);
            y0++;
            includePoint((int)(x0 - 1), (int)y1);
            includePoint((int)(x1 + 1), (int)y1);
            y1--;
        }
    }
}
