Add-Type -AssemblyName System.Drawing

$img = [System.Drawing.Bitmap]::FromFile("C:/Users/jeffc/.gemini/antigravity/brain/ba6e8cb8-bda8-472f-800f-bc09d20109fa/.user_uploaded/media_1792448781248_95bd41f0.png")
$scale = 4
$zw = [int]($img.Width * $scale)
$zh = [int]($img.Height * $scale)
$zoom = New-Object System.Drawing.Bitmap($zw, $zh)
$g = [System.Drawing.Graphics]::FromImage($zoom)
$g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
$g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::Half
$g.DrawImage($img, 0, 0, $zoom.Width, $zoom.Height)
$g.Dispose()

$zoom.Save("C:/Users/jeffc/.gemini/antigravity/brain/ba6e8cb8-bda8-472f-800f-bc09d20109fa/scratch_user_zoom.png", [System.Drawing.Imaging.ImageFormat]::Png)
$zoom.Dispose()
$img.Dispose()
Write-Host "Zoomed user image saved"
