using KtxViewer.Core;
using KtxViewer.Infrastructure;

namespace KtxViewer.Tests;

public sealed class Ktx2LoaderTests
{
    [Fact]
    public async Task LoadAsync_WithInvalidIdentifier_ThrowsException()
    {
        // Arrange
        var invalidData = new byte[80];
        var loader = new Ktx2Loader();
        using var stream = new MemoryStream(invalidData);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidDataException>(
            async () => await loader.LoadAsync(stream));
    }

    [Fact]
    public async Task LoadAsync_WithTooSmallFile_ThrowsException()
    {
        // Arrange
        var smallData = new byte[32];
        var loader = new Ktx2Loader();
        using var stream = new MemoryStream(smallData);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidDataException>(
            async () => await loader.LoadAsync(stream));
    }

    [Fact]
    public async Task LoadAsync_WithRealKtx2File_LoadsSuccessfully()
    {
        // Arrange
        var loader = new Ktx2Loader();
        var testFilePath = Path.Combine("..", "..", "..", "..", "test.ktx2");

        if (!File.Exists(testFilePath))
        {
            return; // Skip if file doesn't exist
        }

        // Act
        var image = await loader.LoadAsync(testFilePath);

        // Assert
        Assert.NotNull(image);
        Assert.True(image.Width > 0);
        Assert.True(image.Height > 0);
        Assert.NotEqual(TextureFormat.Unknown, image.Format);
        Assert.True(image.PixelData.Length > 0);
    }
}
