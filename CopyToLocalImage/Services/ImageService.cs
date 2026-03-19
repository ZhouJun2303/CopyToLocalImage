using System;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

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

        /// <summary>
        /// 从剪切板保存图片
        /// </summary>
        public string? SaveImageFromClipboard()
        {
            try
            {
                var image = Clipboard.GetImage();
                if (image != null)
                {
                    return SaveImage(image);
                }
            }
            catch
            {
                // 尝试其他方式获取
            }
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

            // 保存为 PNG
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(image));

            using (var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            {
                encoder.Save(stream);
            }

            // 生成缩略图
            GenerateThumbnail(filePath, image);

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
                using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    var decoder = new PngBitmapDecoder(stream, BitmapCreateOptions.DelayCreation, BitmapCacheOption.OnDemand);
                    if (decoder.Frames.Count > 0)
                    {
                        return (decoder.Frames[0].PixelWidth, decoder.Frames[0].PixelHeight);
                    }
                }
            }
            catch
            {
                // 尝试其他方式
                try
                {
                    var bmp = new System.Drawing.Bitmap(filePath);
                    return (bmp.Width, bmp.Height);
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
