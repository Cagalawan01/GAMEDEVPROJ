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

    # Crop matching media_1792448781248_95bd41f0.png:
    # Notice: In media_1792448781248_95bd41f0.png, it shows the right half of the stack and the staircase wall.
    # In 1376x768, the region is X = 500 to 780, Y = 380 to 620
    $crop = $canvas.Clone([System.Drawing.Rectangle]::FromLTRB(460, 370, 780, 620), $canvas.PixelFormat)
    $crop.Save("C:/Users/jeffc/.gemini/antigravity/brain/ba6e8cb8-bda8-472f-800f-bc09d20109fa/$name.png", [System.Drawing.Imaging.ImageFormat]::Png)
    $crop.Dispose()
    $canvas.Dispose()
    Write-Host ("Created " + $name + " at X=" + $normX + ", Y=" + $normYBottom + ", W=" + $normW)
}

Test-Pos $bg $prop 0.345 0.285 0.082 "match_user_1"
Test-Pos $bg $prop 0.355 0.280 0.080 "match_user_2"
Test-Pos $bg $prop 0.365 0.275 0.078 "match_user_3"

$bg.Dispose()
$prop.Dispose()
