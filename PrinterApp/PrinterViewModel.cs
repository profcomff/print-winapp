using System.Windows;

namespace PrinterApp
{
    public class PrinterViewModel : NotifyPropertyChangeBase
    {
        private bool _textBlockEnabled = true;
        private string _codeTextBoxText = "";
        private string _errorTextBlockText = "";
        private double _progressBarValue;
        private Visibility _progressBarVisibility = Visibility.Collapsed;

        public bool TextBlockEnabled
        {
            get => _textBlockEnabled;
            set
            {
                if (value == _textBlockEnabled) return;
                _textBlockEnabled = value;
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

        public double ProgressBarValue
        {
            get => _progressBarValue;
            set
            {
                if (value.Equals(_progressBarValue)) return;
                _progressBarValue = value;
                OnPropertyChanged();
            }
        }

        public Visibility ProgressBarVisibility
        {
            get => _progressBarVisibility;
            set
            {
                if (value == _progressBarVisibility) return;
                _progressBarVisibility = value;
                OnPropertyChanged();
            }
        }
    }
}