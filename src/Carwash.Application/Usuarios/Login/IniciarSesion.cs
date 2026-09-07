using Carwash.Application.Common.Interfaces;
using Carwash.Application.Usuarios.Seguridad;
using Carwash.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace Carwash.Application.Usuarios.Login;

public record SesionDto(int UsuarioId, string Nombre, string Username);
public sealed class IniciarSesionHandler(ICarwashDbContext db)
{
  public async Task<Result<SesionDto>> Handle(string username, string password, CancellationToken ct = default)
  {
    if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
      return Result<SesionDto>.Failure("Escribe usuario y clave.");
    var u = await db.Usuarios.FirstOrDefaultAsync(x => x.Username == username.Trim().ToLower() && x.Activo, ct);
    if (u is null || !PasswordHasher.Verify(password, u.PasswordHash))
      return Result<SesionDto>.Failure("Usuario o clave mal.");
    return Result<SesionDto>.Success(new(u.Id, u.Nombre, u.Username));
  }
}
