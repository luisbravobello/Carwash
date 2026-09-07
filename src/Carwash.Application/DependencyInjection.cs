using Carwash.Application.Caja.CuadreDelDia;
using Carwash.Application.Catalogo.Productos;
using Carwash.Application.Negocio.Config;
using Carwash.Application.Personal.Lavadores;
using Carwash.Application.Tickets.AnularTicket;
using Carwash.Application.Tickets.CobrarPendiente;
using Carwash.Application.Tickets.CrearTicket;
using Carwash.Application.Tickets.Historial;
using Carwash.Application.Usuarios.Gestion;
using Carwash.Application.Usuarios.Login;
using Microsoft.Extensions.DependencyInjection;

namespace Carwash.Application;

// Ciclos de vida Application: handlers Transient (nuevos cada venta, sin estado).
public static class DependencyInjection
{
  public static IServiceCollection AddApplication(this IServiceCollection services)
  {
    services.AddTransient<CrearTicketHandler>();
    services.AddTransient<AnularTicketHandler>();
    services.AddTransient<MarcarPagadoHandler>();
    services.AddTransient<CuadreDelDiaHandler>();
    services.AddTransient<CrearProductoHandler>();
    services.AddTransient<ActualizarProductoHandler>();
    services.AddTransient<ListarProductosHandler>();
    services.AddTransient<HistorialDelDiaHandler>();
    services.AddTransient<DetalleTicketHandler>();
    services.AddTransient<ObtenerNegocioHandler>();
    services.AddTransient<ActualizarNegocioHandler>();
    services.AddTransient<IniciarSesionHandler>();
    services.AddTransient<ListarUsuariosHandler>();
    services.AddTransient<CrearUsuarioHandler>();
    services.AddTransient<ActualizarUsuarioHandler>();
    services.AddTransient<CambiarClaveHandler>();
    services.AddTransient<CambiarEstadoUsuarioHandler>();
    services.AddTransient<ListarLavadoresHandler>();
    services.AddTransient<CrearLavadorHandler>();
    services.AddTransient<ActualizarLavadorHandler>();
    services.AddTransient<CambiarEstadoLavadorHandler>();
    return services;
  }
}
