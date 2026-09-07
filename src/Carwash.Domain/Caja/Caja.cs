using Carwash.Domain.Common;

namespace Carwash.Domain.Caja;

public class MetodoPago : Entity
{
  private MetodoPago() { }
  public string Nombre { get; private set; } = null!;
}
