using Carwash.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Carwash.Application.Tickets.Historial;

public record DetalleLineaDto(string Producto, int Cantidad, decimal Precio, decimal Subtotal);
public record TicketDetalleDto(int Numero, DateTime Fecha, string? Cliente, string? Vehiculo, string? Lavador, decimal Total, string Metodo, string? Referencia, string Estado, List<DetalleLineaDto> Lineas);

public sealed class DetalleTicketHandler(ICarwashDbContext db)
{
  public async Task<TicketDetalleDto?> Handle(int numero, CancellationToken ct = default)
  {
    var t = await db.Tickets.Include(x => x.Detalles).FirstOrDefaultAsync(x => x.Numero == numero, ct);
    if (t is null) return null;
    var ids = t.Detalles.Select(d => d.ProductoServicioId).ToList();
    var nombres = await db.Productos.Where(p => ids.Contains(p.Id)).ToDictionaryAsync(p => p.Id, p => p.Nombre, ct);
    return new TicketDetalleDto(t.Numero, t.Fecha, t.ClienteNombre, t.VehiculoDescripcion, t.LavadorNombre, t.Total,
      t.MetodoPagoId == 1 ? "Efectivo" : "Transferencia", t.ReferenciaTransfer, t.Estado,
      t.Detalles.Select(d => new DetalleLineaDto(
        nombres.TryGetValue(d.ProductoServicioId, out var n) ? n : $"#{d.ProductoServicioId}",
        d.Cantidad, d.PrecioUnitario, d.Cantidad * d.PrecioUnitario)).ToList());
  }
}
