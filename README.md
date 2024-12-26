## What is a PIX?
A PIX is an uncompressed, human-readable image format that supports ARGB. A PIX file is a list of dimensions, color hex codes, and new row codes.

Every PIX starts with a pixel width and height definition of the entire picture. This makes it easy for programs to display and optimize.
Everything onward is either two things:
* An [ARGB color hex code](https://en.wikipedia.org/wiki/RGBA_color_model)
* A new row code: **nr**

### To define a color hex code:
* The first two numbers define the transparency of a pixel
* The third and fourth numbers represent the red value of a pixel
* The fifth and sixth numbers represent the green value of a pixel
* The seventh and eighth numbers represent the blue value of a pixel
* **0x** can optionally be added to the start of a color code

### Example
In this example:
`````````
Width=3
Height=2
FFFF0000
A800FF00
110000FF
nr
0xFFFF0000
0xFF00FF00
0xFF0000FF
`````````
The width is defined as 3 (pixels) and the height is defined as 2 (pixels). Then, the first pixel is solid red. The next pixel is a 66% visible green. Finally, the last pixel of the row is a slightly visible blue. After the blue pixel, comes an **nr**, meaning a new row has started. After that, the colors red, green and blue are defined, using a **0x** at the start of their codes.

In the PixReader app, this image is outtputed:

![image](https://github.com/user-attachments/assets/4c4b232d-f575-43f2-9c8d-62af65c0381d)
