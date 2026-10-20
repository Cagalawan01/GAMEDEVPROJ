Add-Type -AssemblyName System.Drawing

$userImg = [System.Drawing.Bitmap]::FromFile("C:/Users/jeffc/.gemini/antigravity/brain/ba6e8cb8-bda8-472f-800f-bc09d20109fa/.user_uploaded/media_1792448781248_95bd41f0.png")
Write-Host "User crop image size: $($userImg.Width) x $($userImg.Height)"

# In userImg, look at the features:
# On the right is the staircase triangular retaining wall and railing!
# On the left is the stack of lanterns under the tarp!
# Look at where the stack's rightmost edge touches the retaining wall:
# In userImg, the triangular wall top corner starts around x = 50, y = 30?
# Let's inspect where the triangular wall is in userImg and in cherry_blossom_school.png

$bg = [System.Drawing.Bitmap]::FromFile("Assets/Art/cherry_blossom_school.png")
Write-Host "Background size: $($bg.Width) x $($bg.Height)"

$userImg.Dispose()
$bg.Dispose()
