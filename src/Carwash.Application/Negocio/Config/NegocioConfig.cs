using Carwash.Application.Common.Interfaces;
using Carwash.Domain.Common;
using Carwash.Domain.Negocio;
using Microsoft.EntityFrameworkCore;

namespace Carwash.Application.Negocio.Config;

public record NegocioDto(string Nombre, string? Telefono, string? Direccion, string? LogoPath);
public sealed class ObtenerNegocioHandler(ICarwashDbContext db)
{
  public async Task<NegocioDto> Handle(CancellationToken ct = default)
  {
    var n = await db.Negocios.FirstOrDefaultAsync(ct);
    return n is null ? new("", null, null, null) : new(n.Nombre, n.Telefono, n.Direccion, n.LogoPath);
  }
}

public record ActualizarNegocioCommand(string Nombre, string? Telefono, string? Direccion, string? LogoPath);
public sealed class ActualizarNegocioHandler(ICarwashDbContext db)
{
  public async Task<Result> Handle(ActualizarNegocioCommand c, CancellationToken ct = default)
  {
    if (string.IsNullOrWhiteSpace(c.Nombre)) return Result.Failure("Nombre requerido");
    var n = await db.Negocios.FirstOrDefaultAsync(ct);
    if (n is null) db.Negocios.Add(Carwash.Domain.Negocio.Negocio.Crear(c.Nombre, c.LogoPath, c.Telefono, c.Direccion));
    else n.ActualizarDatos(c.Nombre, c.LogoPath, c.Telefono, c.Direccion);
    await db.SaveChangesAsync(ct);
    return Result.Success();
  }
}
