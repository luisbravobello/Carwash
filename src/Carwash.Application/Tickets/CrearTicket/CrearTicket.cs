using Carwash.Application.Common.Interfaces;
using Carwash.Domain.Common;
using Carwash.Domain.Tickets;
using Microsoft.EntityFrameworkCore;

namespace Carwash.Application.Tickets.CrearTicket;

// SRP: este handler solo sabe crear tickets. Un caso de uso = una clase.
public record CrearTicketLinea(int ProductoId, int Cantidad);
public record CrearTicketCommand(int UsuarioId, int MetodoPagoId, string? ReferenciaTransfer, int? VehiculoId, List<CrearTicketLinea> Lineas, string? ClienteNombre = null, string? VehiculoDescripcion = null, bool Pendiente = false, decimal? RecibidoEfectivo = null, int? LavadorId = null);

public record CrearTicketResult(int Numero, bool Impreso, string PrintDetalle);

public sealed class CrearTicketHandler(ICarwashDbContext db, ITicketPrinter printer)
{
  public async Task<Result<CrearTicketResult>> Handle(CrearTicketCommand cmd, CancellationToken ct = default)
  {
    if (cmd.Lineas.Count == 0) return Result<CrearTicketResult>.Failure("Ticket vacío");

    var productos = await db.Productos
      .Where(p => cmd.Lineas.Select(l => l.ProductoId).Contains(p.Id) && p.Activo)
      .ToDictionaryAsync(p => p.Id, ct);

    if (productos.Count != cmd.Lineas.Count) return Result<CrearTicketResult>.Failure("Producto inválido o inactivo");

    var metodo = await db.MetodosPago.FirstOrDefaultAsync(m => m.Id == cmd.MetodoPagoId, ct);
    if (metodo is null) return Result<CrearTicketResult>.Failure("Método de pago no existe. Reinicia la app para reparar el catálogo.");

    var ultimo = await db.Tickets.MaxAsync(t => (int?)t.Numero, ct) ?? 0;
    string? lavNombre = null;
    if (cmd.LavadorId.HasValue)
    {
      var lav = await db.Lavadores.FirstOrDefaultAsync(l => l.Id == cmd.LavadorId && l.Activo, ct);
      if (lav is null) return Result<CrearTicketResult>.Failure("Lavador inválido o inactivo.");
      lavNombre = lav.Nombre;
    }
    var resTicket = Ticket.Crear(ultimo + 1, cmd.UsuarioId, cmd.MetodoPagoId, cmd.ReferenciaTransfer, cmd.VehiculoId, cmd.ClienteNombre, cmd.VehiculoDescripcion, cmd.Pendiente, cmd.LavadorId, lavNombre);
    if (!resTicket.IsSuccess) return Result<CrearTicketResult>.Failure(resTicket.Error!);
    var ticket = resTicket.Value!;

    foreach (var l in cmd.Lineas)
    {
      var p = productos[l.ProductoId];
      var r = ticket.AgregarDetalle(p.Id, p.Nombre, l.Cantidad, p.Precio);
      if (!r.IsSuccess) return Result<CrearTicketResult>.Failure(r.Error!);
      var rs = p.DescontarStock(l.Cantidad);
      if (!rs.IsSuccess) return Result<CrearTicketResult>.Failure(rs.Error!);
    }

    if (!cmd.Pendiente && cmd.MetodoPagoId == 1 && cmd.RecibidoEfectivo.HasValue && cmd.RecibidoEfectivo.Value < ticket.Total)
      return Result<CrearTicketResult>.Failure($"Recibido RD${cmd.RecibidoEfectivo:N2} no alcanza. Total RD${ticket.Total:N2}.");

    db.Tickets.Add(ticket);
    await db.SaveChangesAsync(ct);

    // Impresión fuera de la transacción de negocio: si falla impresora, el ticket ya existe.
    var negocio = await db.Negocios.FirstOrDefaultAsync(ct);
    string? placa = cmd.VehiculoId.HasValue
      ? (await db.Vehiculos.FirstOrDefaultAsync(v => v.Id == cmd.VehiculoId, ct))?.Placa
      : null;

    var print = await printer.PrintAsync(new TicketPrintModel(
      negocio?.Nombre ?? "Mi Carwash",
      negocio?.Telefono,
      negocio?.Direccion,
      ticket.Numero,
      ticket.Fecha,
      placa,
      ticket.ClienteNombre,
      ticket.VehiculoDescripcion,
      ticket.LavadorNombre,
      ticket.Detalles.Select(d =>
      {
        var p = productos[d.ProductoServicioId];
        return new TicketPrintLine(p.Nombre, d.Cantidad, d.PrecioUnitario, d.Cantidad * d.PrecioUnitario);
      }).ToList(),
      ticket.Total,
      metodo.Nombre,
      ticket.ReferenciaTransfer,
      ticket.Estado), ct);

    return Result<CrearTicketResult>.Success(new(ticket.Numero, print.Ok, print.Detalle));
  }
}
