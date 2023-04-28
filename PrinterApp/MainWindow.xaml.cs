using Serilog;
using System;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace PrinterApp;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly Regex _regex = new("[a-zA-Z0-9]{0,8}");
    private readonly Random _random = new(Guid.NewGuid().GetHashCode());
    private const int FlakesCount = 12;
    private readonly double[] _flakesTargetsCanvasLeft = new double[FlakesCount];
    private readonly PrinterModel _printerModel;

    public MainWindow(PrinterModel printerModel)
    {
        _printerModel = printerModel;

        for (var i = 0; i < FlakesCount; i++)
        {
            _printerModel.PrinterViewModel.FlakesCanvasTop.Add(0);
            _printerModel.PrinterViewModel.FlakesCanvasLeft.Add(0);
        }

        Loaded += (_, _) =>
        {
            MoveFocus(new TraversalRequest(FocusNavigationDirection.First));

            SetNewYearTimer();
            for (var i = 0; i < FlakesCount; i++)
            {
                _printerModel.PrinterViewModel.FlakesCanvasLeft[i] =
                    _random.Next(0, (int)RootCanvas.Width - 25);
                _printerModel.PrinterViewModel.FlakesCanvasTop[i] =
                    _random.Next((int)RootCanvas.Height * -1, -25);
                _flakesTargetsCanvasLeft[i] = RootCanvas.Width * _random.NextDouble();
            }

            _newYearDispatcherTimer.Tick += (_, _) =>
            {
                for (var i = 0; i < FlakesCount; i++)
                {
                    if (_printerModel.PrinterViewModel.FlakesCanvasTop[i] > RootCanvas.Height)
                    {
                        _printerModel.PrinterViewModel.FlakesCanvasTop[i] = -25;
                    }
                    else
                    {
                        _printerModel.PrinterViewModel.FlakesCanvasTop[i] += 0.3;
                    }

                    if (_printerModel.PrinterViewModel.FlakesCanvasLeft[i] > RootCanvas.Width)
                    {
                        _printerModel.PrinterViewModel.FlakesCanvasLeft[i] =
                            RootCanvas.Width -
                            _printerModel.PrinterViewModel.FlakesCanvasLeft[i];
                    }
                    else if (_printerModel.PrinterViewModel.FlakesCanvasLeft[i] < 0)
                    {
                        _printerModel.PrinterViewModel.FlakesCanvasLeft[i] += RootCanvas.Width;
                    }
                    else
                    {
                        if (Math.Abs(_printerModel.PrinterViewModel.FlakesCanvasLeft[i] -
                                     _flakesTargetsCanvasLeft[i]) < 0.4)
                        {
                            _flakesTargetsCanvasLeft[i] =
                                RootCanvas.Width * _random.NextDouble();
                        }

                        _printerModel.PrinterViewModel.FlakesCanvasLeft[i] +=
                            _printerModel.PrinterViewModel.FlakesCanvasLeft[i] <
                            _flakesTargetsCanvasLeft[i]
                                ? 0.3
                                : -0.3;
                    }
                }
            };
        };
        DataContext = _printerModel.PrinterViewModel;
        InitializeComponent();
        //what this?
        if (40 + Height < SystemParameters.PrimaryScreenHeight)
            Top = 40;
        Left = SystemParameters.PrimaryScreenWidth - 20 - Width;
#if DEBUG
        WindowState = WindowState.Normal;
        WindowStyle = WindowStyle.SingleBorderWindow;
        ResizeMode = ResizeMode.CanResize;
        Topmost = false;
        ShowInTaskbar = true;
#endif
        _printerModel.PrintAsyncCompleteEvent += () => { CodeBox.Focus(); };
        Marketing.MainWindowLoaded();
    }

    private bool IsTextAllowed(string text)
    {
        Log.Debug(
            $"{GetType().Name} {MethodBase.GetCurrentMethod()?.Name}: Regex.IsMatch({text}) = {text.Equals(_regex.Match(text).Value)}");
        return text.Equals(_regex.Match(text).Value);
    }

    private void TextBox_OnPreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        if (_printerModel.PrinterViewModel.ErrorTextBlockText != "")
        {
            _printerModel.PrinterViewModel.ErrorTextBlockVisibility = Visibility.Collapsed;
            _printerModel.PrinterViewModel.ErrorTextBlockText = "";
        }

        e.Handled = !IsTextAllowed((sender as TextBox)?.Text + e.Text);
    }

    private void TextBoxPasting(object sender, DataObjectPastingEventArgs e)
    {
        if (e.DataObject.GetDataPresent(typeof(string)))
        {
            var text = (string)e.DataObject.GetData(typeof(string));
            if (!IsTextAllowed(text))
            {
                e.CancelCommand();
            }
        }
        else
        {
            e.CancelCommand();
        }
    }

    private void TextBox_OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Back)
        {
            if (_printerModel.PrinterViewModel.ErrorTextBlockText != "")
            {
                _printerModel.PrinterViewModel.ErrorTextBlockVisibility = Visibility.Collapsed;
                _printerModel.PrinterViewModel.ErrorTextBlockText = "";
            }
        }
    }

    private void Print_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button button)
        {
            _printerModel.PrintAsync(button.Name == "Print2");
        }
    }

    private void UIElement_OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        _printerModel.PrintAsync( /*button.Name == "Print2"*/false);
        e.Handled = true;
    }

    private readonly DispatcherTimer _newYearDispatcherTimer =
        new() { Interval = new TimeSpan(0, 0, 0, 0, 23) };

    private void SetNewYearTimer()
    {
        NewYearTimerOnElapsed(null, null!);
        var newYearTimer = new Timer();
        newYearTimer.Elapsed += NewYearTimerOnElapsed;
        //check every 12 hours
        newYearTimer.Interval = 12 * 60 * 60 * 1000;
        newYearTimer.Start();
    }

    private void NewYearTimerOnElapsed(object? sender, ElapsedEventArgs e)
    {
        if (DateTime.Now > new DateTime(DateTime.Now.Year, 11, 30) &&
            DateTime.Now < new DateTime(DateTime.Now.Year + 1, 2, 1))
        {
            if (!_newYearDispatcherTimer.IsEnabled)
            {
                _printerModel.PrinterViewModel.FlakesVisibility = Visibility.Visible;
                _newYearDispatcherTimer.Start();
            }
        }
        else
        {
            if (_newYearDispatcherTimer.IsEnabled)
            {
                _printerModel.PrinterViewModel.FlakesVisibility = Visibility.Collapsed;
                _newYearDispatcherTimer.Stop();
            }
        }
    }
}