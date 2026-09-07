using Carwash.Domain.Caja;
using Carwash.Domain.Catalogo;
using Carwash.Domain.Tickets;
using Carwash.Domain.Vehiculos;
using Microsoft.EntityFrameworkCore;

namespace Carwash.Application.Common.Interfaces;

// ISP: interfaz mínima, DIP: Application no conoce SQL, solo abstracción.
public interface ICarwashDbContext
{
  DbSet<global::Carwash.Domain.Negocio.Negocio> Negocios { get; }
  DbSet<global::Carwash.Domain.Negocio.Usuario> Usuarios { get; }
  DbSet<Categoria> Categorias { get; }
  DbSet<ProductoServicio> Productos { get; }
  DbSet<Cliente> Clientes { get; }
  DbSet<Vehiculo> Vehiculos { get; }
  DbSet<MetodoPago> MetodosPago { get; }
  DbSet<Ticket> Tickets { get; }
  DbSet<global::Carwash.Domain.Personal.Lavador> Lavadores { get; }
  Task<int> SaveChangesAsync(CancellationToken ct = default);
}
