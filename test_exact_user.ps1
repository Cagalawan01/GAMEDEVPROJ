Add-Type -AssemblyName System.Drawing

$bg = [System.Drawing.Bitmap]::FromFile("Assets/Art/cherry_blossom_school.png")
$prop = [System.Drawing.Bitmap]::FromFile("C:/Users/jeffc/.gemini/antigravity/brain/ba6e8cb8-bda8-472f-800f-bc09d20109fa/scratch_tarp_trimmed.png")

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

    $crop = $canvas.Clone([System.Drawing.Rectangle]::FromLTRB(500, 370, 780, 620), $canvas.PixelFormat)
    $crop.Save("C:/Users/jeffc/.gemini/antigravity/brain/ba6e8cb8-bda8-472f-800f-bc09d20109fa/$name.png", [System.Drawing.Imaging.ImageFormat]::Png)
    $crop.Dispose()
    $canvas.Dispose()
    Write-Host ("Created " + $name + " at X=" + $normX + ", Y=" + $normYBottom + ", W=" + $normW)
}

Test-Pos $bg $prop 0.410 0.290 0.082 "exact_user_1"
Test-Pos $bg $prop 0.415 0.295 0.082 "exact_user_2"
Test-Pos $bg $prop 0.420 0.300 0.084 "exact_user_3"
Test-Pos $bg $prop 0.425 0.305 0.084 "exact_user_4"

$bg.Dispose()
$prop.Dispose()
