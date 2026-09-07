using Carwash.Application.Common.Interfaces;
using Carwash.Domain.Common;
using Carwash.Domain.Personal;
using Microsoft.EntityFrameworkCore;

namespace Carwash.Application.Personal.Lavadores;

public record LavadorDto(int Id, string Nombre, bool Activo);
public sealed class ListarLavadoresHandler(ICarwashDbContext db)
{
  public async Task<List<LavadorDto>> Handle(bool soloActivos, CancellationToken ct = default)
  {
    var q = db.Lavadores.AsQueryable();
    if (soloActivos) q = q.Where(l => l.Activo);
    return await q.OrderBy(l => l.Nombre).Select(l => new LavadorDto(l.Id, l.Nombre, l.Activo)).ToListAsync(ct);
  }
}

public sealed class CrearLavadorHandler(ICarwashDbContext db)
{
  public async Task<Result<int>> Handle(string nombre, CancellationToken ct = default)
  {
    if (string.IsNullOrWhiteSpace(nombre)) return Result<int>.Failure("Nombre requerido.");
    var l = Lavador.Crear(nombre.Trim());
    db.Lavadores.Add(l);
    await db.SaveChangesAsync(ct);
    return Result<int>.Success(l.Id);
  }
}

public sealed class ActualizarLavadorHandler(ICarwashDbContext db)
{
  public async Task<Result> Handle(int id, string nombre, CancellationToken ct = default)
  {
    var l = await db.Lavadores.FirstOrDefaultAsync(x => x.Id == id, ct);
    if (l is null) return Result.Failure("No existe.");
    l.Renombrar(nombre);
    await db.SaveChangesAsync(ct);
    return Result.Success();
  }
}

public sealed class CambiarEstadoLavadorHandler(ICarwashDbContext db)
{
  public async Task<Result> Handle(int id, CancellationToken ct = default)
  {
    var l = await db.Lavadores.FirstOrDefaultAsync(x => x.Id == id, ct);
    if (l is null) return Result.Failure("No existe.");
    l.SetActivo(!l.Activo);
    await db.SaveChangesAsync(ct);
    return Result.Success();
  }
}
