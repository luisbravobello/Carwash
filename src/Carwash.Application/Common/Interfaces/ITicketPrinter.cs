namespace Carwash.Application.Common.Interfaces;

// DIP: el caso de uso pide imprimir sin saber si es Epson, PDF o WhatsApp.
// PrintResult dice la verdad: Ok=false con el motivo (ej: impresora no compartida).
public record PrintResult(bool Ok, string Detalle);

public interface ITicketPrinter
{
  Task<PrintResult> PrintAsync(TicketPrintModel model, CancellationToken ct = default);
}

public record TicketPrintModel(
  string NombreNegocio,
  string? Telefono,
  string? Direccion,
  int Numero,
  DateTime Fecha,
  string? Placa,
  string? ClienteNombre,
  string? VehiculoDescripcion,
  string? LavadorNombre,
  List<TicketPrintLine> Lineas,
  decimal Total,
  string MetodoPago,
  string? Referencia,
  string Estado = "V");

public record TicketPrintLine(string Producto, int Cantidad, decimal Precio, decimal Subtotal);
