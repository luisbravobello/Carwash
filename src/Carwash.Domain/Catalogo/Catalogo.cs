using Carwash.Domain.Common;

namespace Carwash.Domain.Catalogo;

// Screaming: Catalogo grita qué vende el carwash + bar
public class Categoria : Entity
{
  private Categoria() { }
  public string Nombre { get; private set; } = null!;

  public static Categoria Crear(string nombre)
  {
    if (string.IsNullOrWhiteSpace(nombre)) throw new DomainException("Categoría requerida");
    return new Categoria { Nombre = nombre.Trim() };
  }
}

public class ProductoServicio : Entity
{
  private ProductoServicio() { }
  public int CategoriaId { get; private set; }
  public string Nombre { get; private set; } = null!;
  public decimal Precio { get; private set; }
  public bool ControlaStock { get; private set; }
  public int Stock { get; private set; }
  public bool Activo { get; private set; } = true;

  public static ProductoServicio Crear(int categoriaId, string nombre, decimal precio, bool controlaStock = false, int stock = 0)
  {
    if (precio < 0) throw new DomainException("Precio no puede ser negativo");
    if (string.IsNullOrWhiteSpace(nombre)) throw new DomainException("Nombre requerido");
    return new ProductoServicio { CategoriaId = categoriaId, Nombre = nombre.Trim(), Precio = precio, ControlaStock = controlaStock, Stock = stock };
  }

  public Result DescontarStock(int cantidad)
  {
    if (!ControlaStock) return Result.Success();
    if (cantidad <= 0) return Result.Failure("Cantidad inválida");
    if (Stock < cantidad) return Result.Failure($"Stock insuficiente de {Nombre}");
    Stock -= cantidad;
    return Result.Success();
  }

  public void CambiarPrecio(decimal nuevoPrecio)
  {
    if (nuevoPrecio < 0) throw new DomainException("Precio inválido");
    Precio = nuevoPrecio;
  }

  public void Renombrar(string nombre)
  {
    if (string.IsNullOrWhiteSpace(nombre)) throw new DomainException("Nombre requerido");
    Nombre = nombre.Trim();
  }

  public void SetActivo(bool activo) => Activo = activo;

  public void ReponerStock(int stock, bool controlaStock)
  {
    if (stock < 0) throw new DomainException("Stock inválido");
    Stock = stock;
    ControlaStock = controlaStock;
  }
}
