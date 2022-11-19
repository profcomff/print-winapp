using System.Windows;

namespace PrinterApp
{
    public class PrinterViewModel : NotifyPropertyChangeBase
    {
        private bool _downloadNotInProgress = true;
        private string _codeTextBoxText = "";
        private string _errorTextBlockText = "";
        private Visibility _errorTextBlockVisibility = Visibility.Collapsed;
        private double _progressBarValue;
        private Visibility _progressBarVisibility = Visibility.Collapsed;
        private string _compliment = "";

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
    }
}