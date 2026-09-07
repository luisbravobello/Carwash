using Carwash.Application.Common.Interfaces;
using Carwash.Infrastructure.Persistence;
using Carwash.Infrastructure.Printing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Carwash.Infrastructure;

public static class DependencyInjection
{
  public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString, string printerName = "EPSON TM-T20II")
  {
    services.AddDbContext<CarwashDbContext>(o => o.UseSqlServer(connectionString));
    services.AddScoped<ICarwashDbContext>(sp => sp.GetRequiredService<CarwashDbContext>());
    services.AddSingleton<ITicketPrinter>(_ => new EpsonTmT20IIPrinter(printerName));
    return services;
  }
}
