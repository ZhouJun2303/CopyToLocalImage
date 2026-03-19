using System;

namespace CopyToLocalImage.Models
{
    /// <summary>
    /// 图片数据模型
    /// </summary>
    public class ImageItem
    {
        /// <summary>
        /// 唯一标识符
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString("N");

        /// <summary>
        /// 文件路径
        /// </summary>
        public string FilePath { get; set; } = string.Empty;

        /// <summary>
        /// 缩略图路径
        /// </summary>
        public string ThumbnailPath { get; set; } = string.Empty;

        /// <summary>
        /// 保存时间
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// 文件大小（字节）
        /// </summary>
        public long FileSize { get; set; }

        /// <summary>
        /// 图片宽度
        /// </summary>
        public int Width { get; set; }

        /// <summary>
        /// 图片高度
        /// </summary>
        public int Height { get; set; }

        /// <summary>
        /// 是否被选中（用于批量删除）
        /// </summary>
        public bool IsSelected { get; set; }

        /// <summary>
        /// 显示用的日期字符串
        /// </summary>
        public string DateString => CreatedAt.ToString("yyyy-MM-dd");

        /// <summary>
        /// 显示用的文件大小
        /// </summary>
        public string FileSizeDisplay
        {
            get
            {
                if (FileSize < 1024)
                    return $"{FileSize} B";
                if (FileSize < 1024 * 1024)
                    return $"{FileSize / 1024.0:F1} KB";
                return $"{FileSize / (1024.0 * 1024):F1} MB";
            }
        }

        /// <summary>
        /// 文件是否存在
        /// </summary>
        public bool FileExists => System.IO.File.Exists(FilePath);
    }
}
