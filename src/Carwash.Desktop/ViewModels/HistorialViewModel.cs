using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text;
using System.Windows.Data;
using Carwash.Application.Common.Interfaces;
using Carwash.Application.Tickets.AnularTicket;
using Carwash.Application.Tickets.Historial;
using Microsoft.Extensions.DependencyInjection;

namespace Carwash.Desktop.ViewModels;

public sealed class HistorialViewModel : INotifyPropertyChanged
{
  public event PropertyChangedEventHandler? PropertyChanged;
  private readonly IServiceProvider _root;
  private void Notify(string n) => PropertyChanged?.Invoke(this, new(n));
  private string _msg = "", _factura = "", _filtro = "";
  private TicketHistorialDto? _sel;
  public string Mensaje { get => _msg; set { _msg = value; Notify(nameof(Mensaje)); } }
  public string FacturaTexto { get => _factura; set { _factura = value; Notify(nameof(FacturaTexto)); } }
  public string Filtro
  {
    get => _filtro;
    set { _filtro = value; Notify(nameof(Filtro)); Vista.Refresh(); Notify(nameof(ConteoTexto)); }
  }
  public string ConteoTexto => $"Mostrando {Vista.Cast<object>().Count()} de {Items.Count} comprobantes.";
  public TicketHistorialDto? Seleccionado
  {
    get => _sel;
    set { _sel = value; Notify(nameof(Seleccionado)); AnularCmd.RaiseCanExecute(); ReimprimirCmd.RaiseCanExecute(); PagarCmd.RaiseCanExecute(); _ = CargarDetalleAsync(); }
  }
  public ObservableCollection<TicketHistorialDto> Items { get; } = [];
  public ICollectionView Vista { get; }
  public RelayCommand RecargarCmd { get; }
  public RelayCommand AnularCmd { get; }
  public RelayCommand ReimprimirCmd { get; }
  public RelayCommand PagarCmd { get; }
  public RelayCommand VerCmd { get; }
  public RelayCommand AnularFilaCmd { get; }
  public RelayCommand TicketFilaCmd { get; }
  private int _pagoMetodo = 1;
  public int PagoMetodo { get => _pagoMetodo; set { _pagoMetodo = value; Notify(nameof(PagoMetodo)); PagarCmd.RaiseCanExecute(); } }

  public HistorialViewModel(IServiceProvider root)
  {
    _root = root;
    Vista = CollectionViewSource.GetDefaultView(Items);
    Vista.Filter = o =>
    {
      if (o is not TicketHistorialDto t) return false;
      if (string.IsNullOrWhiteSpace(_filtro)) return true;
      var f = _filtro.Trim().ToLower();
      return t.Numero.ToString().Contains(f) || (t.Cliente ?? "").ToLower().Contains(f);
    };
    RecargarCmd = new(async _ => await RecargarAsync());
    AnularCmd = new(async _ => await AnularAsync(), _ => Seleccionado is not null && Seleccionado.Estado != "A");
    ReimprimirCmd = new(async _ => await ReimprimirAsync(), _ => Seleccionado is not null);
    PagarCmd = new(async _ => await PagarAsync(), _ => Seleccionado is not null && Seleccionado.Estado == "P");
    VerCmd = new(p => { if (p is TicketHistorialDto t) Seleccionado = t; });
    AnularFilaCmd = new(async p => { if (p is TicketHistorialDto t) { Seleccionado = t; await AnularAsync(); } });
    TicketFilaCmd = new(async p => { if (p is TicketHistorialDto t) { Seleccionado = t; await ReimprimirAsync(); } });
  }

  public async Task RecargarAsync()
  {
    try
    {
      using var s = _root.CreateScope();
      var list = await s.ServiceProvider.GetRequiredService<HistorialDelDiaHandler>().Handle(DateOnly.FromDateTime(DateTime.Now));
      Items.Clear();
      foreach (var t in list) Items.Add(t);
      Vista.Refresh();
      Notify(nameof(ConteoTexto));
      Mensaje = $"{Items.Count} comprobantes hoy. Toca uno para ver qué se hizo.";
    }
    catch (Exception ex) { Mensaje = "Error: " + ex.GetBaseException().Message; }
  }

  private async Task CargarDetalleAsync()
  {
    Detalle.Clear();
    FacturaTexto = "";
    if (Seleccionado is null) return;
    try
    {
      using var s = _root.CreateScope();
      var sp = s.ServiceProvider;
      var d = await sp.GetRequiredService<DetalleTicketHandler>().Handle(Seleccionado.Numero);
      if (d is null) return;
      foreach (var l in d.Lineas) Detalle.Add(l);
      var negocio = await sp.GetRequiredService<Carwash.Application.Negocio.Config.ObtenerNegocioHandler>().Handle();
      FacturaTexto = Carwash.Application.Tickets.Formato.TicketTexto.Build(new TicketPrintModel(
        string.IsNullOrWhiteSpace(negocio.Nombre) ? "Mi Carwash" : negocio.Nombre, negocio.Telefono, negocio.Direccion,
        d.Numero, d.Fecha, null, d.Cliente, d.Vehiculo, d.Lavador,
        d.Lineas.Select(l => new TicketPrintLine(l.Producto, l.Cantidad, l.Precio, l.Subtotal)).ToList(),
        d.Total, d.Metodo, d.Referencia, d.Estado));
    }
    catch (Exception ex) { Mensaje = "Error: " + ex.GetBaseException().Message; }
  }

  public ObservableCollection<DetalleLineaDto> Detalle { get; } = [];

  private async Task AnularAsync()
  {
    if (Seleccionado is null) return;
    try
    {
      using var s = _root.CreateScope();
      var r = await s.ServiceProvider.GetRequiredService<AnularTicketHandler>().Handle(new(Seleccionado.Numero));
      Mensaje = r.IsSuccess ? $"Ticket #{Seleccionado.Numero} anulado." : r.Error!;
      if (r.IsSuccess) await RecargarAsync();
    }
    catch (Exception ex) { Mensaje = "Error: " + ex.GetBaseException().Message; }
  }

  private async Task PagarAsync()
  {
    if (Seleccionado is null) return;
    try
    {
      using var s = _root.CreateScope();
      var r = await s.ServiceProvider.GetRequiredService<Carwash.Application.Tickets.CobrarPendiente.MarcarPagadoHandler>()
        .Handle(Seleccionado.Numero, PagoMetodo, null);
      Mensaje = r.IsSuccess ? $"Ticket #{Seleccionado.Numero} cobrado. Ya puede retirar." : r.Error!;
      if (r.IsSuccess) await RecargarAsync();
    }
    catch (Exception ex) { Mensaje = "Error: " + ex.GetBaseException().Message; }
  }

  private async Task ReimprimirAsync()
  {
    if (Seleccionado is null) return;
    try
    {
      using var s = _root.CreateScope();
      var sp = s.ServiceProvider;
      var d = await sp.GetRequiredService<DetalleTicketHandler>().Handle(Seleccionado.Numero);
      if (d is null) return;
      var negocio = await sp.GetRequiredService<Carwash.Application.Negocio.Config.ObtenerNegocioHandler>().Handle();
      var print = await sp.GetRequiredService<ITicketPrinter>().PrintAsync(new TicketPrintModel(
        string.IsNullOrWhiteSpace(negocio.Nombre) ? "Mi Carwash" : negocio.Nombre, negocio.Telefono, negocio.Direccion,
        d.Numero, d.Fecha, null, d.Cliente, d.Vehiculo, d.Lavador,
        d.Lineas.Select(l => new TicketPrintLine(l.Producto, l.Cantidad, l.Precio, l.Subtotal)).ToList(),
        d.Total, d.Metodo, d.Referencia, d.Estado));
      Mensaje = print.Ok ? $"Ticket #{d.Numero} reimpreso en Epson." : $"NO salió en papel. {print.Detalle}";
    }
    catch (Exception ex) { Mensaje = "Error: " + ex.GetBaseException().Message; }
  }
}
