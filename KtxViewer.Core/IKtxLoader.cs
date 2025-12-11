namespace KtxViewer.Core;

public interface IKtxLoader
{
    Task<KtxImage> LoadAsync(string filePath, CancellationToken cancellationToken = default, IProgress<double>? progress = null);
    Task<KtxImage> LoadAsync(Stream stream, CancellationToken cancellationToken = default, IProgress<double>? progress = null);
}
