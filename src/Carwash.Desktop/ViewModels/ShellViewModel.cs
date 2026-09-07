using System.ComponentModel;
using Carwash.Desktop.Seguridad;

namespace Carwash.Desktop.ViewModels;

// Shell fino: solo navegación. Cada pantalla tiene su VM (SRP).
public sealed class ShellViewModel : INotifyPropertyChanged
{
  public event PropertyChangedEventHandler? PropertyChanged;
  private readonly SesionActual _sesion;
  private object? _current;
  private string _seccion = "Ventas";
  public object? Current { get => _current; set { _current = value; PropertyChanged?.Invoke(this, new(nameof(Current))); } }
  public string UsuarioNombre => string.IsNullOrWhiteSpace(_sesion.Nombre) ? "Usuario" : _sesion.Nombre;
  public string UsuarioCuenta => _sesion.Username;
  public string UsuarioRol => _sesion.Username == "admin" ? "ADMINISTRADOR" : "USUARIO";
  public string Iniciales
  {
    get
    {
      var parts = UsuarioNombre.Split(' ', StringSplitOptions.RemoveEmptyEntries);
      if (parts.Length == 0) return "?";
      if (parts.Length == 1) return parts[0][..Math.Min(2, parts[0].Length)].ToUpper();
      return $"{char.ToUpper(parts[0][0])}{char.ToUpper(parts[1][0])}";
    }
  }
  private void NotifyUsuario()
  {
    PropertyChanged?.Invoke(this, new(nameof(UsuarioNombre)));
    PropertyChanged?.Invoke(this, new(nameof(UsuarioCuenta)));
    PropertyChanged?.Invoke(this, new(nameof(UsuarioRol)));
    PropertyChanged?.Invoke(this, new(nameof(Iniciales)));
  }
  public string Seccion { get => _seccion; private set { _seccion = value; PropertyChanged?.Invoke(this, new(nameof(Seccion))); PropertyChanged?.Invoke(this, new(nameof(Breadcrumb))); NotifyNav(); } }
  public string Breadcrumb => $"Inicio > {Seccion}";
  public bool IsVentas => Seccion == "Ventas";
  public bool IsProductos => Seccion == "Catálogo";
  public bool IsHistorial => Seccion == "Historial";
  public bool IsCaja => Seccion == "Caja";
  public bool IsConfig => Seccion == "Configuración";
  private void NotifyNav()
  {
    PropertyChanged?.Invoke(this, new(nameof(IsVentas)));
    PropertyChanged?.Invoke(this, new(nameof(IsProductos)));
    PropertyChanged?.Invoke(this, new(nameof(IsHistorial)));
    PropertyChanged?.Invoke(this, new(nameof(IsCaja)));
    PropertyChanged?.Invoke(this, new(nameof(IsConfig)));
  }

  public MainViewModel Ventas { get; }
  public ProductosViewModel Productos { get; }
  public HistorialViewModel Historial { get; }
  public CajaViewModel Caja { get; }
  public ConfigViewModel Config { get; }

  public ShellViewModel(MainViewModel ventas, ProductosViewModel productos, HistorialViewModel historial, CajaViewModel caja, ConfigViewModel config, SesionActual sesion)
  {
    Ventas = ventas; Productos = productos; Historial = historial; Caja = caja; Config = config;
    _sesion = sesion;
    _current = Ventas;
    _seccion = "Ventas";
  }

  public void GoInicio() => GoVentas();
  public void GoVentas() { NotifyUsuario(); Current = Ventas; Seccion = "Ventas"; }
  public void GoProductos() { _ = Productos.RecargarAsync(); Current = Productos; Seccion = "Catálogo"; }
  public void GoHistorial() { _ = Historial.RecargarAsync(); Current = Historial; Seccion = "Historial"; }
  public void GoCaja() { _ = Caja.RecargarAsync(); Current = Caja; Seccion = "Caja"; }
  public void GoConfig() { _ = Config.CargarAsync(); Current = Config; Seccion = "Configuración"; }
}
