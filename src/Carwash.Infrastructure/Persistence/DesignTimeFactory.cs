using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Carwash.Infrastructure.Persistence;

// Solo para `dotnet ef migrations`. En runtime se usa DI con el connection string real.
public sealed class CarwashDbContextFactory : IDesignTimeDbContextFactory<CarwashDbContext>
{
  public CarwashDbContext CreateDbContext(string[] args)
  {
    var cs = "Server=(localdb)\\MSSQLLocalDB;Database=CarwashDB;Trusted_Connection=True;TrustServerCertificate=True";
    var opt = new DbContextOptionsBuilder<CarwashDbContext>().UseSqlServer(cs).Options;
    return new CarwashDbContext(opt);
  }
}
