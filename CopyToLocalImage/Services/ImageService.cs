using System;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Interop;

namespace CopyToLocalImage.Services
{
    /// <summary>
    /// 图片保存和转换服务
    /// </summary>
    public class ImageService
    {
        private readonly string _savePath;

        public ImageService(string savePath)
        {
            _savePath = savePath;
        }

        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        private static extern bool DeleteObject(nint hObject);

        /// <summary>
        /// 从剪切板保存图片
        /// </summary>
        public string? SaveImageFromClipboard()
        {
            LogService.Debug("=== 开始从剪切板获取图片 ===");

            // 方法 1: 使用 WPF Clipboard.GetImage()
            try
            {
                var image = Clipboard.GetImage();
                if (image != null)
                {
                    LogService.Debug($"WPF 方式：PixelWidth={image.PixelWidth}, PixelHeight={image.PixelHeight}");
                    if (image.PixelWidth > 0 && image.PixelHeight > 0)
                    {
                        var filePath = SaveImage(image);
                        LogService.Info($"WPF 方式保存图片成功：{filePath}");
                        return filePath;
                    }
                    else
                    {
                        LogService.Warning("WPF 方式：图片尺寸为 0，跳过");
                    }
                }
                else
                {
                    LogService.Debug("WPF 方式：Clipboard.GetImage() 返回 null");
                }
            }
            catch (Exception ex)
            {
                LogService.Debug($"WPF 方式获取失败：{ex.Message}");
            }

            // 方法 2: 尝试使用 System.Windows.Forms.Clipboard (兼容微信等)
            try
            {
                if (System.Windows.Forms.Clipboard.ContainsImage())
                {
                    var winFormsImage = System.Windows.Forms.Clipboard.GetImage();
                    if (winFormsImage is Bitmap winFormsBitmap && winFormsBitmap.Width > 0 && winFormsBitmap.Height > 0)
                    {
                        LogService.Debug($"WinForms 方式：Width={winFormsBitmap.Width}, Height={winFormsBitmap.Height}, PixelFormat={winFormsBitmap.PixelFormat}");
                        // 转换为 BitmapSource
                        var hBitmap = winFormsBitmap.GetHbitmap();
                        try
                        {
                            var bitmapSource = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                                hBitmap,
                                nint.Zero,
                                System.Windows.Int32Rect.Empty,
                                System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());

                            LogService.Debug($"转换后：PixelWidth={bitmapSource.PixelWidth}, PixelHeight={bitmapSource.PixelHeight}");
                            var filePath = SaveImage(bitmapSource);
                            LogService.Info($"WinForms 方式保存图片成功：{filePath}");
                            return filePath;
                        }
                        finally
                        {
                            DeleteObject(hBitmap);
                            winFormsBitmap.Dispose();
                        }
                    }
                    else if (winFormsImage != null)
                    {
                        LogService.Warning($"WinForms 方式：图片尺寸异常 Width={winFormsImage.Width}, Height={winFormsImage.Height}");
                        winFormsImage.Dispose();
                    }
                    else
                    {
                        LogService.Debug("WinForms 方式：Clipboard.GetImage() 返回 null");
                    }
                }
                else
                {
                    LogService.Debug("WinForms 方式：Clipboard.ContainsImage() 返回 false");
                }
            }
            catch (Exception ex)
            {
                LogService.Debug($"WinForms 方式获取失败：{ex.Message}");
            }

            // 方法 3: 尝试直接读取 DIB 数据（针对微信等使用 DIB 格式的截图工具）
            try
            {
                if (System.Windows.Forms.Clipboard.ContainsData(System.Windows.Forms.DataFormats.Dib))
                {
                    var dataObject = System.Windows.Forms.Clipboard.GetDataObject();
                    if (dataObject?.GetDataPresent(System.Windows.Forms.DataFormats.Dib) == true)
                    {
                        var dibData = dataObject.GetData(System.Windows.Forms.DataFormats.Dib);
                        if (dibData is System.IO.MemoryStream memStream && memStream.Length > 0)
                        {
                            LogService.Debug($"DIB 方式：流大小={memStream.Length} 字节");
                            memStream.Position = 0; // 重置流位置
                            using (var bitmap = new System.Drawing.Bitmap(memStream))
                            {
                                if (bitmap.Width > 0 && bitmap.Height > 0)
                                {
                                    LogService.Debug($"DIB Bitmap: Width={bitmap.Width}, Height={bitmap.Height}, PixelFormat={bitmap.PixelFormat}");
                                    var hBitmap = bitmap.GetHbitmap();
                                    try
                                    {
                                        var bitmapSource = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                                            hBitmap,
                                            nint.Zero,
                                            System.Windows.Int32Rect.Empty,
                                            System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());

                                        LogService.Debug($"DIB 转换后：PixelWidth={bitmapSource.PixelWidth}, PixelHeight={bitmapSource.PixelHeight}");
                                        var filePath = SaveImage(bitmapSource);
                                        LogService.Info($"DIB 方式保存图片成功：{filePath}");
                                        return filePath;
                                    }
                                    finally
                                    {
                                        DeleteObject(hBitmap);
                                    }
                                }
                                else
                                {
                                    LogService.Warning($"DIB 方式：图片尺寸异常 Width={bitmap.Width}, Height={bitmap.Height}");
                                }
                            }
                        }
                        else
                        {
                            LogService.Debug($"DIB 方式：数据不是 MemoryStream 或长度为 0, Length={(dibData as System.IO.MemoryStream)?.Length}");
                        }
                    }
                    else
                    {
                        LogService.Debug("DIB 方式：GetDataPresent 返回 false");
                    }
                }
                else
                {
                    LogService.Debug("DIB 方式：Clipboard.ContainsData(Dib) 返回 false");
                }
            }
            catch (Exception ex)
            {
                LogService.Debug($"DIB 方式获取失败：{ex.Message}");
            }

            LogService.Warning("所有方式获取剪切板图片均失败");
            return null;
        }

        /// <summary>
        /// 保存图片为 PNG 格式
        /// </summary>
        private string SaveImage(BitmapSource image)
        {
            var dateFolder = Path.Combine(_savePath, DateTime.Now.ToString("yyyy-MM-dd"));
            if (!Directory.Exists(dateFolder))
                Directory.CreateDirectory(dateFolder);

            // 生成文件名
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var randomSuffix = Guid.NewGuid().ToString("N").Substring(0, 6);
            var fileName = $"clipboard_{timestamp}_{randomSuffix}.png";
            var filePath = Path.Combine(dateFolder, fileName);

            // 转换为 FormatConvertedBitmap 以确保正确的像素格式（解决微信截图黑屏问题）
            var convertedImage = new FormatConvertedBitmap(
                image,
                System.Windows.Media.PixelFormats.Bgr32,
                null,
                0);

            // 保存为 PNG
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(convertedImage));

            using (var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            {
                encoder.Save(stream);
            }

            // 生成缩略图
            GenerateThumbnail(filePath, convertedImage);

            return filePath;
        }

        /// <summary>
        /// 生成缩略图
        /// </summary>
        private void GenerateThumbnail(string filePath, BitmapSource original)
        {
            try
            {
                var thumbnailPath = Path.ChangeExtension(filePath, ".thumb.png");
                var thumbnailDir = Path.Combine(_savePath, "_thumbnails", DateTime.Now.ToString("yyyy-MM-dd"));

                if (!Directory.Exists(thumbnailDir))
                    Directory.CreateDirectory(thumbnailDir);

                thumbnailPath = Path.Combine(thumbnailDir, Path.GetFileName(thumbnailPath));

                // 生成 200x200 缩略图
                var width = 200;
                var height = 200;

                var transformedBitmap = new TransformedBitmap(
                    original,
                    new System.Windows.Media.ScaleTransform(
                        width / (double)original.PixelWidth,
                        height / (double)original.PixelHeight));

                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(transformedBitmap));

                using (var stream = new FileStream(thumbnailPath, FileMode.Create, FileAccess.Write))
                {
                    encoder.Save(stream);
                }
            }
            catch
            {
                // 缩略图生成失败不影响主流程
            }
        }

        /// <summary>
        /// 获取图片尺寸
        /// </summary>
        public static (int width, int height) GetImageDimensions(string filePath)
        {
            try
            {
                using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    var decoder = new PngBitmapDecoder(stream, BitmapCreateOptions.DelayCreation, BitmapCacheOption.OnLoad);
                    if (decoder.Frames.Count > 0)
                    {
                        var frame = decoder.Frames[0];
                        return (frame.PixelWidth, frame.PixelHeight);
                    }
                }
            }
            catch
            {
                // 尝试其他方式
                try
                {
                    using (var bmp = new System.Drawing.Bitmap(filePath))
                    {
                        return (bmp.Width, bmp.Height);
                    }
                }
                catch
                {
                    return (0, 0);
                }
            }
            return (0, 0);
        }

        /// <summary>
        /// 获取缩略图路径
        /// </summary>
        public string GetThumbnailPath(string imagePath)
        {
            var dateFolder = Path.GetFileName(Path.GetDirectoryName(imagePath));
            var fileName = Path.GetFileNameWithoutExtension(imagePath) + ".thumb.png";
            return Path.Combine(_savePath, "_thumbnails", dateFolder, fileName);
        }
    }
}
