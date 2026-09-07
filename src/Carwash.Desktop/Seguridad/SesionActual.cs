namespace Carwash.Desktop.Seguridad;

// Singleton de sesión: quién cobró cada ticket (auditoría).
public sealed class SesionActual
{
  public int UsuarioId { get; private set; }
  public string Nombre { get; private set; } = "";
  public string Username { get; private set; } = "";
  public bool Activa => UsuarioId > 0;
  public void Iniciar(int id, string nombre, string username) { UsuarioId = id; Nombre = nombre; Username = username; }
  public void Cerrar() { UsuarioId = 0; Nombre = ""; Username = ""; }
}
