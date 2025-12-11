using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KtxViewer.Application;
using KtxViewer.Core;
using Microsoft.Win32;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace KtxViewer.UI.ViewModels;

public sealed partial class MainViewModel : ObservableObject
{
    private readonly LoadImageUseCase _loadImageUseCase;

    [ObservableProperty]
    private ImageSource? _currentImage;

    [ObservableProperty]
    private string? _imageInfo;

    [ObservableProperty]
    private string? _fileInfo;

    [ObservableProperty]
    private string? _detailedInfo;

    [ObservableProperty]
    private bool _isInfoVisible;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private double _loadProgress;

    [ObservableProperty]
    private double _zoomLevel = 1.0;

    public MainViewModel(LoadImageUseCase loadImageUseCase)
    {
        _loadImageUseCase = loadImageUseCase ?? throw new ArgumentNullException(nameof(loadImageUseCase));
    }

    public void AdjustZoom(double delta)
    {
        var newZoom = ZoomLevel + delta;
        ZoomLevel = Math.Clamp(newZoom, 0.1, 10.0);
    }

    [RelayCommand]
    private void ResetZoom()
    {
        ZoomLevel = 1.0;
    }

    [RelayCommand]
    private void ToggleInfo()
    {
        IsInfoVisible = !IsInfoVisible;
    }

    public async Task LoadFileAsync(string filePath)
    {
        if (!File.Exists(filePath))
        {
            MessageBox.Show($"File not found: {filePath}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        try
        {
            IsLoading = true;
            LoadProgress = 0;

            var progress = new Progress<double>(value =>
            {
                LoadProgress = Math.Min(90, value * 0.9);
            });

            var image = await _loadImageUseCase.ExecuteAsync(filePath, default, progress);

            LoadProgress = 95;
            await Task.Yield();

            CurrentImage = ConvertToBitmap(image);
            LoadProgress = 100;

            ImageInfo = $"{image.Width}x{image.Height} | {image.Format} | {image.MipLevels} mip(s) | {image.LayerCount} layer(s)";

            var fileInfo = new FileInfo(filePath);
            var fileSizeKb = fileInfo.Length / 1024.0;
            var fileSizeMb = fileSizeKb / 1024.0;
            var sizeStr = fileSizeMb >= 1 ? $"{fileSizeMb:F2} MB" : $"{fileSizeKb:F2} KB";
            FileInfo = $"{fileInfo.Name} | {sizeStr}";

            if (image.Metadata != null)
            {
                var md = image.Metadata;
                var sb = new System.Text.StringBuilder();
                sb.AppendLine($"Resolution: {md.PixelWidth}x{md.PixelHeight}x{md.PixelDepth}");

                if (md.VkFormat > 0)
                {
                    sb.AppendLine($"Format: {md.VkFormat} (Vulkan ID)");
                }

                sb.AppendLine($"Type Size: {md.TypeSize}");
                sb.AppendLine($"Levels: {md.LevelCount}");
                sb.AppendLine($"Layers: {md.LayerCount}");
                sb.AppendLine($"Faces: {md.FaceCount}");

                if (md.SupercompressionScheme > 0)
                {
                    sb.AppendLine($"Supercompression: {GetSupercompressionName(md.SupercompressionScheme)}");
                }

                sb.AppendLine($"Color Model: {md.ColorModel}");
                sb.AppendLine($"Color Primaries: {md.ColorPrimaries}");
                sb.AppendLine($"Transfer Function: {md.TransferFunction}");

                if (md.KeyValuePairs.Count > 0)
                {
                    sb.AppendLine("\nMetadata:");
                    foreach (var kvp in md.KeyValuePairs)
                    {
                        sb.AppendLine($"  {kvp.Key}: {kvp.Value}");
                    }
                }

                DetailedInfo = sb.ToString();
            }
            else
            {
                DetailedInfo = "No metadata available";
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading texture: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task OpenFileAsync()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "KTX Files (*.ktx;*.ktx2)|*.ktx;*.ktx2|KTX2 Files (*.ktx2)|*.ktx2|KTX Files (*.ktx)|*.ktx|All Files (*.*)|*.*",
            Title = "Open KTX Texture"
        };

        if (dialog.ShowDialog() == true)
        {
            await LoadFileAsync(dialog.FileName);
        }
    }

    private string GetSupercompressionName(uint scheme)
    {
        return scheme switch
        {
            0 => "None",
            1 => "BasisLZ",
            2 => "Zstandard",
            3 => "ZLIB",
            _ => $"Unknown ({scheme})"
        };
    }

    private static BitmapSource ConvertToBitmap(KtxImage image)
    {
        var stride = image.Width * 4;
        var bitmap = BitmapSource.Create(
            image.Width,
            image.Height,
            96, 96,
            PixelFormats.Bgra32,
            null,
            ConvertRgbaToBgra(image.PixelData.Span),
            stride);

        bitmap.Freeze();
        return bitmap;
    }

    private static byte[] ConvertRgbaToBgra(ReadOnlySpan<byte> rgba)
    {
        var bgra = new byte[rgba.Length];
        for (int i = 0; i < rgba.Length; i += 4)
        {
            bgra[i] = rgba[i + 2];     // B
            bgra[i + 1] = rgba[i + 1]; // G
            bgra[i + 2] = rgba[i];     // R
            bgra[i + 3] = rgba[i + 3]; // A
        }
        return bgra;
    }
}
