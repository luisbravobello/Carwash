using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Carwash.Application.Common.Interfaces;
using Carwash.Application.Tickets.CrearTicket;
using Carwash.Desktop.Seguridad;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Carwash.Desktop.ViewModels;

public sealed class RelayCommand(Action<object?> exec, Func<object?, bool>? can = null) : ICommand
{
  public event EventHandler? CanExecuteChanged;
  public bool CanExecute(object? p) => can?.Invoke(p) ?? true;
  public void Execute(object? p) => exec(p);
  public void RaiseCanExecute() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}

public sealed class ProductoVm(int id, string nombre, decimal precio)
{
  public int Id { get; } = id;
  public string Nombre { get; } = nombre;
  public decimal Precio { get; } = precio;
  public string Display => $"{Nombre} - RD${Precio:N2}";
}

public sealed class LavadorVm(int id, string nombre)
{
  public int Id { get; } = id;
  public string Nombre { get; } = nombre;
}

public sealed class LineaVm(int productoId, string producto, int cantidad, decimal precio)
{
  public int ProductoId { get; } = productoId;
  public string Producto { get; } = producto;
  public int Cantidad { get; set; } = cantidad;
  public decimal Precio { get; } = precio;
  public decimal Subtotal => Cantidad * Precio;
}

// MVVM fino: la Vista no conoce EF ni Epson, solo este ViewModel. SRP: solo estado de ventas.
public sealed class MainViewModel : INotifyPropertyChanged
{
  private readonly IServiceProvider _root;
  private readonly SesionActual _sesion;
  public event PropertyChangedEventHandler? PropertyChanged;
  private void Set<T>(ref T f, T v, [CallerMemberName] string? n = null) { f = v; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n)); }

  private string _nombre = "";
  private string _veh = "";
  private ProductoVm? _sel;
  private int _cant = 1;
  private int _metodo = 1;
  private string _recibido = "";
  private bool _pendiente;
  public string MontoRecibido { get => _recibido; set { Set(ref _recibido, value); RefreshCambio(); } }
  public bool EsPendiente { get => _pendiente; set { Set(ref _pendiente, value); Cobrar.RaiseCanExecute(); RefreshCambio(); } }
  private void RefreshCambio()
  {
    PropertyChanged?.Invoke(this, new(nameof(CambioTexto)));
    PropertyChanged?.Invoke(this, new(nameof(CambioOk)));
    PropertyChanged?.Invoke(this, new(nameof(CambioFalta)));
  }
  public string CambioTexto
  {
    get
    {
      if (EsPendiente) return "Queda pendiente: paga al retirar el vehículo.";
      if (!decimal.TryParse(_recibido, out var r)) return "Escribe lo recibido para ver el cambio.";
      var c = r - Total;
      return c < 0 ? $"Faltan RD${-c:N2}." : $"Cambio: RD${c:N2}.";
    }
  }
  private string _msg = "";

  public string ClienteNombre { get => _nombre; set => Set(ref _nombre, value); }
  public string VehiculoDescripcion { get => _veh; set => Set(ref _veh, value); }
  public ProductoVm? Seleccionado { get => _sel; set { Set(ref _sel, value); Cobrar.RaiseCanExecute(); Agregar.RaiseCanExecute(); } }
  public int Cantidad { get => _cant; set => Set(ref _cant, Math.Max(1, value)); }
  public int MetodoPagoId { get => _metodo; set => Set(ref _metodo, value); }
  public bool CambioOk => !EsPendiente && decimal.TryParse(_recibido, out var r) && r >= Total && Total > 0;
  public bool CambioFalta => !EsPendiente && decimal.TryParse(_recibido, out var r2) && r2 < Total;
  public string Mensaje { get => _msg; set => Set(ref _msg, value); }
  public decimal Total => Carrito.Sum(l => l.Subtotal);

  public ObservableCollection<ProductoVm> Productos { get; } = [];
  public ObservableCollection<LavadorVm> Lavadores { get; } = [];
  private LavadorVm? _lav;
  public LavadorVm? LavadorSel { get => _lav; set => Set(ref _lav, value); }
  public ObservableCollection<LineaVm> Carrito { get; } = [];
  private LineaVm? _lineaSel;
  public LineaVm? LineaSeleccionada { get => _lineaSel; set { Set(ref _lineaSel, value); Quitar.RaiseCanExecute(); } }
  public RelayCommand Agregar { get; }
  public RelayCommand Quitar { get; }
  public RelayCommand Cobrar { get; }
  public RelayCommand Limpiar { get; }

  public MainViewModel(IServiceProvider root, SesionActual sesion)
  {
    _root = root;
    _sesion = sesion;
    Agregar = new(_ => AgregarLinea(), _ => Seleccionado is not null);
    Quitar = new(p => { var l = p as LineaVm ?? LineaSeleccionada; if (l is not null) { Carrito.Remove(l); LineaSeleccionada = null; OnProp(); } }, p => (p as LineaVm) is not null || LineaSeleccionada is not null);
    Cobrar = new(async _ => await CobrarAsync(), _ => Carrito.Count > 0);
    Limpiar = new(_ => { Carrito.Clear(); Mensaje = ""; OnProp(); });
    _ = CargarAsync();
  }

  private void OnProp() { PropertyChanged?.Invoke(this, new(nameof(Total))); RefreshCambio(); Cobrar.RaiseCanExecute(); }

  private async Task CargarAsync()
  {
    try
    {
      using var s = _root.CreateScope();
      var db = s.ServiceProvider.GetRequiredService<ICarwashDbContext>();
      var ps = await db.Productos.Where(p => p.Activo).OrderBy(p => p.Nombre).ToListAsync();
      Productos.Clear();
      foreach (var p in ps) Productos.Add(new ProductoVm(p.Id, p.Nombre, p.Precio));
      var lavs = await s.ServiceProvider.GetRequiredService<Carwash.Application.Personal.Lavadores.ListarLavadoresHandler>().Handle(true);
      Lavadores.Clear();
      foreach (var l in lavs) Lavadores.Add(new LavadorVm(l.Id, l.Nombre));
      Mensaje = $"{ps.Count} productos cargados.";
    }
    catch (Exception ex) { Mensaje = "Error cargando: " + ex.GetBaseException().Message; }
  }

  private void AgregarLinea()
  {
    if (Seleccionado is null) return;
    var ex = Carrito.FirstOrDefault(l => l.ProductoId == Seleccionado.Id);
    if (ex is not null) ex.Cantidad += Cantidad;
    else Carrito.Add(new LineaVm(Seleccionado.Id, Seleccionado.Nombre, Cantidad, Seleccionado.Precio));
    OnProp();
  }

  private async Task CobrarAsync()
  {
    try
    {
      using var s = _root.CreateScope();
      var sp = s.ServiceProvider;
      var db = sp.GetRequiredService<ICarwashDbContext>();
      var crear = sp.GetRequiredService<CrearTicketHandler>();

      var usuarioId = _sesion.Activa ? _sesion.UsuarioId : (await db.Usuarios.FirstOrDefaultAsync())?.Id;
      if (usuarioId is null) { Mensaje = "Error: no hay usuarios. Reinicia la app."; return; }
      decimal? recibido = decimal.TryParse(MontoRecibido, out var rr) ? rr : null;
      var res = await crear.Handle(new CrearTicketCommand(usuarioId.Value, MetodoPagoId,
        null,
        null, Carrito.Select(l => new CrearTicketLinea(l.ProductoId, l.Cantidad)).ToList(),
        string.IsNullOrWhiteSpace(ClienteNombre) ? null : ClienteNombre.Trim(),
        string.IsNullOrWhiteSpace(VehiculoDescripcion) ? null : VehiculoDescripcion.Trim(),
        EsPendiente, recibido, LavadorSel?.Id));
      if (!res.IsSuccess) { Mensaje = "Error: " + res.Error; return; }
      var total = Total;
      var extra = EsPendiente ? "Queda pendiente, paga al retirar."
        : (MetodoPagoId == 1 && recibido.HasValue ? $" Recibido RD${recibido:N2}, cambio RD${recibido - total:N2}." : "");
      Mensaje = res.Value!.Impreso
        ? $"Ticket #{res.Value.Numero} impreso. Total RD${total:N2}.{extra}"
        : $"Ticket #{res.Value.Numero} GUARDADO pero NO salió en papel. Total RD${total:N2}.{extra} {res.Value.PrintDetalle}";
      if (res.IsSuccess) { Carrito.Clear(); ClienteNombre = ""; VehiculoDescripcion = ""; MontoRecibido = ""; EsPendiente = false; LavadorSel = null; OnProp(); }
    }
    catch (Exception ex) { Mensaje = "Error: " + ex.GetBaseException().Message; }
  }
}
