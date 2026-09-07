using Carwash.Domain.Common;

namespace Carwash.Domain.Negocio;

// S: Solo representa el negocio. O: Se extiende sin modificarse.
public class Negocio : Entity
{
  private Negocio() { }
  public string Nombre { get; private set; } = null!;
  public string? LogoPath { get; private set; }
  public string? Telefono { get; private set; }
  public string? Direccion { get; private set; }

  public static Negocio Crear(string nombre, string? logoPath, string? telefono, string? direccion)
  {
    if (string.IsNullOrWhiteSpace(nombre)) throw new DomainException("Nombre requerido");
    return new Negocio { Id = 1, Nombre = nombre.Trim(), LogoPath = logoPath, Telefono = telefono, Direccion = direccion };
  }

  public void ActualizarDatos(string nombre, string? logoPath, string? telefono, string? direccion)
  {
    if (string.IsNullOrWhiteSpace(nombre)) throw new DomainException("Nombre requerido");
    Nombre = nombre.Trim(); LogoPath = logoPath; Telefono = telefono; Direccion = direccion;
  }
}

public class Usuario : Entity
{
  private Usuario() { }
  public string Nombre { get; private set; } = null!;
  public string Username { get; private set; } = null!;
  public string PasswordHash { get; private set; } = "";
  public bool Activo { get; private set; } = true;

  public static Usuario Crear(string nombre, string username = "admin", string passwordHash = "")
  {
    if (string.IsNullOrWhiteSpace(nombre)) throw new DomainException("Nombre requerido");
    if (string.IsNullOrWhiteSpace(username)) throw new DomainException("Usuario requerido");
    return new Usuario { Nombre = nombre.Trim(), Username = username.Trim().ToLower(), PasswordHash = passwordHash };
  }

  public void CambiarClave(string passwordHash) => PasswordHash = passwordHash;

  public void Renombrar(string nombre, string username)
  {
    if (string.IsNullOrWhiteSpace(nombre)) throw new DomainException("Nombre requerido");
    if (string.IsNullOrWhiteSpace(username)) throw new DomainException("Usuario requerido");
    Nombre = nombre.Trim();
    Username = username.Trim().ToLower();
  }

  public void SetActivo(bool activo) => Activo = activo;
}
