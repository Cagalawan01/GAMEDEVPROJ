Add-Type -AssemblyName System.Drawing

$bg = [System.Drawing.Bitmap]::FromFile("Assets/Art/cherry_blossom_school.png")
$prop = [System.Drawing.Bitmap]::FromFile("Assets/Resources/tarp_lanterns.png")

function Test-Pos($bg, $prop, $normX, $normYBottom, $normW, $name) {
    $canvas = New-Object System.Drawing.Bitmap($bg)
    $g = [System.Drawing.Graphics]::FromImage($canvas)
    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality

    $w = [int]($normW * $bg.Width)
    $h = [int](($prop.Height * 1.0 / $prop.Width) * $w)
    $x = [int]($normX * $bg.Width)
    $y = [int]($bg.Height - ($normYBottom * $bg.Height) - $h)

    $g.DrawImage($prop, $x, $y, $w, $h)
    $g.Dispose()

    # Crop to match media_1792448781248_95bd41f0.png region:
    $crop = $canvas.Clone([System.Drawing.Rectangle]::FromLTRB(480, 360, 800, 630), $canvas.PixelFormat)
    $crop.Save("C:/Users/jeffc/.gemini/antigravity/brain/ba6e8cb8-bda8-472f-800f-bc09d20109fa/$name.png", [System.Drawing.Imaging.ImageFormat]::Png)
    $crop.Dispose()
    $canvas.Dispose()
    Write-Host ("Created " + $name + " at X=" + $normX + ", Y=" + $normYBottom + ", W=" + $normW)
}

Test-Pos $bg $prop 0.405 0.285 0.080 "wall_tuck_1"
Test-Pos $bg $prop 0.415 0.275 0.082 "wall_tuck_2"
Test-Pos $bg $prop 0.420 0.265 0.085 "wall_tuck_3"
Test-Pos $bg $prop 0.430 0.255 0.088 "wall_tuck_4"

$bg.Dispose()
$prop.Dispose()
