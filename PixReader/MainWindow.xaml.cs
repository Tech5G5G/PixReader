using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using System.Drawing;
using Windows.Storage.Pickers;
using System.Diagnostics;
using System.Text;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Composition;
using Windows.Storage.Streams;
using Windows.Graphics.Imaging;
using Windows.Storage;
using System.Drawing.Imaging;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Windows.System;
using Windows.ApplicationModel.VoiceCommands;
using System.Reflection.Emit;
using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel.UserDataTasks;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace PixReader
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        private Bitmap currentImage;
        private MemoryStream currentImageStream;

        private ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

        public MainWindow(IStorageFile file)
        {
            InitializeComponent();

            this.ExtendsContentIntoTitleBar = true;
            this.AppWindow.TitleBar.PreferredHeightOption = Microsoft.UI.Windowing.TitleBarHeightOption.Tall;
            this.SetTitleBar(titleBar);

            this.AppWindow.SetIcon("Assets/PixReader.ico");
            this.Title = "PixReader";

            zoomIn.Key = (VirtualKey)187;
            zoomOut.Key = (VirtualKey)189;

            this.Content.AllowDrop = true;
            this.Content.DragEnter += (sender, e) => acrylicOverlay.Visibility = Visibility.Visible;
            this.Content.DragLeave += (sender, e) => acrylicOverlay.Visibility = Visibility.Collapsed;
            this.Content.DragOver += (sender, e) => e.AcceptedOperation = DataPackageOperation.Copy;
            this.Content.Drop += async (sender, e) =>
            {
                acrylicOverlay.Visibility = Visibility.Collapsed;

                if (!e.DataView.Contains(StandardDataFormats.StorageItems))
                    return;

                var items = await e.DataView.GetStorageItemsAsync();

                if (items.Count > 0 && items[0] is IStorageFile file && file.FileType.Equals(".pix", StringComparison.InvariantCultureIgnoreCase))
                    LoadImage(file);
            };
            pictureView.ViewChanged += (sender, e) => zoom?.SetBinding(TextBlock.TextProperty, new Binding() { Source = $"{(int)(sender.ZoomFactor * 100)}%" });

            if (localSettings.Values.TryGetValue("StatusBarEnabled", out object value))
            {
                bool enabled = (bool)value;

                statusBarToggle.IsChecked = enabled;
                statusBar.Visibility = enabled ? Visibility.Visible : Visibility.Collapsed;
                pictureView.BorderThickness = enabled ? new Thickness(0, 1, 0, 1) : new Thickness(0, 1, 0, 0);
                (this.Content as Grid).RowDefinitions[2].Height = enabled ? new GridLength(30) : new GridLength(0);
            }
            else
                localSettings.Values["StatusBarEnabled"] = true;

            if (file is not null)
                LoadImage(file);
        }

        private async void Open_Pix(object sender, RoutedEventArgs e)
        {
            var openDialog = CreateFilePickerDialog<FileOpenPicker>();
            openDialog.FileTypeFilter.Add(".pix");

            var file = await openDialog.PickSingleFileAsync();
            if (file == null)
                return;

            LoadImage(file);
        }

        private async void LoadImage(IStorageFile file)
        {
            long fileSize = new FileInfo(file.Path).Length;
            size.Text = fileSize > 1048576 ? $"{Math.Clamp((int)(fileSize / 1048576), 1, int.MaxValue)} MB" : $"{Math.Clamp((int)(fileSize / 1024), 1, int.MaxValue)} KB";

            var codes = File.ReadAllLines(file.Path);
            var source = new BitmapImage();
            var bmp = new Bitmap(
                int.Parse(codes[0].Replace("Width=", null)),
                int.Parse(codes[1].Replace("Height=", null)));

            progressBar.Maximum = bmp.Height;
            progressBarText.Text = "Opening...";
            progressBar.Visibility = progressBarText.Visibility = Visibility.Visible;

            await Task.Run(() =>
            {
                int x = 0;
                int y = 0;
                for (int i = 2; i < codes.Length; i++)
                {
                    string code = codes[i];

                    if (string.IsNullOrWhiteSpace(code))
                        continue;

                    if (code.Contains("nr"))
                    {
                        x = 0;
                        y++;
                        continue;
                    }

                    string hex = code.StartsWith("0x") ? code.Remove(0, 2) : code;
                    var color = Color.FromArgb(
                        byte.Parse($"{hex[0]}{hex[1]}", System.Globalization.NumberStyles.HexNumber),
                        byte.Parse($"{hex[2]}{hex[3]}", System.Globalization.NumberStyles.HexNumber),
                        byte.Parse($"{hex[4]}{hex[5]}", System.Globalization.NumberStyles.HexNumber),
                        byte.Parse($"{hex[6]}{hex[7]}", System.Globalization.NumberStyles.HexNumber));
                    bmp.SetPixel(x, y, color);

                    x++;
                    this.DispatcherQueue?.TryEnqueue(() => progressBar.Value = y + 1);
                }

                MemoryStream stream = new();
                bmp.Save(stream, ImageFormat.Png);
                stream.Position = 0;

                this.DispatcherQueue?.TryEnqueue(() =>
                {
                    source.SetSource(stream.AsRandomAccessStream());

                    picture.Source = source;
                    picture.Height = picture.Width = double.NaN;
                });

                currentImage = bmp;
                currentImageStream = stream;
            });

            progressBar.Visibility = progressBarText.Visibility = Visibility.Collapsed;
        }

        private async void Convert_Pix(object sender, RoutedEventArgs e)
        {
            var openDialog = CreateFilePickerDialog<FileOpenPicker>();
            openDialog.FileTypeFilter.Add(".png");
            openDialog.FileTypeFilter.Add(".jpg");
            openDialog.FileTypeFilter.Add(".jpeg");
            openDialog.FileTypeFilter.Add(".bmp");
            openDialog.FileTypeFilter.Add(".exif");
            openDialog.FileTypeFilter.Add(".tiff");

            var file = await openDialog.PickSingleFileAsync();
            if (file == null)
                return;

            StringBuilder pix = new();

            using var stream = await file.OpenReadAsync();
            using var bmp = new Bitmap(stream.AsStream());

            int width = bmp.Width;
            int height = bmp.Height;

            progressBar.Maximum = height;
            progressBarText.Text = "Converting...";
            progressBar.Visibility = progressBarText.Visibility = Visibility.Visible;

            await Task.Run(() =>
            {
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        var pixel = bmp.GetPixel(x, y);

                        pix.Append(pixel.A == 0 ? "00" : pixel.A.ToString("X2"))
                           .Append(pixel.R == 0 ? "00" : pixel.R.ToString("X2"))
                           .Append(pixel.G == 0 ? "00" : pixel.G.ToString("X2"))
                           .AppendLine(pixel.B == 0 ? "00" : pixel.B.ToString("X2"));
                    }
                    pix.AppendLine("nr");
                    this.DispatcherQueue?.TryEnqueue(() => progressBar.Value = y + 1);
                }
            });

            var saveDialog = CreateFilePickerDialog<FileSavePicker>();
            saveDialog.SuggestedFileName = "New Pix Image";
            saveDialog.FileTypeChoices.Add("Pix Image", [".pix"]);

            file = await saveDialog.PickSaveFileAsync();
            progressBar.Visibility = progressBarText.Visibility = Visibility.Collapsed;
            if (file == null)
                return;
            File.WriteAllText(file.Path, $"Width={width}\nHeight={height}\n{pix}");
        }

        private T CreateFilePickerDialog<T>() where T : new()
        {
            var dialog = new T();
            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            WinRT.Interop.InitializeWithWindow.Initialize(dialog, hWnd);
            return dialog;
        }

        private void Clear_CurrentImage(object sender, RoutedEventArgs e)
        {
            currentImage = null;
            currentImageStream = null;

            picture.Source = null;
            size.Text = "0 KB";
        }

        private async void Remove_PixelBlur(object sender, RoutedEventArgs e)
        {
            if (currentImage == null || currentImageStream == null)
                return;

            uint scaledHeight = 100 * (uint)currentImage.Height;
            uint scaledWidth = 100 * (uint)currentImage.Width;

            if (scaledHeight > 10000 || scaledWidth > 10000)
            {
                scaledHeight /= 10;
                scaledWidth /= 10;
            }
            if (scaledHeight > 10000 || scaledWidth > 10000)
            {
                var dialog = new ContentDialog()
                {
                    Title = "Error",
                    Content = "The image is too large to remove pixel blur.",
                    CloseButtonText = "OK",
                    XamlRoot = this.Content.XamlRoot,
                    DefaultButton = ContentDialogButton.Close
                };
                await dialog.ShowAsync();
                return;
            }

            BitmapTransform transform = new()
            {
                InterpolationMode = BitmapInterpolationMode.NearestNeighbor,
                ScaledHeight = scaledHeight,
                ScaledWidth = scaledWidth
            };

            var decoder = await BitmapDecoder.CreateAsync(currentImageStream.AsRandomAccessStream());
            var softBmp = await decoder.GetSoftwareBitmapAsync(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied, transform, ExifOrientationMode.RespectExifOrientation, ColorManagementMode.DoNotColorManage);
            var softSource = new SoftwareBitmapSource();
            await softSource.SetBitmapAsync(softBmp);

            picture.Source = softSource;
            picture.Height = currentImage.Height;
            picture.Width = currentImage.Width;
        }

        private void Toggle_StatusBar(object sender, RoutedEventArgs e)
        {
            var toggleSender = sender as ToggleMenuFlyoutItem;

            statusBar.Visibility = toggleSender.IsChecked ? Visibility.Visible : Visibility.Collapsed;
            pictureView.BorderThickness = toggleSender.IsChecked ? new Thickness(0, 1, 0, 1) : new Thickness(0, 1, 0, 0);
            (this.Content as Grid).RowDefinitions[2].Height = toggleSender.IsChecked ? new GridLength(30) : new GridLength(0);

            localSettings.Values["StatusBarEnabled"] = toggleSender.IsChecked;
        }

        private void ZoomIn(object sender, RoutedEventArgs e) => pictureView.ZoomBy(0.1f, new System.Numerics.Vector2((float)pictureView.ActualWidth / 2, (float)pictureView.ActualHeight / 2));

        private void ZoomOut(object sender, RoutedEventArgs e) => pictureView.ZoomBy(-0.1f, new System.Numerics.Vector2((float)pictureView.ActualWidth / 2, (float)pictureView.ActualHeight / 2));

        private void ResetZoom(object sender, RoutedEventArgs e) => pictureView.ZoomTo(1f, new System.Numerics.Vector2((float)pictureView.ActualWidth / 2, (float)pictureView.ActualHeight / 2));
    }
}
