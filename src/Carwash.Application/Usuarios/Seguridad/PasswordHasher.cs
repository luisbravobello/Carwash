using System.Security.Cryptography;
using System.Text;

namespace Carwash.Application.Usuarios.Seguridad;

// SRP: solo hashing. SHA256 local (negocio pequeño, PC local). Sin dependencias externas.
public static class PasswordHasher
{
  public static string Hash(string password)
    => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"carwash::{password}")));
  public static bool Verify(string password, string hash)
    => Hash(password) == hash;
}
