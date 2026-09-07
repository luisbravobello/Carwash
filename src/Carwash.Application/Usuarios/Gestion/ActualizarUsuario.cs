using Carwash.Application.Common.Interfaces;
using Carwash.Application.Usuarios.Seguridad;
using Carwash.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace Carwash.Application.Usuarios.Gestion;

public sealed class ActualizarUsuarioHandler(ICarwashDbContext db)
{
  public async Task<Result> Handle(int id, string nombre, string username, CancellationToken ct = default)
  {
    var u = await db.Usuarios.FirstOrDefaultAsync(x => x.Id == id, ct);
    if (u is null) return Result.Failure("No existe.");
    var user = username.Trim().ToLower();
    if (await db.Usuarios.AnyAsync(x => x.Id != id && x.Username == user, ct))
      return Result.Failure("Ese usuario ya existe.");
    u.Renombrar(nombre, user);
    await db.SaveChangesAsync(ct);
    return Result.Success();
  }
}

public sealed class CambiarClaveHandler(ICarwashDbContext db)
{
  public async Task<Result> Handle(int id, string nuevaClave, CancellationToken ct = default)
  {
    if (string.IsNullOrWhiteSpace(nuevaClave) || nuevaClave.Length < 4)
      return Result.Failure("Clave de 4+ letras.");
    var u = await db.Usuarios.FirstOrDefaultAsync(x => x.Id == id, ct);
    if (u is null) return Result.Failure("No existe.");
    u.CambiarClave(PasswordHasher.Hash(nuevaClave));
    await db.SaveChangesAsync(ct);
    return Result.Success();
  }
}

public sealed class CambiarEstadoUsuarioHandler(ICarwashDbContext db)
{
  public async Task<Result> Handle(int id, CancellationToken ct = default)
  {
    var u = await db.Usuarios.FirstOrDefaultAsync(x => x.Id == id, ct);
    if (u is null) return Result.Failure("No existe.");
    u.SetActivo(!u.Activo);
    await db.SaveChangesAsync(ct);
    return Result.Success();
  }
}
