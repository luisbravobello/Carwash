using Carwash.Domain.Common;

namespace Carwash.Domain.Tickets;

public static class TicketEstado
{
  public const string Valido = "V";     // pagado
  public const string Pendiente = "P";  // se lleva el carro después, paga al retirar
  public const string Anulado = "A";
}

// Entidad con comportamiento, no anémica. SRP: solo reglas del ticket.
public class Ticket : Entity
{
  private readonly List<TicketDetalle> _detalles = [];
  private Ticket() { }

  public int Numero { get; private set; }
  public DateTime Fecha { get; private set; } = DateTime.Now;
  public int UsuarioId { get; private set; }
  public int? VehiculoId { get; private set; }
  public decimal Subtotal { get; private set; }
  public decimal Total { get; private set; }
  public int MetodoPagoId { get; private set; }
  public string? ReferenciaTransfer { get; private set; }
  public string? ClienteNombre { get; private set; }
  public string? VehiculoDescripcion { get; private set; }
  public int? LavadorId { get; private set; }
  public string? LavadorNombre { get; private set; }
  public string Estado { get; private set; } = TicketEstado.Valido;
  public IReadOnlyList<TicketDetalle> Detalles => _detalles.AsReadOnly();

  public static Result<Ticket> Crear(int numero, int usuarioId, int metodoPagoId, string? referenciaTransfer, int? vehiculoId = null, string? clienteNombre = null, string? vehiculoDescripcion = null, bool pendiente = false, int? lavadorId = null, string? lavadorNombre = null)
  {
    if (numero <= 0) return Result<Ticket>.Failure("Número inválido");
    var t = new Ticket { Numero = numero, UsuarioId = usuarioId, MetodoPagoId = metodoPagoId, ReferenciaTransfer = referenciaTransfer, VehiculoId = vehiculoId, ClienteNombre = clienteNombre?.Trim(), VehiculoDescripcion = vehiculoDescripcion?.Trim(), LavadorId = lavadorId, LavadorNombre = lavadorNombre?.Trim(), Fecha = DateTime.Now, Estado = pendiente ? TicketEstado.Pendiente : TicketEstado.Valido };
    return Result<Ticket>.Success(t);
  }

  public Result AgregarDetalle(int productoId, string productoNombre, int cantidad, decimal precioUnitario)
  {
    if (Estado == TicketEstado.Anulado) return Result.Failure("Ticket anulado");
    if (cantidad <= 0) return Result.Failure("Cantidad inválida");
    if (precioUnitario < 0) return Result.Failure("Precio inválido");
    _detalles.Add(TicketDetalle.Crear(productoId, cantidad, precioUnitario));
    Recalcular();
    return Result.Success();
  }

  private void Recalcular()
  {
    Subtotal = _detalles.Sum(d => d.Cantidad * d.PrecioUnitario);
    Total = Subtotal; // descuento futuro: OCP, se extiende sin romper
  }

  public Result Anular()
  {
    if (Estado == TicketEstado.Anulado) return Result.Failure("Ya anulado");
    Estado = TicketEstado.Anulado;
    return Result.Success();
  }

  // Cuando el cliente vuelve a retirar y paga.
  public Result MarcarPagado(int metodoPagoId, string? referenciaTransfer)
  {
    if (Estado != TicketEstado.Pendiente) return Result.Failure("Solo pendientes se pueden cobrar.");
    MetodoPagoId = metodoPagoId;
    ReferenciaTransfer = referenciaTransfer;
    Estado = TicketEstado.Valido;
    return Result.Success();
  }

  // Cambio a devolver en efectivo. Devuelve null si no alcanza.
  public decimal? CalcularCambio(decimal recibido)
  {
    if (recibido < Total) return null;
    return recibido - Total;
  }
}

public class TicketDetalle : Entity
{
  private TicketDetalle() { }
  public int TicketId { get; set; }
  public int ProductoServicioId { get; private set; }
  public int Cantidad { get; private set; }
  public decimal PrecioUnitario { get; private set; }

  public static TicketDetalle Crear(int productoId, int cantidad, decimal precio)
    => new() { ProductoServicioId = productoId, Cantidad = cantidad, PrecioUnitario = precio };
}
