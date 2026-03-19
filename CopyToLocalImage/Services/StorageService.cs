using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CopyToLocalImage.Models;
using Newtonsoft.Json;

namespace CopyToLocalImage.Services
{
    /// <summary>
    /// 存储和元数据管理服务（使用 JSON 存储）
    /// </summary>
    public class StorageService
    {
        private readonly string _savePath;
        private readonly string _metadataPath;
        private List<ImageItem> _imageItems;
        private readonly object _lock = new();

        public StorageService(string savePath)
        {
            _savePath = savePath;
            _metadataPath = Path.Combine(savePath, "metadata.json");
            _imageItems = new List<ImageItem>();
        }

        /// <summary>
        /// 加载元数据
        /// </summary>
        public async Task LoadAsync()
        {
            await Task.Run(() =>
            {
                lock (_lock)
                {
                    if (File.Exists(_metadataPath))
                    {
                        try
                        {
                            var json = File.ReadAllText(_metadataPath);
                            _imageItems = JsonConvert.DeserializeObject<List<ImageItem>>(json) ?? new List<ImageItem>();
                        }
                        catch
                        {
                            _imageItems = new List<ImageItem>();
                        }
                    }
                    else
                    {
                        // 从文件系统扫描
                        ScanDirectory();
                    }
                }
            });
        }

        /// <summary>
        /// 扫描目录加载现有图片
        /// </summary>
        private void ScanDirectory()
        {
            if (!Directory.Exists(_savePath))
                return;

            try
            {
                var pngFiles = Directory.GetFiles(_savePath, "*.png", SearchOption.AllDirectories)
                    .Where(f => !f.Contains("_thumbnails") && !f.EndsWith(".thumb.png"))
                    .OrderByDescending(f => f)
                    .Take(1000); // 限制加载数量

                foreach (var file in pngFiles)
                {
                    try
                    {
                        var info = new FileInfo(file);
                        var (width, height) = ImageService.GetImageDimensions(file);

                        _imageItems.Add(new ImageItem
                        {
                            Id = Guid.NewGuid().ToString("N"),
                            FilePath = file,
                            ThumbnailPath = "",
                            CreatedAt = info.CreationTime,
                            FileSize = info.Length,
                            Width = width,
                            Height = height
                        });
                    }
                    catch
                    {
                        // 跳过无法读取的文件
                    }
                }

                Save();
            }
            catch
            {
                _imageItems = new List<ImageItem>();
            }
        }

        /// <summary>
        /// 保存图片记录
        /// </summary>
        public ImageItem AddImageRecord(string filePath)
        {
            lock (_lock)
            {
                var info = new FileInfo(filePath);
                var (width, height) = ImageService.GetImageDimensions(filePath);

                var item = new ImageItem
                {
                    FilePath = filePath,
                    CreatedAt = info.CreationTime,
                    FileSize = info.Length,
                    Width = width,
                    Height = height
                };

                item.ThumbnailPath = Path.Combine(
                    _savePath,
                    "_thumbnails",
                    DateTime.Now.ToString("yyyy-MM-dd"),
                    Path.GetFileNameWithoutExtension(filePath) + ".thumb.png");

                _imageItems.Insert(0, item);
                Save();

                return item;
            }
        }

        /// <summary>
        /// 保存元数据
        /// </summary>
        private void Save()
        {
            try
            {
                var dir = Path.GetDirectoryName(_metadataPath);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                var json = JsonConvert.SerializeObject(_imageItems, Formatting.Indented);
                File.WriteAllText(_metadataPath, json);
            }
            catch
            {
                // 保存失败静默处理
            }
        }

        /// <summary>
        /// 获取所有图片
        /// </summary>
        public ObservableCollection<ImageItem> GetAllImages()
        {
            lock (_lock)
            {
                return new ObservableCollection<ImageItem>(_imageItems.OrderByDescending(i => i.CreatedAt));
            }
        }

        /// <summary>
        /// 按日期筛选
        /// </summary>
        public ObservableCollection<ImageItem> GetImagesByDate(DateTime date)
        {
            lock (_lock)
            {
                return new ObservableCollection<ImageItem>(
                    _imageItems
                        .Where(i => i.CreatedAt.Date == date.Date)
                        .OrderByDescending(i => i.CreatedAt));
            }
        }

        /// <summary>
        /// 获取今天的图片
        /// </summary>
        public ObservableCollection<ImageItem> GetTodayImages()
        {
            return GetImagesByDate(DateTime.Today);
        }

        /// <summary>
        /// 获取本周的图片
        /// </summary>
        public ObservableCollection<ImageItem> GetThisWeekImages()
        {
            lock (_lock)
            {
                var startOfWeek = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek);
                return new ObservableCollection<ImageItem>(
                    _imageItems
                        .Where(i => i.CreatedAt >= startOfWeek)
                        .OrderByDescending(i => i.CreatedAt));
            }
        }

        /// <summary>
        /// 获取本月的图片
        /// </summary>
        public ObservableCollection<ImageItem> GetThisMonthImages()
        {
            lock (_lock)
            {
                var startOfMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
                return new ObservableCollection<ImageItem>(
                    _imageItems
                        .Where(i => i.CreatedAt >= startOfMonth)
                        .OrderByDescending(i => i.CreatedAt));
            }
        }

        /// <summary>
        /// 删除图片
        /// </summary>
        public bool DeleteImage(ImageItem item)
        {
            lock (_lock)
            {
                try
                {
                    // 删除文件
                    if (File.Exists(item.FilePath))
                        File.Delete(item.FilePath);

                    // 删除缩略图
                    if (File.Exists(item.ThumbnailPath))
                        File.Delete(item.ThumbnailPath);

                    // 从列表中移除
                    _imageItems.Remove(item);
                    Save();

                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// 批量删除
        /// </summary>
        public int DeleteImages(IEnumerable<ImageItem> items)
        {
            lock (_lock)
            {
                var count = 0;
                foreach (var item in items.ToList())
                {
                    if (DeleteImage(item))
                        count++;
                }
                return count;
            }
        }

        /// <summary>
        /// 删除指定日期之前的图片
        /// </summary>
        public int DeleteImagesBeforeDate(DateTime date)
        {
            lock (_lock)
            {
                var toDelete = _imageItems.Where(i => i.CreatedAt < date).ToList();
                return DeleteImages(toDelete);
            }
        }

        /// <summary>
        /// 获取所有日期列表
        /// </summary>
        public List<DateTime> GetUniqueDates()
        {
            lock (_lock)
            {
                return _imageItems
                    .Select(i => i.CreatedAt.Date)
                    .Distinct()
                    .OrderByDescending(d => d)
                    .ToList();
            }
        }

        /// <summary>
        /// 清理 N 天前的图片
        /// </summary>
        public int CleanOldImages(int days)
        {
            var cutoffDate = DateTime.Today.AddDays(-days);
            return DeleteImagesBeforeDate(cutoffDate);
        }
    }
}
