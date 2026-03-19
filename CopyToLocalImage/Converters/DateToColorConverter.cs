using System;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace CopyToLocalImage.Converters
{
    /// <summary>
    /// 日期转颜色转换器（旧图片变灰）
    /// </summary>
    public class DateToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is DateTime dateTime)
            {
                var daysOld = (DateTime.Now - dateTime).TotalDays;

                // 超过 7 天开始变灰，30 天完全灰色
                if (daysOld > 30)
                    return new SolidColorBrush(Color.FromRgb(150, 150, 150));
                if (daysOld > 7)
                {
                    var ratio = (daysOld - 7) / 23.0;
                    var r = (byte)(200 - ratio * 50);
                    var g = (byte)(200 - ratio * 50);
                    var b = (byte)(200 - ratio * 50);
                    return new SolidColorBrush(Color.FromRgb(r, g, b));
                }
                return new SolidColorBrush(Color.FromRgb(200, 200, 200));
            }
            return new SolidColorBrush(Color.FromRgb(200, 200, 200));
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 布尔取反转换器
    /// </summary>
    public class InverseBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is bool b)
                return !b;
            return true;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is bool b)
                return !b;
            return true;
        }
    }

    /// <summary>
    /// 布尔转可见性转换器
    /// </summary>
    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is bool b)
            {
                var useInverse = parameter?.ToString() == "inverse";
                if (useInverse)
                    b = !b;
                return b ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is Visibility v)
                return v == Visibility.Visible;
            return false;
        }
    }

    /// <summary>
    /// 文件路径转文件名转换器
    /// </summary>
    public class FileNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is string path && !string.IsNullOrEmpty(path))
            {
                return Path.GetFileNameWithoutExtension(path);
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
