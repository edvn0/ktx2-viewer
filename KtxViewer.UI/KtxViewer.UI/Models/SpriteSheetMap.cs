using System.Collections.Generic;
using System.Text.Json;

namespace KtxViewer.UI.Models;

/// <summary>A single sprite rectangle inside the sheet, in sheet (meta.size) coordinates.</summary>
public sealed class SpriteFrame
{
    public required string Filename { get; init; }
    public required int X { get; init; }
    public required int Y { get; init; }
    public required int W { get; init; }
    public required int H { get; init; }
}

/// <summary>
/// A TexturePacker-style sprite map (JSON array format): a list of frame rectangles plus
/// the packed sheet size from <c>meta.size</c>. Frame coordinates are relative to the sheet size.
/// </summary>
public sealed class SpriteSheetMap
{
    public required IReadOnlyList<SpriteFrame> Frames { get; init; }
    public required int SheetWidth { get; init; }
    public required int SheetHeight { get; init; }
    public string? ImageName { get; init; }

    /// <summary>
    /// Parses and validates a sprite map. On failure returns <c>false</c> and a detailed,
    /// human-readable description of what does not match the expected structure.
    /// </summary>
    public static bool TryParse(string json, out SpriteSheetMap? map, out string error)
    {
        map = null;
        error = string.Empty;

        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(json);
        }
        catch (JsonException ex)
        {
            error = $"The file is not valid JSON.\n{ex.Message}";
            return false;
        }

        using (doc)
        {
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                error = "The root element must be a JSON object containing 'frames' and 'meta'.";
                return false;
            }

            if (!root.TryGetProperty("frames", out var framesEl))
            {
                error = "Missing required property 'frames'. A TexturePacker JSON (array) map is expected.";
                return false;
            }

            if (framesEl.ValueKind != JsonValueKind.Array)
            {
                error = $"'frames' must be an array, but was '{framesEl.ValueKind}'. " +
                        "Only the TexturePacker 'JSON (Array)' format is supported, not 'JSON (Hash)'.";
                return false;
            }

            if (framesEl.GetArrayLength() == 0)
            {
                error = "'frames' array is empty - there are no sprites to display.";
                return false;
            }

            var frames = new List<SpriteFrame>();
            var index = 0;
            foreach (var frameEl in framesEl.EnumerateArray())
            {
                if (frameEl.ValueKind != JsonValueKind.Object)
                {
                    error = $"frames[{index}] must be an object, but was '{frameEl.ValueKind}'.";
                    return false;
                }

                if (!frameEl.TryGetProperty("frame", out var rect) || rect.ValueKind != JsonValueKind.Object)
                {
                    error = $"frames[{index}] is missing the 'frame' object with x, y, w, h.";
                    return false;
                }

                if (!TryGetInt(rect, "x", out var x) ||
                    !TryGetInt(rect, "y", out var y) ||
                    !TryGetInt(rect, "w", out var w) ||
                    !TryGetInt(rect, "h", out var h))
                {
                    error = $"frames[{index}].frame must contain numeric 'x', 'y', 'w' and 'h'.";
                    return false;
                }

                var name = frameEl.TryGetProperty("filename", out var fn) && fn.ValueKind == JsonValueKind.String
                    ? fn.GetString()!
                    : $"frame {index}";

                frames.Add(new SpriteFrame { Filename = name, X = x, Y = y, W = w, H = h });
                index++;
            }

            if (!root.TryGetProperty("meta", out var meta) || meta.ValueKind != JsonValueKind.Object)
            {
                error = "Missing required 'meta' object (it holds the packed sheet size).";
                return false;
            }

            if (!meta.TryGetProperty("size", out var size) || size.ValueKind != JsonValueKind.Object)
            {
                error = "Missing required 'meta.size' object with the packed sheet dimensions (w, h).";
                return false;
            }

            if (!TryGetInt(size, "w", out var sheetW) || !TryGetInt(size, "h", out var sheetH) || sheetW <= 0 || sheetH <= 0)
            {
                error = "'meta.size' must contain positive numeric 'w' and 'h' values.";
                return false;
            }

            var image = meta.TryGetProperty("image", out var im) && im.ValueKind == JsonValueKind.String
                ? im.GetString()
                : null;

            map = new SpriteSheetMap
            {
                Frames = frames,
                SheetWidth = sheetW,
                SheetHeight = sheetH,
                ImageName = image,
            };
            return true;
        }
    }

    private static bool TryGetInt(JsonElement obj, string property, out int value)
    {
        value = 0;
        if (!obj.TryGetProperty(property, out var el))
        {
            return false;
        }

        if (el.ValueKind == JsonValueKind.Number && el.TryGetDouble(out var d))
        {
            value = (int)System.Math.Round(d);
            return true;
        }

        return false;
    }
}
