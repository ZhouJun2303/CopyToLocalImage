using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using CopyToLocalImage.Models;
using CopyToLocalImage.Services;

namespace CopyToLocalImage
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly StorageService _storageService;
        private readonly ImageService _imageService;
        private ObservableCollection<ImageItem> _allImages;
        private bool _isGridView = true;

        public MainWindow(StorageService storageService, ImageService imageService)
        {
            InitializeComponent();
            _storageService = storageService;
            _imageService = imageService;
            _allImages = new ObservableCollection<ImageItem>();

            LoadImages();
        }

        /// <summary>
        /// 加载图片列表
        /// </summary>
        private async void LoadImages()
        {
            await System.Threading.Tasks.Task.Run(() =>
            {
                _storageService.LoadAsync().Wait();
            });

            _allImages = _storageService.GetAllImages();
            UpdateView();
        }

        /// <summary>
        /// 更新视图
        /// </summary>
        private void UpdateView()
        {
            GridViewListBox.ItemsSource = _allImages;
            ListViewListBox.ItemsSource = _allImages;

            // 更新空状态
            EmptyTextBlock.Visibility = _allImages.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            GridViewListBox.Visibility = _allImages.Count == 0 ? Visibility.Collapsed : (_isGridView ? Visibility.Visible : Visibility.Collapsed);
            ListViewListBox.Visibility = _allImages.Count == 0 ? Visibility.Collapsed : (!_isGridView ? Visibility.Visible : Visibility.Collapsed);
        }

        /// <summary>
        /// 添加新图片
        /// </summary>
        public void AddImage(ImageItem item)
        {
            Dispatcher.Invoke(() =>
            {
                _allImages.Insert(0, item);
                UpdateView();
            });
        }

        /// <summary>
        /// 刷新图片列表
        /// </summary>
        public void RefreshImages()
        {
            LoadImages();
        }

        #region 视图切换

        private void GridViewButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_isGridView)
            {
                _isGridView = true;
                GridViewButton.IsChecked = true;
                ListViewButton.IsChecked = false;
                GridViewListBox.Visibility = Visibility.Visible;
                ListViewListBox.Visibility = Visibility.Collapsed;
            }
        }

        private void ListViewButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isGridView)
            {
                _isGridView = false;
                GridViewButton.IsChecked = false;
                ListViewButton.IsChecked = true;
                GridViewListBox.Visibility = Visibility.Collapsed;
                ListViewListBox.Visibility = Visibility.Visible;
            }
        }

        #endregion

        #region 筛选功能

        private void FilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_storageService == null) return;

            if (FilterComboBox.SelectedItem is ComboBoxItem item)
            {
                var content = item.Content?.ToString();
                switch (content)
                {
                    case "全部":
                        _allImages = _storageService.GetAllImages();
                        break;
                    case "今天":
                        _allImages = _storageService.GetTodayImages();
                        break;
                    case "本周":
                        _allImages = _storageService.GetThisWeekImages();
                        break;
                    case "本月":
                        _allImages = _storageService.GetThisMonthImages();
                        break;
                }
                UpdateView();
            }
        }

        #endregion

        #region 批量删除

        private void SelectAllButton_Click(object sender, RoutedEventArgs e)
        {
            var listBox = _isGridView ? GridViewListBox : ListViewListBox;
            if (listBox != null)
            {
                listBox.SelectAll();
            }
        }

        private async void DeleteSelectedButton_Click(object sender, RoutedEventArgs e)
        {
            var listBox = _isGridView ? GridViewListBox : ListViewListBox;
            if (listBox?.SelectedItems.Count == 0)
            {
                MessageBox.Show("请先选择要删除的图片", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show(
                $"确定要删除选中的 {listBox.SelectedItems.Count} 张图片吗？\n\n此操作不可恢复！",
                "确认删除",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
                return;

            var selectedItems = listBox.SelectedItems.Cast<ImageItem>().ToList();
            var deletedCount = await System.Threading.Tasks.Task.Run(() =>
                _storageService.DeleteImages(selectedItems));

            MessageBox.Show($"已删除 {deletedCount} 张图片", "删除完成", MessageBoxButton.OK, MessageBoxImage.Information);

            // 刷新列表
            RefreshImages();
        }

        private void DeleteOldDataButton_Click(object sender, RoutedEventArgs e)
        {
            var menu = new ContextMenu();

            var item7 = new MenuItem { Header = "删除 7 天前的图片" };
            item7.Click += async (s, args) => await DeleteByDays(7);
            menu.Items.Add(item7);

            var item30 = new MenuItem { Header = "删除 30 天前的图片" };
            item30.Click += async (s, args) => await DeleteByDays(30);
            menu.Items.Add(item30);

            var item90 = new MenuItem { Header = "删除 90 天前的图片" };
            item90.Click += async (s, args) => await DeleteByDays(90);
            menu.Items.Add(item90);

            menu.Items.Add(new Separator());

            var itemCustom = new MenuItem { Header = "选择日期删除..." };
            itemCustom.Click += async (s, args) => await DeleteByCustomDate();
            menu.Items.Add(itemCustom);

            menu.IsOpen = true;
        }

        private async System.Threading.Tasks.Task DeleteByDays(int days)
        {
            var cutoffDate = DateTime.Today.AddDays(-days);
            var result = MessageBox.Show(
                $"确定要删除 {days} 天前的所有图片吗？\n\n此操作不可恢复！",
                "确认删除",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
                return;

            var deletedCount = await System.Threading.Tasks.Task.Run(() =>
                _storageService.DeleteImagesBeforeDate(cutoffDate));

            MessageBox.Show($"已删除 {deletedCount} 张图片", "删除完成", MessageBoxButton.OK, MessageBoxImage.Information);
            RefreshImages();
        }

        private async System.Threading.Tasks.Task DeleteByCustomDate()
        {
            var dialog = new DatePickerDialog();
            if (dialog.ShowDialog() == true)
            {
                var selectedDate = dialog.SelectedDate;
                var result = MessageBox.Show(
                    $"确定要删除 {selectedDate:yyyy-MM-dd} 之前的所有图片吗？\n\n此操作不可恢复！",
                    "确认删除",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result != MessageBoxResult.Yes)
                    return;

                var deletedCount = await System.Threading.Tasks.Task.Run(() =>
                    _storageService.DeleteImagesBeforeDate(selectedDate));

                MessageBox.Show($"已删除 {deletedCount} 张图片", "删除完成", MessageBoxButton.OK, MessageBoxImage.Information);
                RefreshImages();
            }
        }

        #endregion

        #region 拖拽功能

        private void ListBox_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }

        private void ListBox_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                foreach (var file in files)
                {
                    try
                    {
                        // 将文件路径放入剪切板
                        Clipboard.SetFileDropList(new System.Collections.Specialized.StringCollection { file });
                    }
                    catch
                    {
                        // 忽略错误
                    }
                }
            }
        }

        #endregion

        #region 窗口控制

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            // 由 App 处理最小化到托盘逻辑
            // 这里不做任何处理，让 App 来决定是否取消关闭
        }

        /// <summary>
        /// 处理关闭窗口（由 App 调用）
        /// </summary>
        /// <returns>如果应该取消关闭（最小化到托盘），返回 true</returns>
        public bool HandleClosing()
        {
            var settings = AppSettings.Load();
            if (settings.MinimizeToTray)
            {
                // 最小化而不是关闭
                WindowState = WindowState.Minimized;
                return true;
            }
            return false;
        }

        #endregion

        #region 设置按钮

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var settingsWindow = new SettingsWindow();
            settingsWindow.Owner = this;
            settingsWindow.ShowDialog();
        }

        #endregion
    }

    /// <summary>
    /// 日期选择对话框
    /// </summary>
    public class DatePickerDialog : Window
    {
        public DateTime SelectedDate { get; private set; } = DateTime.Today;

        public DatePickerDialog()
        {
            Title = "选择日期";
            Width = 300;
            Height = 200;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            WindowStyle = WindowStyle.SingleBorderWindow;
            ResizeMode = ResizeMode.NoResize;
            Background = (Brush)new BrushConverter().ConvertFrom("#F5F5F5");

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var datePicker = new DatePicker
            {
                SelectedDate = DateTime.Today,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 16
            };

            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 10, 0, 0)
            };

            var okButton = new Button
            {
                Content = "确定",
                Width = 80,
                Margin = new Thickness(5),
                IsDefault = true
            };
            okButton.Click += (s, e) =>
            {
                if (datePicker.SelectedDate.HasValue)
                {
                    SelectedDate = datePicker.SelectedDate.Value;
                }
                DialogResult = true;
                Close();
            };

            var cancelButton = new Button
            {
                Content = "取消",
                Width = 80,
                Margin = new Thickness(5),
                IsCancel = true
            };
            cancelButton.Click += (s, e) =>
            {
                DialogResult = false;
                Close();
            };

            buttonPanel.Children.Add(okButton);
            buttonPanel.Children.Add(cancelButton);

            grid.Children.Add(datePicker);
            grid.Children.Add(buttonPanel);
            Grid.SetRow(buttonPanel, 1);

            Content = grid;
        }
    }
}
