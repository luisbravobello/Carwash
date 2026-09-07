using Carwash.Application.Common.Interfaces;
using Carwash.Application.Usuarios.Seguridad;
using Carwash.Domain.Common;
using Carwash.Domain.Negocio;
using Microsoft.EntityFrameworkCore;

namespace Carwash.Application.Usuarios.Gestion;

public record UsuarioDto(int Id, string Nombre, string Username, bool Activo);
public sealed class ListarUsuariosHandler(ICarwashDbContext db)
{
  public async Task<List<UsuarioDto>> Handle(CancellationToken ct = default)
    => await db.Usuarios.OrderBy(u => u.Nombre)
      .Select(u => new UsuarioDto(u.Id, u.Nombre, u.Username, u.Activo)).ToListAsync(ct);
}

public sealed class CrearUsuarioHandler(ICarwashDbContext db)
{
  public async Task<Result<int>> Handle(string nombre, string username, string clave, CancellationToken ct = default)
  {
    if (string.IsNullOrWhiteSpace(nombre)) return Result<int>.Failure("Nombre requerido.");
    if (string.IsNullOrWhiteSpace(username)) return Result<int>.Failure("Usuario requerido.");
    if (string.IsNullOrWhiteSpace(clave) || clave.Length < 4) return Result<int>.Failure("Clave de 4+ letras.");
    var u = username.Trim().ToLower();
    if (await db.Usuarios.AnyAsync(x => x.Username == u, ct)) return Result<int>.Failure("Ese usuario ya existe.");
    var ent = Usuario.Crear(nombre.Trim(), u, PasswordHasher.Hash(clave));
    db.Usuarios.Add(ent);
    await db.SaveChangesAsync(ct);
    return Result<int>.Success(ent.Id);
  }
}
