using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Carwash.Application.Negocio.Config;
using Carwash.Application.Usuarios.Gestion;
using Microsoft.Extensions.DependencyInjection;

namespace Carwash.Desktop.ViewModels;

public sealed class UsuarioRow(int id, string nombre, string username, bool activo)
{
  public int Id { get; } = id;
  public string Nombre { get; } = nombre;
  public string Username { get; } = username;
  public string Estado => activo ? "Activo" : "Inactivo";
}

public sealed class LavadorRow(int id, string nombre, bool activo)
{
  public int Id { get; } = id;
  public string Nombre { get; } = nombre;
  public string Estado => activo ? "Activo" : "Inactivo";
}

public sealed class ConfigViewModel : INotifyPropertyChanged
{
  public event PropertyChangedEventHandler? PropertyChanged;
  private readonly IServiceProvider _root;
  private void Notify([CallerMemberName] string? n = null) => PropertyChanged?.Invoke(this, new(n));
  private string _nombre = "", _tel = "", _dir = "", _msg = "";
  private string _un = "", _uu = "", _uc = "";
  private string _en = "", _eu = "", _ec = "";
  private UsuarioRow? _sel;
  public string Nombre { get => _nombre; set { _nombre = value; Notify(); } }
  public string Telefono { get => _tel; set { _tel = value; Notify(); } }
  public string Direccion { get => _dir; set { _dir = value; Notify(); } }
  public string Mensaje { get => _msg; set { _msg = value; Notify(); } }
  public string NuevoNombre { get => _un; set { _un = value; Notify(); CrearUsuarioCmd.RaiseCanExecute(); } }
  public string NuevoUser { get => _uu; set { _uu = value; Notify(); CrearUsuarioCmd.RaiseCanExecute(); } }
  public string NuevoClave { get => _uc; set { _uc = value; Notify(); CrearUsuarioCmd.RaiseCanExecute(); } }
  public string EditNombre { get => _en; set { _en = value; Notify(); GuardarUsuarioCmd.RaiseCanExecute(); } }
  public string EditUser { get => _eu; set { _eu = value; Notify(); GuardarUsuarioCmd.RaiseCanExecute(); } }
  public string NuevaClave { get => _ec; set { _ec = value; Notify(); ClaveCmd.RaiseCanExecute(); } }
  public UsuarioRow? Seleccionado
  {
    get => _sel;
    set { _sel = value; Notify(); EditarCmd.RaiseCanExecute(); ToggleCmd.RaiseCanExecute(); }
  }
  public ObservableCollection<UsuarioRow> Usuarios { get; } = [];
  public ObservableCollection<LavadorRow> Lavadores { get; } = [];
  private LavadorRow? _lsel;
  private string _ln = "", _le = "";
  public LavadorRow? LavadorSel { get => _lsel; set { _lsel = value; Notify(); EditarLavCmd.RaiseCanExecute(); ToggleLavCmd.RaiseCanExecute(); } }
  public string NuevoLavador { get => _ln; set { _ln = value; Notify(); CrearLavCmd.RaiseCanExecute(); } }
  public string EditLavador { get => _le; set { _le = value; Notify(); GuardarLavCmd.RaiseCanExecute(); } }
  public RelayCommand GuardarCmd { get; }
  public RelayCommand CrearUsuarioCmd { get; }
  public RelayCommand ProbarImpresoraCmd { get; }
  public RelayCommand EditarCmd { get; }
  public RelayCommand GuardarUsuarioCmd { get; }
  public RelayCommand ClaveCmd { get; }
  public RelayCommand ToggleCmd { get; }
  public RelayCommand CrearLavCmd { get; }
  public RelayCommand EditarLavCmd { get; }
  public RelayCommand GuardarLavCmd { get; }
  public RelayCommand ToggleLavCmd { get; }

  public ConfigViewModel(IServiceProvider root)
  {
    _root = root;
    GuardarCmd = new(async _ => await GuardarAsync());
    ProbarImpresoraCmd = new(async _ => await ProbarImpresoraAsync());
    CrearUsuarioCmd = new(async _ => await CrearUsuarioAsync(),
      _ => !string.IsNullOrWhiteSpace(NuevoNombre) && !string.IsNullOrWhiteSpace(NuevoUser) && (NuevoClave?.Length ?? 0) >= 4);
    EditarCmd = new(_ => CargarEdicion(), _ => Seleccionado is not null);
    GuardarUsuarioCmd = new(async _ => await GuardarUsuarioAsync(),
      _ => Seleccionado is not null && !string.IsNullOrWhiteSpace(EditNombre) && !string.IsNullOrWhiteSpace(EditUser));
    ClaveCmd = new(async _ => await CambiarClaveAsync(),
      _ => Seleccionado is not null && (NuevaClave?.Length ?? 0) >= 4);
    ToggleCmd = new(async _ => await ToggleAsync(), _ => Seleccionado is not null);
    CrearLavCmd = new(async _ => await CrearLavadorAsync(), _ => !string.IsNullOrWhiteSpace(NuevoLavador));
    EditarLavCmd = new(_ => { if (LavadorSel is not null) { EditLavador = LavadorSel.Nombre; Mensaje = $"Editando lavador: {LavadorSel.Nombre}"; } }, _ => LavadorSel is not null);
    GuardarLavCmd = new(async _ => await GuardarLavadorAsync(), _ => LavadorSel is not null && !string.IsNullOrWhiteSpace(EditLavador));
    ToggleLavCmd = new(async _ => await ToggleLavadorAsync(), _ => LavadorSel is not null);
  }

  public async Task CargarAsync()
  {
    using var s = _root.CreateScope();
    var sp = s.ServiceProvider;
    var n = await sp.GetRequiredService<ObtenerNegocioHandler>().Handle();
    Nombre = n.Nombre; Telefono = n.Telefono ?? ""; Direccion = n.Direccion ?? "";
    var us = await sp.GetRequiredService<ListarUsuariosHandler>().Handle();
    Usuarios.Clear();
    foreach (var u in us) Usuarios.Add(new UsuarioRow(u.Id, u.Nombre, u.Username, u.Activo));
    var lavs = await sp.GetRequiredService<Carwash.Application.Personal.Lavadores.ListarLavadoresHandler>().Handle(false);
    Lavadores.Clear();
    foreach (var l in lavs) Lavadores.Add(new LavadorRow(l.Id, l.Nombre, l.Activo));
  }
  public async Task GuardarAsync()
  {
    try
    {
      using var s = _root.CreateScope();
      var r = await s.ServiceProvider.GetRequiredService<ActualizarNegocioHandler>().Handle(new(Nombre, Telefono, Direccion, null));
      Mensaje = r.IsSuccess ? "Negocio guardado." : r.Error!;
    }
    catch (Exception ex) { Mensaje = "Error: " + ex.GetBaseException().Message; }
  }
  private async Task CrearUsuarioAsync()
  {
    try
    {
      using var s = _root.CreateScope();
      var r = await s.ServiceProvider.GetRequiredService<CrearUsuarioHandler>().Handle(NuevoNombre, NuevoUser, NuevoClave);
      Mensaje = r.IsSuccess ? $"Usuario {NuevoUser} creado." : r.Error!;
      if (r.IsSuccess) { NuevoNombre = ""; NuevoUser = ""; NuevoClave = ""; await CargarAsync(); }
    }
    catch (Exception ex) { Mensaje = "Error: " + ex.GetBaseException().Message; }
  }
  private void CargarEdicion()
  {
    if (Seleccionado is null) return;
    EditNombre = Seleccionado.Nombre;
    EditUser = Seleccionado.Username;
    NuevaClave = "";
    Mensaje = $"Editando: {Seleccionado.Nombre}";
  }
  private async Task GuardarUsuarioAsync()
  {
    if (Seleccionado is null) return;
    try
    {
      using var s = _root.CreateScope();
      var r = await s.ServiceProvider.GetRequiredService<ActualizarUsuarioHandler>().Handle(Seleccionado.Id, EditNombre, EditUser);
      Mensaje = r.IsSuccess ? "Usuario guardado." : r.Error!;
      if (r.IsSuccess) await CargarAsync();
    }
    catch (Exception ex) { Mensaje = "Error: " + ex.GetBaseException().Message; }
  }
  private async Task CambiarClaveAsync()
  {
    if (Seleccionado is null) return;
    try
    {
      using var s = _root.CreateScope();
      var r = await s.ServiceProvider.GetRequiredService<CambiarClaveHandler>().Handle(Seleccionado.Id, NuevaClave);
      Mensaje = r.IsSuccess ? "Clave cambiada." : r.Error!;
      if (r.IsSuccess) NuevaClave = "";
    }
    catch (Exception ex) { Mensaje = "Error: " + ex.GetBaseException().Message; }
  }
  private async Task ToggleAsync()
  {
    if (Seleccionado is null) return;
    try
    {
      using var s = _root.CreateScope();
      var r = await s.ServiceProvider.GetRequiredService<CambiarEstadoUsuarioHandler>().Handle(Seleccionado.Id);
      Mensaje = r.IsSuccess ? "Estado cambiado." : r.Error!;
      if (r.IsSuccess) await CargarAsync();
    }
    catch (Exception ex) { Mensaje = "Error: " + ex.GetBaseException().Message; }
  }
  private async Task CrearLavadorAsync()
  {
    try
    {
      using var s = _root.CreateScope();
      var r = await s.ServiceProvider.GetRequiredService<Carwash.Application.Personal.Lavadores.CrearLavadorHandler>().Handle(NuevoLavador);
      Mensaje = r.IsSuccess ? $"Lavador {NuevoLavador} creado." : r.Error!;
      if (r.IsSuccess) { NuevoLavador = ""; await CargarAsync(); }
    }
    catch (Exception ex) { Mensaje = "Error: " + ex.GetBaseException().Message; }
  }
  private async Task GuardarLavadorAsync()
  {
    if (LavadorSel is null) return;
    try
    {
      using var s = _root.CreateScope();
      var r = await s.ServiceProvider.GetRequiredService<Carwash.Application.Personal.Lavadores.ActualizarLavadorHandler>().Handle(LavadorSel.Id, EditLavador);
      Mensaje = r.IsSuccess ? "Lavador guardado." : r.Error!;
      if (r.IsSuccess) await CargarAsync();
    }
    catch (Exception ex) { Mensaje = "Error: " + ex.GetBaseException().Message; }
  }
  private async Task ToggleLavadorAsync()
  {
    if (LavadorSel is null) return;
    try
    {
      using var s = _root.CreateScope();
      var r = await s.ServiceProvider.GetRequiredService<Carwash.Application.Personal.Lavadores.CambiarEstadoLavadorHandler>().Handle(LavadorSel.Id);
      Mensaje = r.IsSuccess ? "Estado cambiado." : r.Error!;
      if (r.IsSuccess) await CargarAsync();
    }
    catch (Exception ex) { Mensaje = "Error: " + ex.GetBaseException().Message; }
  }
  private async Task ProbarImpresoraAsync()
  {
    try
    {
      string impresoras;
      try
      {
        impresoras = string.Join(", ", new System.Printing.PrintServer().GetPrintQueues().Select(q => q.Name));
        if (string.IsNullOrWhiteSpace(impresoras)) impresoras = "(ninguna)";
      }
      catch (Exception ex) { impresoras = "no se pudieron listar: " + ex.GetBaseException().Message; }
      using var s = _root.CreateScope();
      var sp = s.ServiceProvider;
      var n = await sp.GetRequiredService<ObtenerNegocioHandler>().Handle();
      var r = await sp.GetRequiredService<Carwash.Application.Common.Interfaces.ITicketPrinter>().PrintAsync(
        new Carwash.Application.Common.Interfaces.TicketPrintModel(
          string.IsNullOrWhiteSpace(n.Nombre) ? "Mi Carwash" : n.Nombre, n.Telefono, n.Direccion,
          0, DateTime.Now, null, "PRUEBA", "VEHICULO PRUEBA", "LAVADOR PRUEBA",
          [new("Lavado prueba", 1, 0, 0)], 0, "Efectivo", null));
      Mensaje = (r.Ok ? "Prueba OK: " : "FALLÓ la prueba. ") + r.Detalle + " Impresoras instaladas aquí: " + impresoras + ".";
    }
    catch (Exception ex) { Mensaje = "Error: " + ex.GetBaseException().Message; }
  }
}
