using System.Globalization;
using System.Text;
using Carwash.Application.Common.Interfaces;

namespace Carwash.Application.Tickets.Formato;

// Formato térmico clásico 80mm (42 cols), ASCII seguro para Epson ESC/POS.
// Una sola fuente de verdad: impresora y vista previa usan este mismo texto.
public static class TicketTexto
{
  public static string Build(TicketPrintModel m)
  {
    var sb = new StringBuilder();
    sb.AppendLine(Center("*** " + m.NombreNegocio.ToUpper() + " ***", 42));
    if (!string.IsNullOrWhiteSpace(m.Telefono)) sb.AppendLine(Center("TEL: " + m.Telefono, 42));
    if (!string.IsNullOrWhiteSpace(m.Direccion)) sb.AppendLine(Center(m.Direccion.ToUpper(), 42));
    sb.AppendLine(Dash());
    sb.AppendLine($"#TICKET {m.Numero:000000} - {m.Fecha:dd/MM/yyyy HH:mm:ss}");
    sb.AppendLine(Dash());
    sb.AppendLine(Dash());
    sb.AppendLine("CANT " + "DESCRIPCION".PadRight(24) + "TOTAL".PadLeft(9));
    foreach (var l in m.Lineas)
      sb.AppendLine($"{l.Cantidad,3} {Trunc(l.Producto.ToUpper(), 24),-24} {l.Subtotal,9:N2}");
    sb.AppendLine(Dash());
    sb.AppendLine(Row("SUB TOTAL", m.Total));
    sb.AppendLine(Row("TOTAL", m.Total));
    sb.AppendLine(Dash());
    if (!string.IsNullOrWhiteSpace(m.ClienteNombre)) sb.AppendLine("CLIENTE: " + m.ClienteNombre.ToUpper());
    if (!string.IsNullOrWhiteSpace(m.VehiculoDescripcion)) sb.AppendLine("VEHICULO: " + m.VehiculoDescripcion.ToUpper());
    if (!string.IsNullOrWhiteSpace(m.LavadorNombre)) sb.AppendLine("LAVADOR: " + m.LavadorNombre.ToUpper());
    if (!string.IsNullOrWhiteSpace(m.Placa)) sb.AppendLine("PLACA: " + m.Placa);
    sb.AppendLine("PAGO: " + m.MetodoPago.ToUpper() + (m.Referencia is not null ? " REF:" + m.Referencia : ""));
    if (m.Estado == "P") sb.AppendLine(Center("*** PENDIENTE DE PAGO ***", 42));
    if (m.Estado == "A") sb.AppendLine(Center("*** ANULADO ***", 42));
    sb.AppendLine(Dash());
    sb.AppendLine(Center("GRACIAS POR SU VISITA", 42));
    return Ascii(sb.ToString());
  }

  // La TM-T20II habla otra página de códigos: las tildes salen como "?".
  // Se normaliza a ASCII (BASICO, a. m.) para que papel y preview coincidan.
  private static string Ascii(string s)
  {
    var sb = new StringBuilder();
    foreach (var c in s.Normalize(NormalizationForm.FormD))
      if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
        sb.Append(c);
    var clean = new StringBuilder();
    foreach (var c in sb.ToString().Normalize(NormalizationForm.FormC))
      clean.Append(c == '\u00A0' ? ' ' : c <= 126 ? c : '?');
    return clean.ToString();
  }

  private static string Dash() => new('-', 42);
  private static string Row(string label, decimal v)
  {
    var val = v.ToString("N2");
    return label + new string(' ', Math.Max(1, 42 - label.Length - val.Length)) + val;
  }
  private static string Center(string s, int w)
  {
    if (s.Length >= w) return s[..w];
    return new string(' ', (w - s.Length) / 2) + s;
  }
  private static string Trunc(string s, int n) => s.Length <= n ? s : s[..n];
}
