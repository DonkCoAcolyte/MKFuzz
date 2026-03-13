using CommunityToolkit.Mvvm.ComponentModel;

namespace MKFuzz.ViewModels;

public class ViewModelBase : ObservableObject
{
    public virtual string Header { get; } = "";
}