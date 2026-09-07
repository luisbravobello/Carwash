using System.ComponentModel;
using Carwash.Application.Caja.CuadreDelDia;
using Microsoft.Extensions.DependencyInjection;

namespace Carwash.Desktop.ViewModels;

public sealed class CajaViewModel : INotifyPropertyChanged
{
  public event PropertyChangedEventHandler? PropertyChanged;
  private readonly IServiceProvider _root;
  private int _tickets, _pend;
  private decimal _efec, _transf, _total, _pendTot;
  private string _err = "";

  public int Tickets { get => _tickets; set { _tickets = value; Notify(nameof(Tickets)); } }
  public int Pendientes { get => _pend; set { _pend = value; Notify(nameof(Pendientes)); Notify(nameof(PendientesTexto)); } }
  public decimal TotalPendiente { get => _pendTot; set { _pendTot = value; Notify(nameof(TotalPendiente)); Notify(nameof(PendientesTexto)); } }
  public string PendientesTexto => Pendientes == 0 ? "Sin pendientes. Todo cobrado." : $"{Pendientes} pendiente(s) por RD${TotalPendiente:N2} (pagan al retirar).";
  public decimal Efectivo { get => _efec; set { _efec = value; Notify(nameof(Efectivo)); Notify(nameof(PorcEfectivo)); } }
  public decimal Transferencia { get => _transf; set { _transf = value; Notify(nameof(Transferencia)); Notify(nameof(PorcEfectivo)); } }
  public decimal Total { get => _total; set { _total = value; Notify(nameof(Total)); Notify(nameof(PorcEfectivo)); } }
  public double PorcEfectivo => Total <= 0 ? 0 : (double)(Efectivo / Total) * 100;
  public string Error { get => _err; set { _err = value; Notify(nameof(Error)); } }
  private void Notify(string n) => PropertyChanged?.Invoke(this, new(n));

  public CajaViewModel(IServiceProvider root) => _root = root;

  public async Task RecargarAsync()
  {
    try
    {
      Error = "";
      using var s = _root.CreateScope();
      var h = s.ServiceProvider.GetRequiredService<CuadreDelDiaHandler>();
      var r = await h.Handle(DateOnly.FromDateTime(DateTime.Now));
      Tickets = r.CantidadTickets;
      Efectivo = r.TotalEfectivo;
      Transferencia = r.TotalTransferencia;
      Total = r.Total;
      Pendientes = r.Pendientes;
      TotalPendiente = r.TotalPendiente;
    }
    catch (Exception ex) { Error = "Error: " + ex.GetBaseException().Message; }
  }
}
