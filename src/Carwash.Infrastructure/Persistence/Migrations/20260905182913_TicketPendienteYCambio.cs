using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Carwash.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class TicketPendienteYCambio : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Ticket_Estado",
                table: "Tickets");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Ticket_Estado",
                table: "Tickets",
                sql: "Estado IN ('V','P','A')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Ticket_Estado",
                table: "Tickets");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Ticket_Estado",
                table: "Tickets",
                sql: "Estado IN ('V','A')");
        }
    }
}
