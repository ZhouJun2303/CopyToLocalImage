using System;
using System.Windows;
using System.Windows.Forms;
using CopyToLocalImage.Models;

namespace CopyToLocalImage
{
    /// <summary>
    /// SettingsWindow.xaml 的交互逻辑
    /// </summary>
    public partial class SettingsWindow : Window
    {
        private AppSettings _settings;
        public event Action? SettingsSaved;

        public SettingsWindow()
        {
            InitializeComponent();
            _settings = AppSettings.Load();
            LoadSettings();
        }

        private void LoadSettings()
        {
            SavePathTextBox.Text = _settings.SavePath;
            MinimizeToTrayCheckBox.IsChecked = _settings.MinimizeToTray;
            StartMinimizedCheckBox.IsChecked = _settings.StartMinimized;
            EnableHotkeyCheckBox.IsChecked = _settings.EnableHotkey;
            HotkeyTextBox.Text = _settings.Hotkey;
            AutoCleanDaysTextBox.Text = _settings.AutoCleanDays.ToString();
            DarkThemeCheckBox.IsChecked = _settings.UseDarkTheme;
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new FolderBrowserDialog
            {
                Description = "选择图片保存路径",
                SelectedPath = _settings.SavePath,
                ShowNewFolderButton = true
            };

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                SavePathTextBox.Text = dialog.SelectedPath;
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            _settings.SavePath = SavePathTextBox.Text;
            _settings.MinimizeToTray = MinimizeToTrayCheckBox.IsChecked == true;
            _settings.StartMinimized = StartMinimizedCheckBox.IsChecked == true;
            _settings.EnableHotkey = EnableHotkeyCheckBox.IsChecked == true;
            _settings.Hotkey = HotkeyTextBox.Text;

            if (int.TryParse(AutoCleanDaysTextBox.Text, out var days))
            {
                _settings.AutoCleanDays = days;
            }

            _settings.UseDarkTheme = DarkThemeCheckBox.IsChecked == true;

            _settings.Save();

            System.Windows.MessageBox.Show("设置已保存", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            SettingsSaved?.Invoke();
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
