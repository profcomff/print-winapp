using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PrinterApp;

public class NotifyPropertyChangeBase : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string prop = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
    }
}