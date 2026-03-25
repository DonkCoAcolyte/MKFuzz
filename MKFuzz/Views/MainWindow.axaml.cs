using Avalonia.Controls;
using Avalonia.Platform.Storage;
using MKFuzz.ViewModels;
using System;
using System.Threading.Tasks;

namespace MKFuzz.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Opened += (s, e) =>
        {
            if (DataContext is MainWindowViewModel vm)
            {
                vm.StorageProvider = this.StorageProvider;
                if (vm.Tabs[0] is ProjectSetupViewModel setupVm)
                {
                    setupVm.StorageProvider = this.StorageProvider;
                }
            }
        };
        Closing += async (s, e) =>
        {
            if (DataContext is IAsyncDisposable asyncDisposable)
            {
                await asyncDisposable.DisposeAsync();
            }
        };
    }
}