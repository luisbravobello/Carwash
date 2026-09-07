using System.Runtime.InteropServices;
using System.Text;
using Carwash.Application.Common.Interfaces;
using Carwash.Application.Tickets.Formato;

namespace Carwash.Infrastructure.Printing;

// OCP + DIP: si mañana cambias de Epson a otra, creas otra clase sin tocar casos de uso.
// TM-T20II: 80mm = 42 columnas aprox, ESC/POS enviado RAW por Winspool
// (funciona con la impresora instalada por USB, sin necesidad de compartirla).
public sealed class EpsonTmT20IIPrinter(string printerName = "EPSON TM-T20II", string? logoPath = null) : ITicketPrinter
{
  private readonly string? _logoPath = logoPath;

  public async Task<PrintResult> PrintAsync(TicketPrintModel m, CancellationToken ct = default)
  {
    var text = TicketTexto.Build(m);
    var sb = new StringBuilder(text);
    if (!string.IsNullOrWhiteSpace(_logoPath)) sb.Insert(0, Center("[LOGO]", 42) + Environment.NewLine);
    sb.AppendLine("\n\n\n"); // avance para cortar

    // 1) Siempre guarda copia .txt (auditoría aunque no haya impresora)
    var dir = Path.Combine(AppContext.BaseDirectory, "tickets");
    Directory.CreateDirectory(dir);
    await File.WriteAllTextAsync(Path.Combine(dir, $"ticket-{m.Numero}.txt"), sb.ToString(), ct);

    // 2) Envío RAW directo a la impresora instalada.
    var bytes = BuildEscPos(sb.ToString());
    await File.WriteAllBytesAsync(Path.Combine(dir, $"ticket-{m.Numero}.bin"), bytes, ct);

    List<string> instaladas;
    try { instaladas = ListPrinters(); }
    catch (Exception ex) { return new PrintResult(false, "No se pudieron listar impresoras: " + ex.GetBaseException().Message); }
    if (instaladas.Count == 0)
      return new PrintResult(false, "Windows no tiene ninguna impresora instalada. Instala el driver de la Epson.");

    var destino = instaladas.FirstOrDefault(n => n.Equals(printerName, StringComparison.OrdinalIgnoreCase))
      ?? instaladas.FirstOrDefault(n => n.Contains("EPSON", StringComparison.OrdinalIgnoreCase))
      ?? instaladas.FirstOrDefault(n => n.Contains("TM-T20", StringComparison.OrdinalIgnoreCase))
      ?? instaladas.FirstOrDefault(n => n.Contains("TICKET", StringComparison.OrdinalIgnoreCase) || n.Contains("POS", StringComparison.OrdinalIgnoreCase))
      ?? instaladas[0];

    if (!RawPrinter.SendBytes(destino, bytes, $"Ticket-{m.Numero}", out var err))
      return new PrintResult(false, $"Falló envío a '{destino}': {err}. Instaladas: {string.Join(", ", instaladas)}.");
    return new PrintResult(true, $"Impreso en '{destino}'.");
  }

  private static string Center(string s, int w)
  {
    if (s.Length >= w) return s[..w];
    var pad = (w - s.Length) / 2;
    return new string(' ', pad) + s;
  }

  private static byte[] BuildEscPos(string text)
  {
    using var ms = new MemoryStream();
    ms.Write([0x1B, 0x40]); // init (alineación izquierda por defecto)
    ms.Write(Encoding.Latin1.GetBytes(text)); // el centrado ya viene con espacios
    ms.Write([0x1D, 0x56, 0x01]); // corte parcial TM-T20II
    return ms.ToArray();
  }

  private static List<string> ListPrinters()
  {
    const int PRINTER_ENUM_LOCAL = 0x00000002;
    const int PRINTER_ENUM_CONNECTIONS = 0x00000004;
    var outList = new List<string>();
    if (!RawPrinter.EnumPrinters(PRINTER_ENUM_LOCAL | PRINTER_ENUM_CONNECTIONS, null, 1, IntPtr.Zero, 0, out var needed, out var count) && needed == 0)
      return outList;
    var buf = Marshal.AllocHGlobal((int)needed);
    try
    {
      if (!RawPrinter.EnumPrinters(PRINTER_ENUM_LOCAL | PRINTER_ENUM_CONNECTIONS, null, 1, buf, needed, out _, out count))
        return outList;
      var size = Marshal.SizeOf<RawPrinter.PRINTER_INFO_1>();
      for (var i = 0; i < count; i++)
      {
        var info = Marshal.PtrToStructure<RawPrinter.PRINTER_INFO_1>(buf + i * size);
        if (!string.IsNullOrWhiteSpace(info.pName)) outList.Add(info.pName);
      }
    }
    finally { Marshal.FreeHGlobal(buf); }
    return outList;
  }
}

internal static class RawPrinter
{
  [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
  public struct PRINTER_INFO_1
  {
    public int Flags;
    public string pDescription;
    public string pName;
    public string pComment;
  }

  [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
  private struct DOC_INFO_1
  {
    [MarshalAs(UnmanagedType.LPTStr)] public string pDocName;
    [MarshalAs(UnmanagedType.LPTStr)] public string? pOutputFile;
    [MarshalAs(UnmanagedType.LPTStr)] public string? pDatatype;
  }

  [DllImport("winspool.drv", CharSet = CharSet.Auto, SetLastError = true)]
  public static extern bool EnumPrinters(int flags, string? name, uint level, IntPtr pPrinterEnum, uint cbBuf, out uint pcbNeeded, out uint pcReturned);

  [DllImport("winspool.drv", CharSet = CharSet.Auto, SetLastError = true)]
  private static extern bool OpenPrinter(string pPrinterName, out IntPtr phPrinter, IntPtr pDefault);

  [DllImport("winspool.drv", SetLastError = true)]
  private static extern bool ClosePrinter(IntPtr hPrinter);

  [DllImport("winspool.drv", CharSet = CharSet.Auto, SetLastError = true)]
  private static extern bool StartDocPrinter(IntPtr hPrinter, int level, [In] ref DOC_INFO_1 pDocInfo);

  [DllImport("winspool.drv", SetLastError = true)]
  private static extern bool EndDocPrinter(IntPtr hPrinter);

  [DllImport("winspool.drv", SetLastError = true)]
  private static extern bool StartPagePrinter(IntPtr hPrinter);

  [DllImport("winspool.drv", SetLastError = true)]
  private static extern bool EndPagePrinter(IntPtr hPrinter);

  [DllImport("winspool.drv", SetLastError = true)]
  private static extern bool WritePrinter(IntPtr hPrinter, IntPtr pBytes, int dwCount, out int dwWritten);

  public static bool SendBytes(string printerName, byte[] bytes, string docName, out string error)
  {
    error = "";
    var unmanaged = Marshal.AllocHGlobal(bytes.Length);
    try
    {
      Marshal.Copy(bytes, 0, unmanaged, bytes.Length);
      if (!OpenPrinter(printerName, out var h, IntPtr.Zero))
      {
        error = "no se pudo abrir (código " + Marshal.GetLastWin32Error() + ")";
        return false;
      }
      try
      {
        var doc = new DOC_INFO_1 { pDocName = docName, pDatatype = "RAW" };
        if (!StartDocPrinter(h, 1, ref doc)) { error = "no inicia documento (código " + Marshal.GetLastWin32Error() + ")"; return false; }
        try
        {
          if (!StartPagePrinter(h)) { error = "no inicia página"; return false; }
          if (!WritePrinter(h, unmanaged, bytes.Length, out var written) || written != bytes.Length)
          {
            error = $"solo escribió {written}/{bytes.Length} bytes";
            return false;
          }
          EndPagePrinter(h);
        }
        finally { EndDocPrinter(h); }
      }
      finally { ClosePrinter(h); }
      return true;
    }
    finally { Marshal.FreeHGlobal(unmanaged); }
  }
}
