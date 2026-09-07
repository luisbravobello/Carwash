using Carwash.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Carwash.Application.Tickets.Historial;

public record TicketHistorialDto(int Numero, DateTime Fecha, string? Cliente, string? Lavador, decimal Total, string Metodo, string Estado, int Items)
{
  public decimal Abono => Estado == "V" ? Total : 0;
  public decimal Saldo => Estado == "V" ? 0 : Total;
  public string EstadoTexto => Estado switch { "V" => "Pagada Total", "P" => "Pendiente", _ => "Anulada" };
  public string Folio => $"T-{Numero:000000}";
}
public sealed class HistorialDelDiaHandler(ICarwashDbContext db)
{
  public async Task<List<TicketHistorialDto>> Handle(DateOnly dia, CancellationToken ct = default)
  {
    var ini = dia.ToDateTime(TimeOnly.MinValue);
    var fin = dia.ToDateTime(TimeOnly.MaxValue);
    return await db.Tickets.Where(t => t.Fecha >= ini && t.Fecha <= fin)
      .OrderByDescending(t => t.Numero)
      .Select(t => new TicketHistorialDto(t.Numero, t.Fecha, t.ClienteNombre, t.LavadorNombre, t.Total,
        t.MetodoPagoId == 1 ? "Efectivo" : "Transferencia", t.Estado, t.Detalles.Count))
      .ToListAsync(ct);
  }
}
