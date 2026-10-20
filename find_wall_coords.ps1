Add-Type -AssemblyName System.Drawing

$src = [System.Drawing.Bitmap]::FromFile("Assets/Art/cherry_blossom_school.png")
Write-Host "Image size: $($src.Width) x $($src.Height)"

# In curb_region_1920.jpg, the crop was:
# X from 672, Y from 350.
# The stone wall at the staircase:
# In curb_region_1920.jpg:
# The bottom corner of the concrete curb (bottom of the stairs) is at:
# Let's inspect where the bottom concrete block is in the cropped region.
# Let's draw coordinate marks on that region and save it.

$bmp = New-Object System.Drawing.Bitmap(500, 400)
$g = [System.Drawing.Graphics]::FromImage($bmp)
# Let's crop x=0 to 300, y=100 to 450 in the background image:
# Wait, where was the stone wall in cherry_blossom_school.png?
# In curb_region_1920.jpg, the left side of the crop (x=672 in background) was:
# Look at curb_region_1920.jpg: The railing is on the LEFT side of curb_region_1920.jpg!
# So x in background is around 672 + (x in crop).
# Let's find where the railing and stone wall are in the background image.

$g.Dispose()
$bmp.Dispose()
$src.Dispose()
