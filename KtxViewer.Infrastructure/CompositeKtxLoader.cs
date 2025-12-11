using KtxViewer.Core;

namespace KtxViewer.Infrastructure;

public sealed class CompositeKtxLoader : IKtxLoader
{
    private readonly Ktx1Loader _ktx1Loader;
    private readonly Ktx2Loader _ktx2Loader;

    public CompositeKtxLoader()
    {
        _ktx1Loader = new Ktx1Loader();
        _ktx2Loader = new Ktx2Loader();
    }

    public async Task<KtxImage> LoadAsync(string filePath, CancellationToken cancellationToken = default)
    {
        await using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true);
        return await LoadAsync(stream, cancellationToken);
    }

    public async Task<KtxImage> LoadAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        var identifier = new byte[12];
        var bytesRead = await stream.ReadAsync(identifier, cancellationToken);

        if (bytesRead < 12)
        {
            throw new InvalidDataException($"File too small. Expected at least 12 bytes, got {bytesRead}");
        }

        stream.Seek(0, SeekOrigin.Begin);

        // KTX2: «KTX 20»
        if (identifier[0] == 0xAB &&
            identifier[1] == 0x4B &&
            identifier[2] == 0x54 &&
            identifier[3] == 0x58 &&
            identifier[4] == 0x20 &&
            identifier[5] == 0x32 &&
            identifier[6] == 0x30 &&
            identifier[7] == 0xBB)
        {
            return await _ktx2Loader.LoadAsync(stream, cancellationToken);
        }

        // KTX1: «KTX 11»
        if (identifier[0] == 0xAB &&
            identifier[1] == 0x4B &&
            identifier[2] == 0x54 &&
            identifier[3] == 0x58 &&
            identifier[4] == 0x20 &&
            identifier[5] == 0x31 &&
            identifier[6] == 0x31 &&
            identifier[7] == 0xBB)
        {
            return await _ktx1Loader.LoadAsync(stream, cancellationToken);
        }

        throw new InvalidDataException($"Unknown file format. Expected KTX1 or KTX2 identifier, got: {BitConverter.ToString(identifier)}");
    }
}
