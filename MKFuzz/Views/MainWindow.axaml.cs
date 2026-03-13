using Avalonia.Controls;
using Avalonia.Platform.Storage;
using MKFuzz.ViewModels;

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
                if (vm.Tabs[0] is ProjectSetupViewModel setupVm)
                {
                    setupVm.StorageProvider = this.StorageProvider;
                }
            }
        };
    }
}