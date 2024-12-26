<h1 align="center"><img src="https://raw.githubusercontent.com/Tech5G5G/PixReader/refs/heads/master/PixReader/Assets/Original/PixReader%20Icon%20(Large).png" height="128"><br>PixReader</h1>
<p align="center"><strong>Official reader of the PIX image format</strong></p>

<p align="center">
  <img src="https://github.com/Tech5G5G/PixReader/blob/master/PixReader/Assets/Showcase.png?raw=true">
</p>

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

To try this image for your self, open [this file](https://github.com/Tech5G5G/PixReader/blob/master/Test%20pixes/Transparency%20Showcase.pix) in PixReader. Then, either press **Ctrl+Shift+R** or click **View>Remove pixel blur**

## Installation
Because I'm too lazy to either make PixReader unpackaged or create an app package and certificate, you have to build it yourself.

In order to build PixReader yourself (assuming you've already built a [WinUI project](https://learn.microsoft.com/en-us/windows/apps/winui/winui3/) before and you have [Visual Studio](https://visualstudio.microsoft.com/) already installed), simply clone the repo, open **PixReader.sln** in Visual Studio, and run it packaged (unpackaged will result in an error as PixReader relies on an API that only works in packaged mode). PixReader should be built and automatically run.

If an error occurs at any point in your usage, please [open an issue](https://github.com/Tech5G5G/PixReader/issues).

## Conversion
One interesting feature of the PixReader app is its ability to convert most popular image formats into PIX. How does it do this? Well, it's not as complicated as you may think.

After choosing an image, a GDI+ bitmap is creating using the [System.Drawing.Bitmap](https://learn.microsoft.com/en-us/dotnet/api/system.drawing.bitmap?view=windowsdesktop-9.0) class in C#. Then (on a background thread so the UI isn't lagged), every pixel of the bitmap is looped through. For each pixel, 2 things occur. First, the color of the pixel is read using the [Bitmap.GetPixel()](https://learn.microsoft.com/en-us/dotnet/api/system.drawing.bitmap.getpixel?view=windowsdesktop-9.0#system-drawing-bitmap-getpixel(system-int32-system-int32)) method. Second, the pixel's color is added to a [System.Text.StringBuilder](https://learn.microsoft.com/en-us/dotnet/api/system.text.stringbuilder?view=net-9.0). Every time the end of a row in the bitmap is reached, a PIX **nr** code is added to the StringBuilder. Finally, a save location is chosen by the user, and the width and height of the PIX are written to the file, along with the StringBuilder (which contains all the color and new row codes).

And that's basically how PixReader converts images to PIX.
