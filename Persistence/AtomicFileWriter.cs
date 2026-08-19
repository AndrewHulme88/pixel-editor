using System;
using System.IO;

namespace pixel_editor.Persistence;

internal static class AtomicFileWriter
{
    public static void Write(string targetPath, Action<Stream> write)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(targetPath);
        ArgumentNullException.ThrowIfNull(write);

        var fullTargetPath = Path.GetFullPath(targetPath);
        var directory = Path.GetDirectoryName(fullTargetPath)
            ?? throw new ArgumentException("The target must have a parent directory.", nameof(targetPath));
        var temporaryPath = Path.Combine(
            directory,
            $".{Path.GetFileName(fullTargetPath)}.{Guid.NewGuid():N}.tmp");

        try
        {
            using (var output = new FileStream(
                temporaryPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 4096,
                FileOptions.WriteThrough))
            {
                write(output);
                output.Flush(flushToDisk: true);
            }

            File.Move(temporaryPath, fullTargetPath, overwrite: true);
        }
        catch
        {
            TryDeleteTemporaryFile(temporaryPath);
            throw;
        }
    }

    private static void TryDeleteTemporaryFile(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (Exception exception)
            when (exception is IOException or UnauthorizedAccessException)
        {
            // Cleanup must not hide the original save failure.
        }
    }
}
