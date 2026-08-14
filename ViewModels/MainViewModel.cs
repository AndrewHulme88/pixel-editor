using PixelEditor.Core.Documents;

namespace pixel_editor.ViewModels;

public class MainViewModel : ViewModelBase
{
    public MainViewModel()
    {
        Document = CreateSampleDocument();
    }

    public PixelDocument Document { get; }

    private static PixelDocument CreateSampleDocument()
    {
        var document = new PixelDocument(16, 16);
        var yellow = new PixelColor(245, 196, 66);
        var dark = new PixelColor(55, 46, 40);
        var red = new PixelColor(211, 72, 65);

        for (var y = 3; y <= 12; y++)
        {
            for (var x = 3; x <= 12; x++)
            {
                var isCorner = (x is 3 or 12) && (y is 3 or 12);
                if (!isCorner)
                {
                    document.SetPixel(x, y, yellow);
                }
            }
        }

        document.SetPixel(6, 6, dark);
        document.SetPixel(9, 6, dark);

        for (var x = 6; x <= 9; x++)
        {
            document.SetPixel(x, 10, red);
        }

        document.SetPixel(5, 9, red);
        document.SetPixel(10, 9, red);

        return document;
    }
}
