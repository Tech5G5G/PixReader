Width=3 <-- Width and height definition
Height=2
FFFF0000 <-- Solid red
A800FF00 <-- 66% visible green
110000FF <-- Slightly visible blue
nr <-- Start a new row
0xFFFF0000 <-- Hex starting with 0x also usable
0xFF00FF00
0xFF0000FF

Start with defining the width and height of the entire image

Define pixels using hex codes (0-9-A-F)
-First two numbers define transparency of pixel
-Third and fourth #s represent R value of pixel
-Fifth and sixth #s represent G value of pixel
-Seventh and eighth #s represent B value of pixel

Start a new row using "nr" code