using KtxViewer.Core;
using KtxViewer.Infrastructure;

namespace KtxViewer.Tests;

public sealed class CompositeKtxLoaderTests
{
    [Fact]
    public async Task LoadAsync_WithKtx1Identifier_UsesKtx1Loader()
    {
        // Arrange - Create minimal valid KTX1 header
        var ktx1Data = new List<byte>();
        ktx1Data.AddRange(new byte[] { 0xAB, 0x4B, 0x54, 0x58, 0x20, 0x31, 0x31, 0xBB, 0x0D, 0x0A, 0x1A, 0x0A });
        ktx1Data.AddRange(BitConverter.GetBytes(0x04030201u)); // endianness
        ktx1Data.AddRange(BitConverter.GetBytes(0x1401u)); // glType
        ktx1Data.AddRange(BitConverter.GetBytes(1u)); // glTypeSize
        ktx1Data.AddRange(BitConverter.GetBytes(0x1908u)); // glFormat
        ktx1Data.AddRange(BitConverter.GetBytes(0x8058u)); // glInternalFormat
        ktx1Data.AddRange(BitConverter.GetBytes(0x1908u)); // glBaseInternalFormat
        ktx1Data.AddRange(BitConverter.GetBytes(16u)); // width
        ktx1Data.AddRange(BitConverter.GetBytes(16u)); // height
        ktx1Data.AddRange(BitConverter.GetBytes(0u)); // depth
        ktx1Data.AddRange(BitConverter.GetBytes(0u)); // arrayElements
        ktx1Data.AddRange(BitConverter.GetBytes(1u)); // faces
        ktx1Data.AddRange(BitConverter.GetBytes(1u)); // mipLevels
        ktx1Data.AddRange(BitConverter.GetBytes(0u)); // bytesOfKeyValueData
        ktx1Data.AddRange(BitConverter.GetBytes(1024u)); // imageSize
        ktx1Data.AddRange(new byte[1024]); // pixel data

        var loader = new CompositeKtxLoader();
        using var stream = new MemoryStream(ktx1Data.ToArray());

        // Act
        var image = await loader.LoadAsync(stream);

        // Assert
        Assert.NotNull(image);
        Assert.Equal(16, image.Width);
        Assert.Equal(16, image.Height);
    }

    [Fact]
    public async Task LoadAsync_WithKtx2File_UsesKtx2Loader()
    {
        // Arrange
        var loader = new CompositeKtxLoader();
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
    }

    [Fact]
    public async Task LoadAsync_WithInvalidIdentifier_ThrowsException()
    {
        // Arrange
        var invalidData = new byte[64];
        invalidData[0] = 0xFF; // Invalid identifier

        var loader = new CompositeKtxLoader();
        using var stream = new MemoryStream(invalidData);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidDataException>(
            async () => await loader.LoadAsync(stream));
    }

    [Fact]
    public async Task LoadAsync_WithTooSmallFile_ThrowsException()
    {
        // Arrange
        var smallData = new byte[8];
        var loader = new CompositeKtxLoader();
        using var stream = new MemoryStream(smallData);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidDataException>(
            async () => await loader.LoadAsync(stream));
    }
}
