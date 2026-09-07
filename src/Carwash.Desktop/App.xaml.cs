using System.Windows;
using Carwash.Application;
using Carwash.Application.Usuarios.Seguridad;
using Carwash.Desktop.Seguridad;
using Carwash.Desktop.ViewModels;
using Carwash.Domain.Catalogo;
using Carwash.Domain.Negocio;
using Carwash.Infrastructure;
using Carwash.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Carwash.Desktop;

public partial class App : System.Windows.Application
{
  public static IServiceProvider Services { get; private set; } = null!;

  protected override async void OnStartup(StartupEventArgs e)
  {
    base.OnStartup(e);
    try
    {
    var cs = "Server=(localdb)\\MSSQLLocalDB;Database=CarwashDB;Trusted_Connection=True;TrustServerCertificate=True";
    var services = new ServiceCollection();
    services.AddApplication();
    services.AddInfrastructure(cs, "EPSON TM-T20II");
    services.AddSingleton<SesionActual>();
    services.AddTransient<LoginViewModel>();
    services.AddTransient<LoginWindow>();
    services.AddTransient<MainViewModel>();
    services.AddTransient<ProductosViewModel>();
    services.AddTransient<HistorialViewModel>();
    services.AddTransient<CajaViewModel>();
    services.AddTransient<ConfigViewModel>();
    services.AddTransient<ShellViewModel>();
    services.AddTransient<MainWindow>();
    Services = services.BuildServiceProvider();

    using (var s = Services.CreateScope())
    {
      var db = s.ServiceProvider.GetRequiredService<CarwashDbContext>();
      await db.Database.MigrateAsync();
      if (!await db.MetodosPago.AnyAsync())
      {
        await db.Database.ExecuteSqlRawAsync(
          "IF NOT EXISTS (SELECT 1 FROM MetodosPago WHERE Id = 1) INSERT INTO MetodosPago (Id, Nombre) VALUES (1, 'Efectivo'); " +
          "IF NOT EXISTS (SELECT 1 FROM MetodosPago WHERE Id = 2) INSERT INTO MetodosPago (Id, Nombre) VALUES (2, 'Transferencia');");
      }
      // Seed admin usuario+clave si falta (primera vez: admin/1234).
      if (!await db.Usuarios.AnyAsync(x => x.Username == "admin"))
      {
        var old = await db.Usuarios.FirstOrDefaultAsync();
        if (old is null)
          db.Usuarios.Add(Usuario.Crear("Administradora", "admin", PasswordHasher.Hash("1234")));
        else
          old.CambiarClave(PasswordHasher.Hash("1234"));
        // Si el viejo no tenía username, EF lo deja ""; lo normalizamos por SQL.
        await db.SaveChangesAsync();
        await db.Database.ExecuteSqlRawAsync("UPDATE Usuarios SET Username='admin' WHERE Username=''");
      }
      if (!await db.Negocios.AnyAsync())
        db.Negocios.Add(Negocio.Crear("Mi Carwash Punta Cana", null, "809-555-0000", "Punta Cana"));
      if (!await db.Categorias.AnyAsync())
      {
        var lav = Categoria.Crear("Lavado");
        var bar = Categoria.Crear("Bar");
        db.Categorias.AddRange(lav, bar);
        await db.SaveChangesAsync();
        db.Productos.AddRange(
          ProductoServicio.Crear(lav.Id, "Lavado Básico Carro", 500),
          ProductoServicio.Crear(lav.Id, "Lavado Profundo Yipeta", 1200),
          ProductoServicio.Crear(bar.Id, "Refresco", 100, true, 50),
          ProductoServicio.Crear(bar.Id, "Cerveza", 200, true, 48));
      }
      await db.SaveChangesAsync();
    }

    var login = Services.GetRequiredService<LoginWindow>();
    if (login.ShowDialog() != true)
    {
      Shutdown();
      return;
    }
    var main = Services.GetRequiredService<MainWindow>();
    MainWindow = main;
    main.Show();
    ShutdownMode = ShutdownMode.OnLastWindowClose;
    }
    catch (Exception ex)
    {
      MessageBox.Show("Error al iniciar:\n" + ex.GetBaseException().Message, "Mi Carwash",
        MessageBoxButton.OK, MessageBoxImage.Error);
      Shutdown();
    }
  }
}
