using System.Windows;
using Carwash.Desktop.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace Carwash.Desktop;

public partial class MainWindow : Wpf.Ui.Controls.FluentWindow
{
  private readonly ShellViewModel _shell;
  public MainWindow(ShellViewModel shell)
  {
    InitializeComponent();
    _shell = shell;
    DataContext = _shell;
  }
  private void GoInicio(object s, RoutedEventArgs e) => _shell.GoInicio();
  private void GoVentas(object s, RoutedEventArgs e) => _shell.GoVentas();
  private void GoProductos(object s, RoutedEventArgs e) => _shell.GoProductos();
  private void GoHistorial(object s, RoutedEventArgs e) => _shell.GoHistorial();
  private void GoCaja(object s, RoutedEventArgs e) => _shell.GoCaja();
  private void GoConfig(object s, RoutedEventArgs e) => _shell.GoConfig();
  private void CerrarSesion(object s, RoutedEventArgs e)
  {
    App.Services.GetRequiredService<Carwash.Desktop.Seguridad.SesionActual>().Cerrar();
    Hide();
    var login = App.Services.GetRequiredService<LoginWindow>();
    login.Owner = this;
    login.WindowStartupLocation = WindowStartupLocation.CenterOwner;
    if (login.ShowDialog() != true) { Close(); return; }
    _shell.GoVentas();
    Show();
    Focus();
  }
}
