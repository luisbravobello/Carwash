using Carwash.Application.Common.Interfaces;
using Carwash.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace Carwash.Application.Tickets.CobrarPendiente;

public sealed class MarcarPagadoHandler(ICarwashDbContext db)
{
  public async Task<Result> Handle(int numero, int metodoPagoId, string? referencia, CancellationToken ct = default)
  {
    var t = await db.Tickets.FirstOrDefaultAsync(x => x.Numero == numero, ct);
    if (t is null) return Result.Failure("Ticket no existe.");
    var r = t.MarcarPagado(metodoPagoId, referencia);
    if (!r.IsSuccess) return r;
    await db.SaveChangesAsync(ct);
    return Result.Success();
  }
}
