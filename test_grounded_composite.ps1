Add-Type -AssemblyName System.Drawing

$bg = [System.Drawing.Bitmap]::FromFile("Assets/Art/cherry_blossom_school.png")
$prop = [System.Drawing.Bitmap]::FromFile("Assets/Resources/tarp_lanterns.png")

function Test-Comp($bg, $prop, $normX, $normYBottom, $normW, $name) {
    $canvas = New-Object System.Drawing.Bitmap($bg)
    $g = [System.Drawing.Graphics]::FromImage($canvas)
    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality

    $w = [int]($normW * $bg.Width)
    $h = [int](($prop.Height * 1.0 / $prop.Width) * $w)
    $x = [int]($normX * $bg.Width)
    $y = [int]($bg.Height - ($normYBottom * $bg.Height) - $h)

    # Draw a soft road asphalt contact shadow under the mat
    $shadowBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(80, 15, 18, 25))
    $g.FillEllipse($shadowBrush, $x + [int]($w * 0.02), $y + $h - [int]($h * 0.05), [int]($w * 0.96), [int]($h * 0.08))

    $g.DrawImage($prop, $x, $y, $w, $h)
    $g.Dispose()

    # Full view
    $canvas.Save("C:/Users/jeffc/.gemini/antigravity/brain/ba6e8cb8-bda8-472f-800f-bc09d20109fa/$name" + "_full.png", [System.Drawing.Imaging.ImageFormat]::Png)

    # Zoomed crop
    $crop = $canvas.Clone([System.Drawing.Rectangle]::FromLTRB(400, 320, 850, 660), $canvas.PixelFormat)
    $crop.Save("C:/Users/jeffc/.gemini/antigravity/brain/ba6e8cb8-bda8-472f-800f-bc09d20109fa/$name" + "_zoom.png", [System.Drawing.Imaging.ImageFormat]::Png)
    $crop.Dispose()
    $canvas.Dispose()
    Write-Host ("Created " + $name + ": X=" + $normX + ", YBottom=" + $normYBottom + ", W=" + $normW + ", H=" + $h)
}

# In 1376x768:
# Test 1: Resting on asphalt at curb, matching the reference image media_1792424141402_d84c4724.jpg
Test-Comp $bg $prop 0.405 0.235 0.096 "grounded_curb_A"
Test-Comp $bg $prop 0.415 0.228 0.098 "grounded_curb_B"
Test-Comp $bg $prop 0.395 0.245 0.092 "grounded_curb_C"

$bg.Dispose()
$prop.Dispose()
