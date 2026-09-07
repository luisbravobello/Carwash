using System.Windows;
using System.Windows.Input;
using Carwash.Desktop.ViewModels;

namespace Carwash.Desktop;

public partial class LoginWindow : Wpf.Ui.Controls.FluentWindow
{
  private readonly LoginViewModel _vm;
  public LoginWindow(LoginViewModel vm)
  {
    InitializeComponent();
    _vm = vm;
    DataContext = _vm;
    Loaded += (_, _) => Pass.Focus();
  }
  private async void DoEntrar(object s, RoutedEventArgs e) => await EntrarAsync();
  private async void Pass_KeyDown(object s, KeyEventArgs e)
  {
    if (e.Key == Key.Enter) await EntrarAsync();
  }
  private void CerrarApp(object s, RoutedEventArgs e)
  {
    DialogResult = false;
    Close();
  }
  private void Pass_Changed(object s, RoutedEventArgs e)
  {
    PassMark.Visibility = string.IsNullOrEmpty(Pass.Password) ? Visibility.Visible : Visibility.Collapsed;
  }
  private async Task EntrarAsync()
  {
    if (await _vm.LoginAsync(Pass.Password))
    {
      DialogResult = true;
      Close();
    }
  }
}
