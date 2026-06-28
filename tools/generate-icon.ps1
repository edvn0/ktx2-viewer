#Requires -Version 5.1
<#
.SYNOPSIS
    Generates the application icon (icon.ico) in a Liquid Glass style with a red tint.

    The mark is a glossy red rounded-glass tile (vertical red gradient + specular top
    highlight + hairline border) with a minimalist white "image" glyph (sun + mountains),
    rendered at multiple sizes and packed into a single multi-resolution .ico.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File tools\generate-icon.ps1
#>
param(
    [string]$OutputPath = (Join-Path $PSScriptRoot '..\KtxViewer.UI\KtxViewer.UI\icon.ico')
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

function New-RoundedRectPath {
    param([float]$x, [float]$y, [float]$w, [float]$h, [float]$r)
    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $d = $r * 2
    $path.AddArc($x, $y, $d, $d, 180, 90)
    $path.AddArc($x + $w - $d, $y, $d, $d, 270, 90)
    $path.AddArc($x + $w - $d, $y + $h - $d, $d, $d, 0, 90)
    $path.AddArc($x, $y + $h - $d, $d, $d, 90, 90)
    $path.CloseFigure()
    return $path
}

function New-IconBitmap {
    param([int]$size)

    $bmp = New-Object System.Drawing.Bitmap($size, $size, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $g.Clear([System.Drawing.Color]::Transparent)

    $s = [float]$size
    $pad = $s * 0.05
    $tileX = $pad
    $tileY = $pad
    $tileW = $s - 2 * $pad
    $tileH = $s - 2 * $pad
    $radius = $tileW * 0.24

    # Soft drop shadow (a few stacked translucent rounded rects below the tile)
    for ($i = 3; $i -ge 1; $i--) {
        $grow = $s * 0.008 * $i
        $offY = $s * 0.012
        $shadow = New-RoundedRectPath ($tileX - $grow) ($tileY - $grow + $offY) ($tileW + 2 * $grow) ($tileH + 2 * $grow) ($radius + $grow)
        $alpha = [int](26 / $i)
        $sb = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb($alpha, 0, 0, 0))
        $g.FillPath($sb, $shadow)
        $sb.Dispose(); $shadow.Dispose()
    }

    $tile = New-RoundedRectPath $tileX $tileY $tileW $tileH $radius

    # Red glass base: bright crimson at top -> deep red at bottom
    $rectF = New-Object System.Drawing.RectangleF($tileX, $tileY, $tileW, $tileH)
    $topColor = [System.Drawing.Color]::FromArgb(255, 232, 58, 72)
    $bottomColor = [System.Drawing.Color]::FromArgb(255, 138, 12, 28)
    $grad = New-Object System.Drawing.Drawing2D.LinearGradientBrush($rectF, $topColor, $bottomColor, 90.0)
    $g.FillPath($grad, $tile)
    $grad.Dispose()

    # Specular top highlight (lensing): white fading from top to ~half height
    $oldClip = $g.Clip
    $g.SetClip($tile)
    $hlRect = New-Object System.Drawing.RectangleF($tileX, $tileY, $tileW, $tileH * 0.55)
    $hlGrad = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
        $hlRect,
        [System.Drawing.Color]::FromArgb(150, 255, 255, 255),
        [System.Drawing.Color]::FromArgb(0, 255, 255, 255),
        90.0)
    $g.FillRectangle($hlGrad, $hlRect)
    $hlGrad.Dispose()
    $g.Clip = $oldClip

    # --- Eye housing: dark rounded panel (keeps the original icon's form) ---
    $hX = $tileX + $tileW * 0.09
    $hY = $tileY + $tileH * 0.09
    $hW = $tileW * 0.82
    $hH = $tileH * 0.54
    $hR = $tileW * 0.15
    $housing = New-RoundedRectPath $hX $hY $hW $hH $hR
    $hBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 30, 20, 26))
    $g.FillPath($hBrush, $housing)
    $hBrush.Dispose()

    # --- Eye (white almond) - large for legibility at small sizes ---
    $ew = $hW * 0.84
    $eh = $hH * 0.66
    $ecx = $hX + $hW * 0.5
    $ecy = $hY + $hH * 0.5
    $ex = $ecx - $ew / 2
    $ey = $ecy - $eh / 2
    $eye = New-Object System.Drawing.Drawing2D.GraphicsPath
    $eye.AddBezier(
        $ex, $ecy,
        ($ex + $ew * 0.25), $ey,
        ($ex + $ew * 0.75), $ey,
        ($ex + $ew), $ecy)
    $eye.AddBezier(
        ($ex + $ew), $ecy,
        ($ex + $ew * 0.75), ($ey + $eh),
        ($ex + $ew * 0.25), ($ey + $eh),
        $ex, $ecy)
    $eye.CloseFigure()
    $white = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(245, 255, 255, 255))
    $g.FillPath($white, $eye)
    $white.Dispose()

    # Iris (red, on-theme), pupil and a small specular catchlight
    $irisR = $eh * 0.48
    $iris = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 214, 40, 58))
    $g.FillEllipse($iris, ($ecx - $irisR), ($ecy - $irisR), ($irisR * 2), ($irisR * 2))
    $iris.Dispose()
    $pupilR = $eh * 0.23
    $pupil = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 36, 10, 16))
    $g.FillEllipse($pupil, ($ecx - $pupilR), ($ecy - $pupilR), ($pupilR * 2), ($pupilR * 2))
    $pupil.Dispose()
    $clR = $eh * 0.11
    $clBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(235, 255, 255, 255))
    $g.FillEllipse($clBrush, ($ecx - $irisR * 0.35 - $clR), ($ecy - $irisR * 0.4 - $clR), ($clR * 2), ($clR * 2))
    $clBrush.Dispose()
    $eye.Dispose()

    # --- "KTX2" caption ---
    $fontPx = [float]($tileH * 0.17)
    $font = New-Object System.Drawing.Font('Segoe UI', $fontPx, [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
    $fmt = New-Object System.Drawing.StringFormat
    $fmt.Alignment = [System.Drawing.StringAlignment]::Center
    $fmt.LineAlignment = [System.Drawing.StringAlignment]::Center
    $g.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAlias
    $capRect = New-Object System.Drawing.RectangleF($tileX, ($hY + $hH - $tileH * 0.01), $tileW, (($tileY + $tileH) - ($hY + $hH) - $tileH * 0.02))
    $textBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(245, 255, 255, 255))
    $g.DrawString('KTX2', $font, $textBrush, $capRect, $fmt)
    $textBrush.Dispose(); $font.Dispose(); $fmt.Dispose(); $housing.Dispose()

    # Hairline glass border
    $penW = [Math]::Max(1.0, $s / 64.0)
    $pen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(140, 255, 255, 255)), $penW
    $g.DrawPath($pen, $tile)
    $pen.Dispose()

    $tile.Dispose()
    $g.Dispose()
    return $bmp
}

# Render each size and collect PNG bytes
$sizes = @(16, 24, 32, 48, 64, 128, 256)
$pngs = @()
foreach ($sz in $sizes) {
    $bmp = New-IconBitmap -size $sz
    $ms = New-Object System.IO.MemoryStream
    $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    $pngs += , ([pscustomobject]@{ Size = $sz; Bytes = $ms.ToArray() })
    $ms.Dispose(); $bmp.Dispose()
}

# Assemble a PNG-based .ico (ICONDIR + ICONDIRENTRY[] + PNG blobs)
$out = New-Object System.IO.MemoryStream
$bw = New-Object System.IO.BinaryWriter($out)

$bw.Write([uint16]0)            # reserved
$bw.Write([uint16]1)            # type = icon
$bw.Write([uint16]$pngs.Count)  # image count

$offset = 6 + 16 * $pngs.Count
foreach ($p in $pngs) {
    $dim = if ($p.Size -ge 256) { 0 } else { $p.Size }
    $bw.Write([byte]$dim)        # width
    $bw.Write([byte]$dim)        # height
    $bw.Write([byte]0)           # palette count
    $bw.Write([byte]0)           # reserved
    $bw.Write([uint16]1)         # color planes
    $bw.Write([uint16]32)        # bits per pixel
    $bw.Write([uint32]$p.Bytes.Length)
    $bw.Write([uint32]$offset)
    $offset += $p.Bytes.Length
}
foreach ($p in $pngs) { $bw.Write($p.Bytes) }

$bw.Flush()
$resolved = [System.IO.Path]::GetFullPath($OutputPath)
[System.IO.File]::WriteAllBytes($resolved, $out.ToArray())
$bw.Dispose(); $out.Dispose()

Write-Host "Icon written: $resolved ($([math]::Round((Get-Item $resolved).Length/1KB,1)) KB, sizes: $($sizes -join ', '))"
