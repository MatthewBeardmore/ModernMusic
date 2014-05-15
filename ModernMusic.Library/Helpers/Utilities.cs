using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media.Imaging;

namespace ModernMusic.Helpers
{
    public class ObjectToTypeStringConverter : IValueConverter
    {
        public object Convert(
         object value, Type targetType,
         object parameter, string culture)
        {
            return value.GetType().Name;
        }

        public object ConvertBack(
         object value, Type targetType,
         object parameter, string culture)
        {
            // I don't think you'll need this
            throw new Exception("Can't convert back");
        }
    }

    public class Utilities
    {
        public static async Task<Uri> DownloadFile(Uri uri)
        {
            try
            {
                var response = await HttpWebRequest.Create(uri).GetResponseAsync();
                List<Byte> allBytes = new List<byte>();
                using (Stream imageStream = response.GetResponseStream())
                {
                    string fileName = Path.GetRandomFileName();
                    var file = await ApplicationData.Current.LocalFolder.CreateFileAsync(
                        fileName + ".jpg", CreationCollisionOption.GenerateUniqueName);

                    using (var fileStream = await file.OpenAsync(FileAccessMode.ReadWrite))
                    {
                        await RandomAccessStream.CopyAndCloseAsync(imageStream.AsInputStream(), fileStream.GetOutputStreamAt(0));
                    }
                    return new Uri("ms-appdata:///local/" + file.Name);
                }
            }
            catch { return null; }
        }

        public static async Task<Uri> ResizeImageFileToFile(Uri uri, uint size)
        {
            IRandomAccessStream ras = await ResizeImageFile(uri, size);

            string fileName = Path.GetRandomFileName();
            var file = await ApplicationData.Current.LocalFolder.CreateFileAsync(
                fileName + ".jpg", CreationCollisionOption.GenerateUniqueName);

            using (var fileStream = await file.OpenAsync(FileAccessMode.ReadWrite))
            {
                await RandomAccessStream.CopyAndCloseAsync(ras.GetInputStreamAt(0), fileStream.GetOutputStreamAt(0));
            }
            return new Uri("ms-appdata:///local/" + file.Name);
        }

        public static async Task<IRandomAccessStream> ResizeImageFile(Uri uri, uint size)
        {
            StorageFile file = await StorageFile.GetFileFromApplicationUriAsync(uri);

            using (var fileStream = await file.OpenAsync(FileAccessMode.Read))
            {
                BitmapDecoder bmpDecoder = await BitmapDecoder.CreateAsync(fileStream);
                BitmapFrame frame = await bmpDecoder.GetFrameAsync(0);

                BitmapTransform bmpTrans = new BitmapTransform();
                bmpTrans.InterpolationMode = BitmapInterpolationMode.Cubic;
                /*BitmapBounds bounds = new BitmapBounds();
                bounds.X = 0;
                bounds.Y = 0;
                bounds.Height = 360;
                bounds.Height = 360;
                bmpTrans.Bounds = bounds;*/
                bmpTrans.ScaledHeight = size;
                bmpTrans.ScaledWidth = size;

                PixelDataProvider pixelDataProvider = await frame.GetPixelDataAsync(BitmapPixelFormat.Rgba8, BitmapAlphaMode.Ignore, bmpTrans, ExifOrientationMode.RespectExifOrientation, ColorManagementMode.ColorManageToSRgb);
                byte[] pixelData = pixelDataProvider.DetachPixelData();

                InMemoryRandomAccessStream ras = new InMemoryRandomAccessStream();
                BitmapEncoder enc = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, ras);
                // write the pixel data to our stream
                enc.SetPixelData(BitmapPixelFormat.Rgba8, BitmapAlphaMode.Ignore, size, size, bmpDecoder.DpiX, bmpDecoder.DpiY, pixelData);
                await enc.FlushAsync();

                return ras;
            }
        }

        public static async Task<Uri> ResizeImage(Uri uri, uint size)
        {
            if (uri.IsFile)
                return uri;

            var response = await HttpWebRequest.Create(uri).GetResponseAsync();
            List<Byte> allBytes = new List<byte>();
            using (Stream imageStream = response.GetResponseStream())
            {
                InMemoryRandomAccessStream downloadedImageRas = new InMemoryRandomAccessStream();
                await RandomAccessStream.CopyAndCloseAsync(imageStream.AsInputStream(), downloadedImageRas.GetOutputStreamAt(0));
                BitmapDecoder bmpDecoder = await BitmapDecoder.CreateAsync(downloadedImageRas);
                BitmapFrame frame = await bmpDecoder.GetFrameAsync(0);

                BitmapTransform bmpTrans = new BitmapTransform();
                bmpTrans.InterpolationMode = BitmapInterpolationMode.Cubic;
                /*BitmapBounds bounds = new BitmapBounds();
                bounds.X = 0;
                bounds.Y = 0;
                bounds.Height = 360;
                bounds.Height = 360;
                bmpTrans.Bounds = bounds;*/
                bmpTrans.ScaledHeight = size;
                bmpTrans.ScaledWidth = size;

                PixelDataProvider pixelDataProvider = await frame.GetPixelDataAsync(BitmapPixelFormat.Rgba8, BitmapAlphaMode.Ignore, bmpTrans, ExifOrientationMode.RespectExifOrientation, ColorManagementMode.ColorManageToSRgb);
                byte[] pixelData = pixelDataProvider.DetachPixelData();

                InMemoryRandomAccessStream ras = new InMemoryRandomAccessStream();
                BitmapEncoder enc = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, ras);
                // write the pixel data to our stream
                enc.SetPixelData(BitmapPixelFormat.Rgba8, BitmapAlphaMode.Ignore, size, size, bmpDecoder.DpiX, bmpDecoder.DpiY, pixelData);
                await enc.FlushAsync();

                string fileName = Path.GetRandomFileName();
                var file = await ApplicationData.Current.LocalFolder.CreateFileAsync(
                    fileName + ".jpg", CreationCollisionOption.GenerateUniqueName);

                using (var fileStream = await file.OpenAsync(FileAccessMode.ReadWrite))
                {
                    await RandomAccessStream.CopyAndCloseAsync(ras.GetInputStreamAt(0), fileStream.GetOutputStreamAt(0));
                }
                return new Uri("ms-appdata:///local/" + file.Name);
            }
        }
    }
}
