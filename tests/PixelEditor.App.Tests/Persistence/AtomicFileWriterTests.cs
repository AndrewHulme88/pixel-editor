using pixel_editor.Persistence;
using Xunit;

namespace PixelEditor.App.Tests.Persistence;

public sealed class AtomicFileWriterTests
{
    [Fact]
    public void Write_ReplacesExistingFileAndRemovesTemporaryFile()
    {
        var directory = Directory.CreateTempSubdirectory("pixel-editor-atomic-");
        var targetPath = Path.Combine(directory.FullName, "art.png");

        try
        {
            File.WriteAllBytes(targetPath, "original"u8.ToArray());

            AtomicFileWriter.Write(targetPath, output =>
                output.Write("replacement"u8));

            Assert.Equal("replacement"u8.ToArray(), File.ReadAllBytes(targetPath));
            Assert.Empty(Directory.GetFiles(directory.FullName, ".art.png.*.tmp"));
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    [Fact]
    public void Write_WhenEncodingFails_PreservesExistingFileAndRemovesTemporaryFile()
    {
        var directory = Directory.CreateTempSubdirectory("pixel-editor-atomic-");
        var targetPath = Path.Combine(directory.FullName, "art.png");
        var original = "original"u8.ToArray();

        try
        {
            File.WriteAllBytes(targetPath, original);

            Assert.Throws<InvalidOperationException>(() =>
                AtomicFileWriter.Write(targetPath, output =>
                {
                    output.Write("partial"u8);
                    throw new InvalidOperationException("Encoding failed.");
                }));

            Assert.Equal(original, File.ReadAllBytes(targetPath));
            Assert.Empty(Directory.GetFiles(directory.FullName, ".art.png.*.tmp"));
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    [Fact]
    public void Write_CreatesNewFile()
    {
        var directory = Directory.CreateTempSubdirectory("pixel-editor-atomic-");
        var targetPath = Path.Combine(directory.FullName, "art.png");

        try
        {
            AtomicFileWriter.Write(targetPath, output => output.Write("new"u8));

            Assert.Equal("new"u8.ToArray(), File.ReadAllBytes(targetPath));
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }
}
