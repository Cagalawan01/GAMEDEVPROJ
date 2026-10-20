Add-Type -AssemblyName System.Drawing

$bg = [System.Drawing.Bitmap]::FromFile("Assets/Art/cherry_blossom_school.png")

# Look at the triangular wall in cherry_blossom_school.png:
# In scratch_wall_details.png (from 400 to 850 in X, 350 to 650 in Y)
# Let's find the top-left corner of the triangular stone wall under the railing:
# In cherry_blossom_school.png:
# Where does the handrail meet the top concrete cap of the triangular wall?
# Let's crop a tight 150x150 region around X = 600..750, Y = 400..550
$crop1 = $bg.Clone([System.Drawing.Rectangle]::FromLTRB(580, 400, 750, 580), $bg.PixelFormat)
$crop1.Save("C:/Users/jeffc/.gemini/antigravity/brain/ba6e8cb8-bda8-472f-800f-bc09d20109fa/scratch_triangular_wall.png")
$crop1.Dispose()
$bg.Dispose()
Write-Host "Cropped triangular wall region"
