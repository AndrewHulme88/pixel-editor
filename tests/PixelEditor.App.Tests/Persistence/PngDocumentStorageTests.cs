using pixel_editor.Persistence;
using PixelEditor.Core.Documents;
using Xunit;

namespace PixelEditor.App.Tests.Persistence;

public sealed class PngDocumentStorageTests
{
    [Fact]
    public void SaveToPath_AtomicallyReplacesPngAndPreservesPixels()
    {
        var directory = Directory.CreateTempSubdirectory("pixel-editor-png-");
        var targetPath = Path.Combine(directory.FullName, "art.png");
        var document = new PixelDocument(2, 1);
        document.SetPixel(0, 0, new PixelColor(10, 20, 30, 40));
        document.SetPixel(1, 0, new PixelColor(200, 150, 100, 255));

        try
        {
            File.WriteAllBytes(targetPath, "previous contents"u8.ToArray());

            PngDocumentStorage.SaveToPath(document, targetPath);

            using var input = File.OpenRead(targetPath);
            var loaded = PngDocumentCodec.Load(input);
            Assert.Equal(document.GetPixel(0, 0), loaded.GetPixel(0, 0));
            Assert.Equal(document.GetPixel(1, 0), loaded.GetPixel(1, 0));
            Assert.Empty(Directory.GetFiles(directory.FullName, ".art.png.*.tmp"));
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }
}
