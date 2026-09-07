# Mi Carwash Punto de venta (WPF + .NET 10)

Sistema local de un solo puesto para carwash + bar: tickets en Epson TM-T20II,
caja diaria, pendientes (paga al retirar), catálogo, lavadores, usuarios y login.

## Stack

- .NET 10, WPF (WPF-UI Fluent), EF Core + SQL Server LocalDB (migraciones)
- Clean Architecture + DDD + SOLID (ver `src/`)

## Proyectos

- `src/Carwash.Domain` — entidades con comportamiento
- `src/Carwash.Application` — casos de uso
- `src/Carwash.Infrastructure` — EF Core, migraciones, impresión ESC/POS
- `src/Carwash.Desktop` — UI (proyecto de inicio)

## Correr

1. Abrir `Carwash.slnx` en Visual Studio 2026
2. `Carwash.Desktop` como inicio → F5 (migra y crea seed solo)
3. Entrar con `admin / 1234` (cambiar luego en Config → Usuarios)
