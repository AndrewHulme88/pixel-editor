namespace PixelEditor.Core.Documents;

public static class PixelDocumentLimits
{
    public const int MinimumDimension = 1;
    public const int MaximumDimension = 4096;

    public static bool AreDimensionsSupported(int width, int height) =>
        IsDimensionSupported(width) && IsDimensionSupported(height);

    public static void ValidateDimensions(int width, int height)
    {
        ValidateDimension(width, nameof(width), "Width");
        ValidateDimension(height, nameof(height), "Height");
    }

    private static bool IsDimensionSupported(int dimension) =>
        dimension is >= MinimumDimension and <= MaximumDimension;

    private static void ValidateDimension(
        int dimension,
        string parameterName,
        string displayName)
    {
        if (!IsDimensionSupported(dimension))
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                dimension,
                $"{displayName} must be between {MinimumDimension} and {MaximumDimension} pixels.");
        }
    }
}
