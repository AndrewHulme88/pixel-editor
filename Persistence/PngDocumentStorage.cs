using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using PixelEditor.Core.Documents;

namespace pixel_editor.Persistence;

internal static class PngDocumentStorage
{
    public static async Task SaveAsync(PixelDocument document, IStorageFile file)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(file);

        if (file.TryGetLocalPath() is { } localPath)
        {
            SaveToPath(document, localPath);
            return;
        }

        using var encoded = new MemoryStream();
        PngDocumentCodec.Save(document, encoded);
        encoded.Position = 0;

        await using var output = await file.OpenWriteAsync();

        if (output.CanSeek)
        {
            output.Position = 0;
        }

        await encoded.CopyToAsync(output);

        if (output.CanSeek)
        {
            output.SetLength(output.Position);
        }

        await output.FlushAsync();
    }

    internal static void SaveToPath(PixelDocument document, string path)
    {
        ArgumentNullException.ThrowIfNull(document);
        AtomicFileWriter.Write(path, output => PngDocumentCodec.Save(document, output));
    }
}
