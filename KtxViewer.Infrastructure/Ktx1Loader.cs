using KtxViewer.Core;
using KtxViewer.Core.Models;
using System.Buffers.Binary;
using System.Text;

namespace KtxViewer.Infrastructure;

public sealed class Ktx1Loader : IKtxLoader
{
    // KTX 1.0 identifier: «KTX 11»\r\n\x1A\n
    private static ReadOnlySpan<byte> Ktx1Identifier => [0xAB, 0x4B, 0x54, 0x58, 0x20, 0x31, 0x31, 0xBB, 0x0D, 0x0A, 0x1A, 0x0A];

    public async Task<KtxImage> LoadAsync(string filePath, CancellationToken cancellationToken = default)
    {
        await using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true);
        return await LoadAsync(stream, cancellationToken);
    }

    public async Task<KtxImage> LoadAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        var header = new byte[64];
        var bytesRead = await stream.ReadAsync(header, cancellationToken);

        if (bytesRead < 64)
        {
            throw new InvalidDataException($"File too small for KTX1. Expected at least 64 bytes, got {bytesRead}");
        }

        if (!header.AsSpan(0, 12).SequenceEqual(Ktx1Identifier))
        {
            throw new InvalidDataException("Invalid KTX1 file identifier");
        }

        // Endianness marker at offset 12
        var endiannessMarker = BinaryPrimitives.ReadUInt32LittleEndian(header.AsSpan(12));
        bool needsSwap = endiannessMarker == 0x01020304; // Big-endian file on little-endian system

        uint ReadUInt32(ReadOnlySpan<byte> span, int offset)
        {
            var value = BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(offset));
            return needsSwap ? BinaryPrimitives.ReverseEndianness(value) : value;
        }

        var glType = ReadUInt32(header, 16);
        var glTypeSize = ReadUInt32(header, 20);
        var glFormat = ReadUInt32(header, 24);
        var glInternalFormat = ReadUInt32(header, 28);
        var glBaseInternalFormat = ReadUInt32(header, 32);
        var width = ReadUInt32(header, 36);
        var height = ReadUInt32(header, 40);
        var depth = ReadUInt32(header, 44);
        var numberOfArrayElements = ReadUInt32(header, 48);
        var numberOfFaces = ReadUInt32(header, 52);
        var numberOfMipmapLevels = ReadUInt32(header, 56);
        var bytesOfKeyValueData = ReadUInt32(header, 60);

        if (width == 0 || height == 0)
        {
            throw new InvalidDataException("Invalid KTX1 dimensions");
        }

        if (numberOfMipmapLevels == 0) numberOfMipmapLevels = 1;
        if (numberOfFaces == 0) numberOfFaces = 1;
        if (numberOfArrayElements == 0) numberOfArrayElements = 1;
        if (depth == 0) depth = 1;

        // Create metadata
        var metadata = new KtxMetadata
        {
            PixelWidth = width,
            PixelHeight = height,
            PixelDepth = depth,
            LayerCount = numberOfArrayElements,
            FaceCount = numberOfFaces,
            LevelCount = numberOfMipmapLevels,
            VkFormat = 0, // KTX1 uses GL formats, not Vulkan
            TypeSize = glTypeSize,
            SupercompressionScheme = 0,
            ColorModel = GetGlFormatColorModel(glInternalFormat, glFormat),
            ColorPrimaries = "Unspecified",
            TransferFunction = "Unspecified"
        };

        // Parse key/value data
        if (bytesOfKeyValueData > 0)
        {
            var kvdBuffer = new byte[bytesOfKeyValueData];
            await stream.ReadExactlyAsync(kvdBuffer, cancellationToken);
            ParseKeyValueData(metadata, kvdBuffer, needsSwap);
        }

        // Read first mip level
        var imageSize = new byte[4];
        await stream.ReadExactlyAsync(imageSize, cancellationToken);
        var imageDataSize = ReadUInt32(imageSize, 0);

        var imageData = new byte[imageDataSize];
        await stream.ReadExactlyAsync(imageData, cancellationToken);

        var format = MapGlFormat(glInternalFormat, glFormat, glType);
        var pixelData = ConvertToRgba8(imageData, (int)width, (int)height, format, glType, glFormat);

        return new KtxImage
        {
            Width = (int)width,
            Height = (int)height,
            Format = format,
            PixelData = pixelData,
            MipLevels = (int)numberOfMipmapLevels,
            LayerCount = (int)numberOfArrayElements,
            Metadata = metadata
        };
    }

    private static TextureFormat MapGlFormat(uint glInternalFormat, uint glFormat, uint glType)
    {
        // GL_RGBA8 = 0x8058, GL_RGB8 = 0x8051
        // GL_COMPRESSED_RGBA_S3TC_DXT1_EXT = 0x83F1
        // GL_COMPRESSED_RGBA_S3TC_DXT5_EXT = 0x83F3

        return glInternalFormat switch
        {
            0x8058 => TextureFormat.RGBA8,  // GL_RGBA8
            0x8051 => TextureFormat.RGB8,   // GL_RGB8
            0x83F1 => TextureFormat.BC1,    // GL_COMPRESSED_RGBA_S3TC_DXT1_EXT
            0x83F3 => TextureFormat.BC3,    // GL_COMPRESSED_RGBA_S3TC_DXT5_EXT
            _ => glFormat == 0x1908 && glType == 0x1401 ? TextureFormat.RGBA8 : TextureFormat.Unknown
        };
    }

    private static ReadOnlyMemory<byte> ConvertToRgba8(byte[] data, int width, int height, TextureFormat format, uint glType, uint glFormat)
    {
        if (format == TextureFormat.RGBA8)
        {
            return data;
        }

        if (format == TextureFormat.RGB8)
        {
            var rgba = new byte[width * height * 4];
            for (int i = 0, j = 0; i < data.Length; i += 3, j += 4)
            {
                rgba[j] = data[i];
                rgba[j + 1] = data[i + 1];
                rgba[j + 2] = data[i + 2];
                rgba[j + 3] = 255;
            }
            return rgba;
        }

        // Placeholder for compressed formats
        var placeholder = new byte[width * height * 4];
        var color = format switch
        {
            TextureFormat.BC1 => (r: 255, g: 200, b: 150),
            TextureFormat.BC3 => (r: 200, g: 255, b: 150),
            _ => (r: 200, g: 200, b: 200)
        };

        for (int i = 0; i < placeholder.Length; i += 4)
        {
            placeholder[i] = (byte)color.r;
            placeholder[i + 1] = (byte)color.g;
            placeholder[i + 2] = (byte)color.b;
            placeholder[i + 3] = 255;
        }
        return placeholder;
    }

    private void ParseKeyValueData(KtxMetadata metadata, byte[] buffer, bool needsSwap)
    {
        int offset = 0;
        while (offset < buffer.Length)
        {
            if (offset + 4 > buffer.Length) break;

            var keyAndValueByteSize = BinaryPrimitives.ReadUInt32LittleEndian(buffer.AsSpan(offset));
            if (needsSwap) keyAndValueByteSize = BinaryPrimitives.ReverseEndianness(keyAndValueByteSize);
            offset += 4;

            if (offset + keyAndValueByteSize > buffer.Length) break;

            var kvSpan = buffer.AsSpan(offset, (int)keyAndValueByteSize);
            int nullIndex = -1;
            for (int i = 0; i < kvSpan.Length; i++)
            {
                if (kvSpan[i] == 0)
                {
                    nullIndex = i;
                    break;
                }
            }

            if (nullIndex > 0)
            {
                var key = Encoding.UTF8.GetString(kvSpan.Slice(0, nullIndex));
                var valueSpan = kvSpan.Slice(nullIndex + 1);

                if (valueSpan.Length > 0 && valueSpan[valueSpan.Length - 1] == 0)
                {
                    valueSpan = valueSpan.Slice(0, valueSpan.Length - 1);
                }

                string value;
                try
                {
                    value = Encoding.UTF8.GetString(valueSpan);
                }
                catch
                {
                    value = BitConverter.ToString(valueSpan.ToArray()).Replace("-", " ");
                }

                metadata.KeyValuePairs[key] = value;
            }

            offset += (int)keyAndValueByteSize;

            // Padding to 4-byte alignment
            int padding = (4 - ((int)keyAndValueByteSize % 4)) % 4;
            offset += padding;
        }
    }

    private static string GetGlFormatColorModel(uint glInternalFormat, uint glFormat)
    {
        return glInternalFormat switch
        {
            0x8058 => "RGBA",        // GL_RGBA8
            0x8051 => "RGB",         // GL_RGB8
            0x83F1 => "BC1 (DXT1)",  // GL_COMPRESSED_RGBA_S3TC_DXT1_EXT
            0x83F3 => "BC3 (DXT5)",  // GL_COMPRESSED_RGBA_S3TC_DXT5_EXT
            _ => glFormat switch
            {
                0x1908 => "RGBA",    // GL_RGBA
                0x1907 => "RGB",     // GL_RGB
                _ => $"GL Format 0x{glFormat:X}"
            }
        };
    }
}
