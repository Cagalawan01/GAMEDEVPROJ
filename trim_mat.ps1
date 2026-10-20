Add-Type -AssemblyName System.Drawing

$src = [System.Drawing.Bitmap]::FromFile("C:/Users/jeffc/.gemini/antigravity/brain/ba6e8cb8-bda8-472f-800f-bc09d20109fa/scratch_tarp_grounded_cutout.png")
$w = $src.Width
$h = $src.Height

# Let's inspect where the bottom mat corner is:
# In scratch_tarp_grounded_cutout.png:
# The wooden box is at x ~ 370 to 520, y ~ 750 to 860
# Below the box and around the bottom, there is a dark grey ground mat extending to:
# Left corner: (x ~ 10, y ~ 660)
# Bottom corner: (x ~ 500, y ~ 850)
# Right corner: (x ~ 1050, y ~ 710)

# If we trim the excess mat below y = 840 and softly feather the bottom edge:
$dst = New-Object System.Drawing.Bitmap($src)

for ($y = 0; $y -lt $h; $y++) {
    for ($x = 0; $x -lt $w; $x++) {
        $c = $dst.GetPixel($x, $y)
        if ($c.A -gt 0) {
            # Trim the sharp pointy mat corners that stick out far from the lanterns
            # Left pointy mat flap (x < 100, y > 600):
            if ($x -lt 85 -and $y -gt 630) {
                $dst.SetPixel($x, $y, [System.Drawing.Color]::FromArgb(0, 0, 0, 0))
            }
            # Bottom pointy mat corner below the box (y > 835):
            if ($y -gt 835) {
                $fade = [math]::Max(0, [math]::Min(255, [int]((853 - $y) * 255.0 / 18.0)))
                $dst.SetPixel($x, $y, [System.Drawing.Color]::FromArgb([int]($c.A * $fade / 255.0), $c.R, $c.G, $c.B))
            }
        }
    }
}

$dst.Save("C:/Users/jeffc/.gemini/antigravity/brain/ba6e8cb8-bda8-472f-800f-bc09d20109fa/scratch_tarp_trimmed.png", [System.Drawing.Imaging.ImageFormat]::Png)
$dst.Dispose()
$src.Dispose()
Write-Host "Trimmed prop created"
