namespace KtxViewer.Infrastructure;

internal static class StreamExtensions
{
    public static async Task ReadExactlyWithProgressAsync(
        this Stream stream,
        byte[] buffer,
        long totalSize,
        CancellationToken cancellationToken = default,
        IProgress<double>? progress = null)
    {
        const int chunkSize = 8192; // 8KB chunks
        int totalBytesRead = 0;
        int bytesToRead = buffer.Length;

        while (totalBytesRead < bytesToRead)
        {
            int currentChunkSize = Math.Min(chunkSize, bytesToRead - totalBytesRead);
            int bytesRead = await stream.ReadAsync(buffer.AsMemory(totalBytesRead, currentChunkSize), cancellationToken);

            if (bytesRead == 0)
            {
                throw new EndOfStreamException($"Unexpected end of stream. Expected {bytesToRead} bytes, got {totalBytesRead}");
            }

            totalBytesRead += bytesRead;

            // Report progress
            if (progress != null && totalSize > 0)
            {
                double percentage = Math.Min(100.0, (stream.Position / (double)totalSize) * 100.0);
                progress.Report(percentage);
            }
        }
    }

    public static async Task<byte[]> ReadAllWithProgressAsync(
        this Stream stream,
        long totalSize,
        CancellationToken cancellationToken = default,
        IProgress<double>? progress = null)
    {
        var buffer = new byte[totalSize];
        await stream.ReadExactlyWithProgressAsync(buffer, totalSize, cancellationToken, progress);
        return buffer;
    }
}
