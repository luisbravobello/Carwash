using Carwash.Application.Common.Interfaces;
using Carwash.Domain.Caja;
using Carwash.Domain.Catalogo;
using Carwash.Domain.Negocio;
using Carwash.Domain.Tickets;
using Carwash.Domain.Vehiculos;
using Microsoft.EntityFrameworkCore;

namespace Carwash.Infrastructure.Persistence;

// Infrastructure es plugin: implementa la abstracción, no al revés (DIP).
public class CarwashDbContext(DbContextOptions<CarwashDbContext> options) : DbContext(options), ICarwashDbContext
{
  public DbSet<Negocio> Negocios => Set<Negocio>();
  public DbSet<Usuario> Usuarios => Set<Usuario>();
  public DbSet<Categoria> Categorias => Set<Categoria>();
  public DbSet<ProductoServicio> Productos => Set<ProductoServicio>();
  public DbSet<Cliente> Clientes => Set<Cliente>();
  public DbSet<Vehiculo> Vehiculos => Set<Vehiculo>();
  public DbSet<MetodoPago> MetodosPago => Set<MetodoPago>();
  public DbSet<Ticket> Tickets => Set<Ticket>();
  public DbSet<Carwash.Domain.Personal.Lavador> Lavadores => Set<Carwash.Domain.Personal.Lavador>();

  protected override void OnModelCreating(ModelBuilder m)
  {
    m.ApplyConfigurationsFromAssembly(typeof(CarwashDbContext).Assembly);
    // Sin HasData: el seed se hace en Program.cs para que sea idempotente en LocalDB.
  }
}
