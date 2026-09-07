using Carwash.Domain.Common;

namespace Carwash.Domain.Personal;

// Quién lavó el vehículo. Se guarda el nombre en el ticket (foto al momento de vender).
public class Lavador : Entity
{
  private Lavador() { }
  public string Nombre { get; private set; } = null!;
  public bool Activo { get; private set; } = true;

  public static Lavador Crear(string nombre)
  {
    if (string.IsNullOrWhiteSpace(nombre)) throw new DomainException("Nombre requerido");
    return new Lavador { Nombre = nombre.Trim() };
  }

  public void Renombrar(string nombre)
  {
    if (string.IsNullOrWhiteSpace(nombre)) throw new DomainException("Nombre requerido");
    Nombre = nombre.Trim();
  }

  public void SetActivo(bool activo) => Activo = activo;
}
