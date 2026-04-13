using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Collectibles.SyncTool.Models;
using Collectibles.SyncTool.ViewModels;

namespace Collectibles.SyncTool;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Loaded += MainWindow_Loaded;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        // Sync PasswordBox with ViewModel (PasswordBox doesn't support binding)
        if (DataContext is MainViewModel vm)
        {
            if (!string.IsNullOrEmpty(vm.ApiKey))
            {
                ApiKeyBox.Password = vm.ApiKey;
            }

            // Track when the GridSplitter changes the column width
            DependencyPropertyDescriptor
                .FromProperty(ColumnDefinition.WidthProperty, typeof(ColumnDefinition))
                .AddValueChanged(PreviewColumn, (_, _) =>
                {
                    if (PreviewColumn.ActualWidth > 0)
                    {
                        vm.PreviewPanelWidth = PreviewColumn.ActualWidth;
                    }
                });

            // Collapse/expand preview column based on visibility
            vm.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(MainViewModel.IsPreviewPanelVisible))
                {
                    SetPreviewColumnVisible(vm.IsPreviewPanelVisible, vm.PreviewPanelWidth);
                }
            };

            // Set initial state
            SetPreviewColumnVisible(vm.IsPreviewPanelVisible, vm.PreviewPanelWidth);
        }
    }

    private void ApiKeyBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.ApiKey = ApiKeyBox.Password;
        }
    }

    private async void ActionButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: SyncItemViewModel item } && DataContext is MainViewModel vm)
        {
            if (item.Status == SyncStatus.ToUpload)
            {
                await vm.UploadSingleCommand.ExecuteAsync(item);
            }
            else if (item.Status == SyncStatus.ServerOnly)
            {
                await vm.DeleteSingleCommand.ExecuteAsync(item);
            }
        }
    }

    private async void DownloadButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: SyncItemViewModel item } && DataContext is MainViewModel vm)
        {
            await vm.DownloadSingleCommand.ExecuteAsync(item);
        }
    }

    private async void ServerDeleteButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: SyncItemViewModel item } && DataContext is MainViewModel vm)
        {
            await vm.DeleteSingleCommand.ExecuteAsync(item);
        }
    }

    private async void CopyButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: SyncItemViewModel item } && DataContext is MainViewModel vm)
        {
            await vm.CopySingleCommand.ExecuteAsync(item);
        }
    }

    private async void MoveButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: SyncItemViewModel item } && DataContext is MainViewModel vm)
        {
            await vm.MoveSingleCommand.ExecuteAsync(item);
        }
    }

    private void FileGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (FileGrid.CurrentItem is SyncItemViewModel item && item.LocalFilePath != null)
        {
            OpenFileWithDefault(item.LocalFilePath);
            e.Handled = true;
        }
    }

    private void OpenFile_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem { DataContext: SyncItemViewModel item } && item.LocalFilePath != null)
        {
            OpenFileWithDefault(item.LocalFilePath);
        }
    }

    private void OpenContainingFolder_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem { DataContext: SyncItemViewModel item } && item.LocalFilePath != null)
        {
            Process.Start("explorer.exe", $"/select,\"{item.LocalFilePath}\"");
        }
    }

    private static void OpenFileWithDefault(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return;
        }

        Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
    }

    private void FileGrid_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Space)
        {
            return;
        }

        var selectedItems = FileGrid.SelectedItems.OfType<SyncItemViewModel>().ToList();
        if (selectedItems.Count == 0)
        {
            return;
        }

        // Toggle based on the first selected item's state
        var newValue = !selectedItems[0].IsSelected;
        foreach (var item in selectedItems)
        {
            item.IsSelected = newValue;
        }

        e.Handled = true;
    }

    private void SetPreviewColumnVisible(bool visible, double width)
    {
        if (visible)
        {
            PreviewColumn.MinWidth = 200;
            PreviewColumn.MaxWidth = 600;
            PreviewColumn.Width = new GridLength(width);
        }
        else
        {
            PreviewColumn.MinWidth = 0;
            PreviewColumn.MaxWidth = 0;
            PreviewColumn.Width = new GridLength(0);
        }
    }

    private void FileGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is not MainViewModel vm) return;

        vm.SelectedPreviewItem = FileGrid.CurrentItem as SyncItemViewModel;
    }

    private void PreviewImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.ToggleZoomCommand.Execute(null);
        }
    }
}
