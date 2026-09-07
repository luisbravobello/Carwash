using Carwash.Application.Common.Interfaces;
using Carwash.Domain.Catalogo;
using Carwash.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace Carwash.Application.Catalogo.Productos;

public record CrearProductoCommand(int CategoriaId, string Nombre, decimal Precio, bool ControlaStock, int Stock);
public sealed class CrearProductoHandler(ICarwashDbContext db)
{
  public async Task<Result<int>> Handle(CrearProductoCommand c, CancellationToken ct = default)
  {
    if (string.IsNullOrWhiteSpace(c.Nombre)) return Result<int>.Failure("Nombre requerido");
    if (c.Precio < 0) return Result<int>.Failure("Precio inválido");
    var p = ProductoServicio.Crear(c.CategoriaId, c.Nombre.Trim(), c.Precio, c.ControlaStock, c.Stock);
    db.Productos.Add(p);
    await db.SaveChangesAsync(ct);
    return Result<int>.Success(p.Id);
  }
}

public record ActualizarProductoCommand(int Id, string Nombre, decimal Precio, bool Activo, bool ControlaStock, int Stock);
public sealed class ActualizarProductoHandler(ICarwashDbContext db)
{
  public async Task<Result> Handle(ActualizarProductoCommand c, CancellationToken ct = default)
  {
    var p = await db.Productos.FirstOrDefaultAsync(x => x.Id == c.Id, ct);
    if (p is null) return Result.Failure("No existe");
    p.Renombrar(c.Nombre);
    p.CambiarPrecio(c.Precio);
    p.SetActivo(c.Activo);
    p.ReponerStock(c.Stock, c.ControlaStock);
    await db.SaveChangesAsync(ct);
    return Result.Success();
  }
}

public record ProductoDto(int Id, string Categoria, string Nombre, decimal Precio, bool Activo, int Stock);
public sealed class ListarProductosHandler(ICarwashDbContext db)
{
  public async Task<List<ProductoDto>> Handle(CancellationToken ct = default)
    => await db.Productos.Select(p => new ProductoDto(p.Id, "", p.Nombre, p.Precio, p.Activo, p.Stock)).ToListAsync(ct);
}
