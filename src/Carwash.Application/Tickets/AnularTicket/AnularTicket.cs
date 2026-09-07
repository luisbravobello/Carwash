using Carwash.Application.Common.Interfaces;
using Carwash.Domain.Common;
using Carwash.Domain.Tickets;
using Microsoft.EntityFrameworkCore;

namespace Carwash.Application.Tickets.AnularTicket;

public record AnularTicketCommand(int Numero);

public sealed class AnularTicketHandler(ICarwashDbContext db)
{
  public async Task<Result> Handle(AnularTicketCommand cmd, CancellationToken ct = default)
  {
    var ticket = await db.Tickets
      .Include(t => t.Detalles)
      .FirstOrDefaultAsync(t => t.Numero == cmd.Numero, ct);
    if (ticket is null) return Result.Failure("Ticket no existe");
    var r = ticket.Anular();
    if (!r.IsSuccess) return r;
    await db.SaveChangesAsync(ct);
    return Result.Success();
  }
}
