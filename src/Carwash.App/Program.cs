using Carwash.Application;
using Carwash.Application.Caja.CuadreDelDia;
using Carwash.Application.Common.Interfaces;
using Carwash.Application.Tickets.CrearTicket;
using Carwash.Domain.Catalogo;
using Carwash.Domain.Negocio;
using Carwash.Domain.Vehiculos;
using Carwash.Infrastructure;
using Carwash.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

static void PrintFull(Exception? ex, int lvl = 0)
{
  while (ex is not null)
  {
    Console.WriteLine($"[{lvl}] {ex.GetType().Name}: {ex.Message}");
    ex = ex.InnerException;
    lvl++;
  }
}

var cs = "Server=(localdb)\\MSSQLLocalDB;Database=CarwashDB;Trusted_Connection=True;TrustServerCertificate=True";

var services = new ServiceCollection();
// Application: handlers Transient (uno nuevo por venta, sin estado).
services.AddApplication();
// Infrastructure: DbContext Scoped (uno por operación), impresora Singleton (una sola).
services.AddInfrastructure(cs, "EPSON TM-T20II");
using var provider = services.BuildServiceProvider();

try
{
  // Migraciones al arrancar (scope corto solo para eso).
  using (var migScope = provider.CreateScope())
  {
    var db = migScope.ServiceProvider.GetRequiredService<CarwashDbContext>();
    await db.Database.MigrateAsync();
  }

  // Operación 1: seed + venta + cuadre, cada una en su scope (Scoped DbContext fresh).
  using (var scope = provider.CreateScope())
  {
    var sp = scope.ServiceProvider;
    var db = sp.GetRequiredService<ICarwashDbContext>();
    var dbConcrete = sp.GetRequiredService<CarwashDbContext>();
    var crear = sp.GetRequiredService<CrearTicketHandler>();
    var cuadre = sp.GetRequiredService<CuadreDelDiaHandler>();

    if (!await db.MetodosPago.AnyAsync())
    {
      await dbConcrete.Database.ExecuteSqlRawAsync(
        "IF NOT EXISTS (SELECT 1 FROM MetodosPago WHERE Id = 1) INSERT INTO MetodosPago (Id, Nombre) VALUES (1, 'Efectivo'); " +
        "IF NOT EXISTS (SELECT 1 FROM MetodosPago WHERE Id = 2) INSERT INTO MetodosPago (Id, Nombre) VALUES (2, 'Transferencia');");
    }

    if (!await db.Negocios.AnyAsync())
    {
      db.Negocios.Add(Negocio.Crear("Mi Carwash Punta Cana", null, "809-555-0000", "Punta Cana"));
      db.Usuarios.Add(Usuario.Crear("Administradora", "admin", Carwash.Application.Usuarios.Seguridad.PasswordHasher.Hash("1234")));
      var catLav = Categoria.Crear("Lavado");
      var catBar = Categoria.Crear("Bar");
      db.Categorias.AddRange(catLav, catBar);
      await db.SaveChangesAsync();
      db.Productos.AddRange(
        ProductoServicio.Crear(catLav.Id, "Lavado Básico Carro", 500),
        ProductoServicio.Crear(catLav.Id, "Lavado Profundo Yipeta", 1200),
        ProductoServicio.Crear(catBar.Id, "Refresco", 100, true, 50),
        ProductoServicio.Crear(catBar.Id, "Cerveza", 200, true, 48));
      await db.SaveChangesAsync();
      Console.WriteLine("Seed inicial creado.");
    }

    var prodBasico = await db.Productos.FirstAsync(p => p.Nombre.Contains("Básico"));
    var veh = await db.Vehiculos.FirstOrDefaultAsync(v => v.Placa == "A123456");
    if (veh is null)
    {
      veh = Vehiculo.Crear("A123456", "Carro", "Toyota Corolla");
      db.Vehiculos.Add(veh);
      await db.SaveChangesAsync();
    }

    var res = await crear.Handle(new CrearTicketCommand(
      UsuarioId: (await db.Usuarios.FirstAsync()).Id,
      MetodoPagoId: 1,
      ReferenciaTransfer: null,
      VehiculoId: veh.Id,
      Lineas: [new(prodBasico.Id, 1)]));

    Console.WriteLine(res.IsSuccess ? $"Ticket #{res.Value!.Numero} creado + .txt en /tickets" : $"Error negocio: {res.Error}");

    var hoy = await cuadre.Handle(DateOnly.FromDateTime(DateTime.Now));
    Console.WriteLine($"Cuadre hoy: {hoy.CantidadTickets} tickets | Efec RD${hoy.TotalEfectivo:N2} | Transf RD${hoy.TotalTransferencia:N2} | Total RD${hoy.Total:N2}");
  }
}
catch (Exception ex)
{
  Console.WriteLine("=== ERROR REAL (copia todo esto) ===");
  PrintFull(ex);
}
