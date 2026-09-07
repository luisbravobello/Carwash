using Carwash.Domain.Caja;
using Carwash.Domain.Catalogo;
using Carwash.Domain.Negocio;
using Carwash.Domain.Tickets;
using Carwash.Domain.Vehiculos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Carwash.Infrastructure.Persistence.Configurations;

public class MetodoPagoConfiguration : IEntityTypeConfiguration<MetodoPago>
{
  public void Configure(EntityTypeBuilder<MetodoPago> e)
  {
    e.HasKey(x => x.Id);
    e.Property(x => x.Id).ValueGeneratedNever();
    e.Property(x => x.Nombre).HasMaxLength(20).IsRequired();
  }
}

public class TicketConfiguration : IEntityTypeConfiguration<Ticket>
{
  public void Configure(EntityTypeBuilder<Ticket> e)
  {
    e.HasKey(x => x.Id);
    e.HasIndex(x => x.Numero).IsUnique();
    e.Property(x => x.Subtotal).HasColumnType("decimal(12,2)");
    e.Property(x => x.Total).HasColumnType("decimal(12,2)");
    e.Property(x => x.Estado).HasMaxLength(1).IsFixedLength().IsRequired();
    e.Property(x => x.ReferenciaTransfer).HasMaxLength(50);
    e.Property(x => x.ClienteNombre).HasMaxLength(100);
    e.Property(x => x.VehiculoDescripcion).HasMaxLength(150);
    e.Property(x => x.LavadorNombre).HasMaxLength(100);
    e.HasMany(x => x.Detalles).WithOne().HasForeignKey(d => d.TicketId).OnDelete(DeleteBehavior.Restrict);
    e.ToTable(t =>
    {
      t.HasCheckConstraint("CK_Ticket_Total", "Total >= 0");
      t.HasCheckConstraint("CK_Ticket_Estado", "Estado IN ('V','P','A')");
    });
    e.Navigation(x => x.Detalles).HasField("_detalles").UsePropertyAccessMode(PropertyAccessMode.Field);
  }
}

public class TicketDetalleConfiguration : IEntityTypeConfiguration<TicketDetalle>
{
  public void Configure(EntityTypeBuilder<TicketDetalle> e)
  {
    e.HasKey(x => x.Id);
    e.Property(x => x.PrecioUnitario).HasColumnType("decimal(12,2)");
    e.ToTable(t =>
    {
      t.HasCheckConstraint("CK_Det_Cant", "Cantidad > 0");
      t.HasCheckConstraint("CK_Det_Precio", "PrecioUnitario >= 0");
    });
  }
}

public class VehiculoConfiguration : IEntityTypeConfiguration<Vehiculo>
{
  public void Configure(EntityTypeBuilder<Vehiculo> e)
  {
    e.HasKey(x => x.Id);
    e.HasIndex(x => x.Placa).IsUnique();
    e.Property(x => x.Placa).HasMaxLength(15).IsRequired();
  }
}

public class ProductoConfiguration : IEntityTypeConfiguration<ProductoServicio>
{
  public void Configure(EntityTypeBuilder<ProductoServicio> e)
  {
    e.HasKey(x => x.Id);
    e.Property(x => x.Precio).HasColumnType("decimal(12,2)");
    e.Property(x => x.Nombre).HasMaxLength(100).IsRequired();
    e.ToTable(t =>
    {
      t.HasCheckConstraint("CK_Prod_Precio", "Precio >= 0");
      t.HasCheckConstraint("CK_Prod_Stock", "Stock >= 0");
    });
  }
}

public class NegocioConfiguration : IEntityTypeConfiguration<Negocio>
{
  public void Configure(EntityTypeBuilder<Negocio> e)
  {
    e.HasKey(x => x.Id);
    e.Property(x => x.Id).ValueGeneratedNever(); // singleton Id=1 explícito
    e.Property(x => x.Nombre).HasMaxLength(100).IsRequired();
    e.ToTable(t => t.HasCheckConstraint("CK_Negocio_SingleRow", "Id = 1"));
  }
}

public class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
{
  public void Configure(EntityTypeBuilder<Usuario> e)
  {
    e.HasKey(x => x.Id);
    e.Property(x => x.Nombre).HasMaxLength(100).IsRequired();
    e.Property(x => x.Username).HasMaxLength(50).IsRequired();
    e.HasIndex(x => x.Username).IsUnique();
    e.Property(x => x.PasswordHash).HasMaxLength(128).IsRequired();
  }
}

public class LavadorConfiguration : IEntityTypeConfiguration<Carwash.Domain.Personal.Lavador>
{
  public void Configure(EntityTypeBuilder<Carwash.Domain.Personal.Lavador> e)
  {
    e.HasKey(x => x.Id);
    e.Property(x => x.Nombre).HasMaxLength(100).IsRequired();
  }
}
