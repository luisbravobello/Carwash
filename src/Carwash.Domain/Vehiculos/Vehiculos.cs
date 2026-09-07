using Carwash.Domain.Common;

namespace Carwash.Domain.Vehiculos;

public class Cliente : Entity
{
  private readonly List<Vehiculo> _vehiculos = [];
  private Cliente() { }
  public string Nombre { get; private set; } = null!;
  public string? Telefono { get; private set; }
  public IReadOnlyList<Vehiculo> Vehiculos => _vehiculos.AsReadOnly();

  public static Cliente Crear(string nombre, string? telefono)
  {
    if (string.IsNullOrWhiteSpace(nombre)) throw new DomainException("Nombre requerido");
    return new Cliente { Nombre = nombre.Trim(), Telefono = telefono };
  }
}

public class Vehiculo : Entity
{
  private Vehiculo() { }
  public int? ClienteId { get; private set; }
  public string Placa { get; private set; } = null!;
  public string Tipo { get; private set; } = "Carro";
  public string? MarcaModelo { get; private set; }

  public static Vehiculo Crear(string placa, string tipo = "Carro", string? marcaModelo = null, int? clienteId = null)
  {
    if (string.IsNullOrWhiteSpace(placa)) throw new DomainException("Placa requerida");
    return new Vehiculo { Placa = placa.Trim().ToUpper(), Tipo = tipo, MarcaModelo = marcaModelo, ClienteId = clienteId };
  }
}
