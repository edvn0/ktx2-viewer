using KtxViewer.Core;
using KtxViewer.Infrastructure;

namespace KtxViewer.Tests;

public sealed class Ktx1LoaderTests
{
    [Fact]
    public async Task LoadAsync_WithValidKtx1Header_ParsesCorrectly()
    {
        // Arrange - Create minimal valid KTX1 header
        var ktx1Data = new List<byte>();

        // Identifier: «KTX 11»\r\n\x1A\n
        ktx1Data.AddRange(new byte[] { 0xAB, 0x4B, 0x54, 0x58, 0x20, 0x31, 0x31, 0xBB, 0x0D, 0x0A, 0x1A, 0x0A });

        // Endianness (0x04030201 = little-endian)
        ktx1Data.AddRange(BitConverter.GetBytes(0x04030201u));

        // glType (0x1401 = GL_UNSIGNED_BYTE)
        ktx1Data.AddRange(BitConverter.GetBytes(0x1401u));

        // glTypeSize (1)
        ktx1Data.AddRange(BitConverter.GetBytes(1u));

        // glFormat (0x1908 = GL_RGBA)
        ktx1Data.AddRange(BitConverter.GetBytes(0x1908u));

        // glInternalFormat (0x8058 = GL_RGBA8)
        ktx1Data.AddRange(BitConverter.GetBytes(0x8058u));

        // glBaseInternalFormat (0x1908 = GL_RGBA)
        ktx1Data.AddRange(BitConverter.GetBytes(0x1908u));

        // pixelWidth (16)
        ktx1Data.AddRange(BitConverter.GetBytes(16u));

        // pixelHeight (16)
        ktx1Data.AddRange(BitConverter.GetBytes(16u));

        // pixelDepth (0)
        ktx1Data.AddRange(BitConverter.GetBytes(0u));

        // numberOfArrayElements (0)
        ktx1Data.AddRange(BitConverter.GetBytes(0u));

        // numberOfFaces (1)
        ktx1Data.AddRange(BitConverter.GetBytes(1u));

        // numberOfMipmapLevels (1)
        ktx1Data.AddRange(BitConverter.GetBytes(1u));

        // bytesOfKeyValueData (0)
        ktx1Data.AddRange(BitConverter.GetBytes(0u));

        // imageSize (16*16*4 = 1024)
        ktx1Data.AddRange(BitConverter.GetBytes(1024u));

        // Pixel data (16x16 RGBA)
        var pixelData = new byte[1024];
        for (int i = 0; i < 1024; i += 4)
        {
            pixelData[i] = 255;     // R
            pixelData[i + 1] = 128; // G
            pixelData[i + 2] = 64;  // B
            pixelData[i + 3] = 255; // A
        }
        ktx1Data.AddRange(pixelData);

        var loader = new Ktx1Loader();
        using var stream = new MemoryStream(ktx1Data.ToArray());

        // Act
        var image = await loader.LoadAsync(stream);

        // Assert
        Assert.NotNull(image);
        Assert.Equal(16, image.Width);
        Assert.Equal(16, image.Height);
        Assert.Equal(TextureFormat.RGBA8, image.Format);
        Assert.Equal(1, image.MipLevels);
        Assert.Equal(1024, image.PixelData.Length);
    }

    [Fact]
    public async Task LoadAsync_WithInvalidIdentifier_ThrowsException()
    {
        // Arrange
        var invalidData = new byte[64];
        var loader = new Ktx1Loader();
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
        var loader = new Ktx1Loader();
        using var stream = new MemoryStream(smallData);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidDataException>(
            async () => await loader.LoadAsync(stream));
    }
}
