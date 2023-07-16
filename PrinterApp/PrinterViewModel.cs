using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;

namespace PrinterApp;

public class PrinterViewModel : NotifyPropertyChangeBase
{
    private bool _downloadNotInProgress = true;
    private string _codeTextBoxText = "";
    private string _errorTextBlockText = "";
    private Visibility _errorTextBlockVisibility = Visibility.Collapsed;
    private Geometry _printQr = Geometry.Empty;
    private Visibility _printQrVisibility = Visibility.Visible;
    private string _compliment = "";
    private Visibility _flakesVisibility = Visibility.Collapsed;
    private ObservableCollection<double> _flakesCanvasTop = new();
    private ObservableCollection<double> _flakesCanvasLeft = new();

    public bool DownloadNotInProgress
    {
        get => _downloadNotInProgress;
        set
        {
            if (value == _downloadNotInProgress) return;
            _downloadNotInProgress = value;
            OnPropertyChanged();
        }
    }

    public string CodeTextBoxText
    {
        get => _codeTextBoxText;
        set
        {
            if (value == _codeTextBoxText) return;
            _codeTextBoxText = value.ToUpper();
            OnPropertyChanged();
        }
    }

    public string ErrorTextBlockText
    {
        get => _errorTextBlockText;
        set
        {
            if (value == _errorTextBlockText) return;
            _errorTextBlockText = value;
            OnPropertyChanged();
        }
    }

    public Visibility ErrorTextBlockVisibility
    {
        get => _errorTextBlockVisibility;
        set
        {
            if (value == _errorTextBlockVisibility) return;
            _errorTextBlockVisibility = value;
            OnPropertyChanged();
        }
    }

    public Geometry PrintQr
    {
        get => _printQr;
        set
        {
            if (Equals(value, _printQr)) return;
            _printQr = value;
            OnPropertyChanged();
        }
    }

    public Visibility PrintQrVisibility
    {
        get => _printQrVisibility;
        set
        {
            if (value == _printQrVisibility) return;
            _printQrVisibility = value;
            OnPropertyChanged();
        }
    }

    public string Compliment
    {
        get => _compliment;
        set
        {
            if (value == _compliment) return;
            _compliment = value;
            OnPropertyChanged();
        }
    }

    public Visibility FlakesVisibility
    {
        get => _flakesVisibility;
        set
        {
            if (value == _flakesVisibility) return;
            _flakesVisibility = value;
            OnPropertyChanged();
        }
    }

    public ObservableCollection<double> FlakesCanvasTop
    {
        get => _flakesCanvasTop;
        set
        {
            if (Equals(value, _flakesCanvasTop)) return;
            _flakesCanvasTop = value;
            OnPropertyChanged();
        }
    }

    public ObservableCollection<double> FlakesCanvasLeft
    {
        get => _flakesCanvasLeft;
        set
        {
            if (Equals(value, _flakesCanvasLeft)) return;
            _flakesCanvasLeft = value;
            OnPropertyChanged();
        }
    }
}