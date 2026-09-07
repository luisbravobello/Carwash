using System.ComponentModel;
using System.IO;
using Carwash.Application.Usuarios.Login;
using Carwash.Desktop.Seguridad;
using Microsoft.Extensions.DependencyInjection;

namespace Carwash.Desktop.ViewModels;

public sealed class LoginViewModel : INotifyPropertyChanged
{
  public event PropertyChangedEventHandler? PropertyChanged;
  private readonly IServiceProvider _root;
  private readonly SesionActual _sesion;
  private static string RememberPath
  {
    get
    {
      var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MiCarwash");
      Directory.CreateDirectory(dir);
      return Path.Combine(dir, "usuario.txt");
    }
  }
  private string _u = "", _msg = "";
  private bool _busy, _remember = true;
  public string Username { get => _u; set { _u = value; PropertyChanged?.Invoke(this, new(nameof(Username))); } }
  public bool Recordarme { get => _remember; set { _remember = value; PropertyChanged?.Invoke(this, new(nameof(Recordarme))); } }
  public string Mensaje { get => _msg; set { _msg = value; PropertyChanged?.Invoke(this, new(nameof(Mensaje))); } }
  public bool Ocupado { get => _busy; private set { _busy = value; PropertyChanged?.Invoke(this, new(nameof(Ocupado))); } }
  public bool Ok { get; private set; }

  public LoginViewModel(IServiceProvider root, SesionActual sesion)
  {
    _root = root; _sesion = sesion;
    try { if (File.Exists(RememberPath)) _u = File.ReadAllText(RememberPath).Trim(); } catch { }
  }

  public async Task<bool> LoginAsync(string password)
  {
    if (Ocupado) return false;
    Ocupado = true;
    Mensaje = "Verificando...";
    try
    {
      using var s = _root.CreateScope();
      var r = await s.ServiceProvider.GetRequiredService<IniciarSesionHandler>().Handle(Username, password);
      if (!r.IsSuccess) { Mensaje = r.Error!; return false; }
      _sesion.Iniciar(r.Value!.UsuarioId, r.Value.Nombre, r.Value.Username);
      try
      {
        if (Recordarme) File.WriteAllText(RememberPath, Username.Trim().ToLower());
        else if (File.Exists(RememberPath)) File.Delete(RememberPath);
      }
      catch { }
      Ok = true;
      Mensaje = $"Bienvenida, {r.Value.Nombre}.";
      return true;
    }
    catch (Exception ex) { Mensaje = "Error: " + ex.GetBaseException().Message; return false; }
    finally { Ocupado = false; }
  }
}
