using Carwash.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Carwash.Application.Caja.CuadreDelDia;

// Query gritando negocio: CuadreDelDia, no GetReportDto.
public record CuadreDelDiaResult(DateOnly Dia, int CantidadTickets, decimal TotalEfectivo, decimal TotalTransferencia, decimal Total, int Pendientes, decimal TotalPendiente);

public sealed class CuadreDelDiaHandler(ICarwashDbContext db)
{
  public async Task<CuadreDelDiaResult> Handle(DateOnly dia, CancellationToken ct = default)
  {
    var ini = dia.ToDateTime(TimeOnly.MinValue);
    var fin = dia.ToDateTime(TimeOnly.MaxValue);
    var pagados = await db.Tickets
      .Where(t => t.Fecha >= ini && t.Fecha <= fin && t.Estado == "V")
      .ToListAsync(ct);
    var pendientes = await db.Tickets
      .Where(t => t.Fecha >= ini && t.Fecha <= fin && t.Estado == "P")
      .ToListAsync(ct);
    return new CuadreDelDiaResult(
      dia,
      pagados.Count,
      pagados.Where(t => t.MetodoPagoId == 1).Sum(t => t.Total),
      pagados.Where(t => t.MetodoPagoId == 2).Sum(t => t.Total),
      pagados.Sum(t => t.Total),
      pendientes.Count,
      pendientes.Sum(t => t.Total));
  }
}
