using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Carwash.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class QuitarReferenciaObligatoria : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Ticket_RefTransf",
                table: "Tickets");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddCheckConstraint(
                name: "CK_Ticket_RefTransf",
                table: "Tickets",
                sql: "MetodoPagoId <> 2 OR ReferenciaTransfer IS NOT NULL");
        }
    }
}
