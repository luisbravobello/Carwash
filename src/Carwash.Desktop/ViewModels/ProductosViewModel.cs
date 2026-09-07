using System.Collections.ObjectModel;
using System.ComponentModel;
using Carwash.Application.Catalogo.Productos;
using Microsoft.Extensions.DependencyInjection;

namespace Carwash.Desktop.ViewModels;

public sealed class ProductoRow(int id, string nombre, decimal precio, bool activo, int stock)
{
  public int Id { get; } = id;
  public string Nombre { get; } = nombre;
  public decimal Precio { get; } = precio;
  public bool Activo { get; } = activo;
  public int Stock { get; } = stock;
  public string Estado => Activo ? "Activo" : "Inactivo";
}

public sealed class ProductosViewModel : INotifyPropertyChanged
{
  public event PropertyChangedEventHandler? PropertyChanged;
  private readonly IServiceProvider _root;
  private void Notify(string n) => PropertyChanged?.Invoke(this, new(n));
  private string _msg = "", _nn = "", _np = "", _ns = "";
  private string _en = "", _ep = "", _es = "";
  private ProductoRow? _sel;

  public string Mensaje { get => _msg; set { _msg = value; Notify(nameof(Mensaje)); } }
  public string NuevoNombre { get => _nn; set { _nn = value; Notify(nameof(NuevoNombre)); CrearCmd.RaiseCanExecute(); } }
  public string NuevoPrecio { get => _np; set { _np = value; Notify(nameof(NuevoPrecio)); CrearCmd.RaiseCanExecute(); } }
  public string NuevoStock { get => _ns; set { _ns = value; Notify(nameof(NuevoStock)); } }
  // Formulario de edición (lápiz): solo se edita aquí, nunca directo en la tabla.
  public string EditNombre { get => _en; set { _en = value; Notify(nameof(EditNombre)); GuardarCmd.RaiseCanExecute(); } }
  public string EditPrecio { get => _ep; set { _ep = value; Notify(nameof(EditPrecio)); GuardarCmd.RaiseCanExecute(); } }
  public string EditStock { get => _es; set { _es = value; Notify(nameof(EditStock)); } }
  public ProductoRow? Seleccionado { get => _sel; set { _sel = value; Notify(nameof(Seleccionado)); EditarCmd.RaiseCanExecute(); ToggleCmd.RaiseCanExecute(); } }
  public ObservableCollection<ProductoRow> Items { get; } = [];
  public RelayCommand RecargarCmd { get; }
  public RelayCommand CrearCmd { get; }
  public RelayCommand EditarCmd { get; }
  public RelayCommand GuardarCmd { get; }
  public RelayCommand ToggleCmd { get; }

  public ProductosViewModel(IServiceProvider root)
  {
    _root = root;
    RecargarCmd = new(async _ => await RecargarAsync());
    CrearCmd = new(async _ => await CrearAsync(), _ => !string.IsNullOrWhiteSpace(NuevoNombre) && decimal.TryParse(NuevoPrecio, out var p) && p >= 0);
    EditarCmd = new(_ => CargarEdicion(), _ => Seleccionado is not null);
    GuardarCmd = new(async _ => await GuardarAsync(), _ => Seleccionado is not null && !string.IsNullOrWhiteSpace(EditNombre) && decimal.TryParse(EditPrecio, out var p) && p >= 0);
    ToggleCmd = new(async _ => await ToggleAsync(), _ => Seleccionado is not null);
  }

  public async Task RecargarAsync()
  {
    try
    {
      using var s = _root.CreateScope();
      var list = await s.ServiceProvider.GetRequiredService<ListarProductosHandler>().Handle();
      Items.Clear();
      foreach (var p in list) Items.Add(new ProductoRow(p.Id, p.Nombre, p.Precio, p.Activo, p.Stock));
      Mensaje = $"{Items.Count} productos. Selecciona una fila y dale al lápiz para editar.";
    }
    catch (Exception ex) { Mensaje = "Error: " + ex.GetBaseException().Message; }
  }

  private async Task CrearAsync()
  {
    try
    {
      using var s = _root.CreateScope();
      var h = s.ServiceProvider.GetRequiredService<CrearProductoHandler>();
      if (!decimal.TryParse(NuevoPrecio, out var precio)) { Mensaje = "Precio inválido."; return; }
      int.TryParse(NuevoStock, out var stock);
      var r = await h.Handle(new(1, NuevoNombre.Trim(), precio, stock > 0, stock));
      Mensaje = r.IsSuccess ? $"Creado Id {r.Value}." : r.Error!;
      if (r.IsSuccess) { NuevoNombre = ""; NuevoPrecio = ""; NuevoStock = ""; await RecargarAsync(); }
    }
    catch (Exception ex) { Mensaje = "Error: " + ex.GetBaseException().Message; }
  }

  private void CargarEdicion()
  {
    if (Seleccionado is null) return;
    EditNombre = Seleccionado.Nombre;
    EditPrecio = Seleccionado.Precio.ToString("N2");
    EditStock = Seleccionado.Stock.ToString();
    Mensaje = $"Editando: {Seleccionado.Nombre}";
  }

  private async Task GuardarAsync()
  {
    if (Seleccionado is null) return;
    if (!decimal.TryParse(EditPrecio, out var precio)) { Mensaje = "Precio inválido."; return; }
    int.TryParse(EditStock, out var stock);
    try
    {
      using var s = _root.CreateScope();
      var h = s.ServiceProvider.GetRequiredService<ActualizarProductoHandler>();
      var r = await h.Handle(new(Seleccionado.Id, EditNombre.Trim(), precio, Seleccionado.Activo, stock > 0, stock));
      Mensaje = r.IsSuccess ? "Guardado." : r.Error!;
      if (r.IsSuccess) await RecargarAsync();
    }
    catch (Exception ex) { Mensaje = "Error: " + ex.GetBaseException().Message; }
  }

  private async Task ToggleAsync()
  {
    if (Seleccionado is null) return;
    try
    {
      using var s = _root.CreateScope();
      var h = s.ServiceProvider.GetRequiredService<ActualizarProductoHandler>();
      var r = await h.Handle(new(Seleccionado.Id, Seleccionado.Nombre, Seleccionado.Precio, !Seleccionado.Activo, Seleccionado.Stock > 0, Seleccionado.Stock));
      Mensaje = r.IsSuccess ? (Seleccionado.Activo ? "Desactivado." : "Activado.") : r.Error!;
      if (r.IsSuccess) await RecargarAsync();
    }
    catch (Exception ex) { Mensaje = "Error: " + ex.GetBaseException().Message; }
  }
}
