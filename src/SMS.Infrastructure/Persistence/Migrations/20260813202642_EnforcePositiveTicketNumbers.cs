using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SMS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EnforcePositiveTicketNumbers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Tickets_Number_FiveDigits",
                table: "Tickets");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Tickets_Number_FiveDigits",
                table: "Tickets",
                sql: "LEN([Number]) = 5 AND [Number] NOT LIKE '%[^0-9]%' AND [Number] <> '00000'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Tickets_Number_FiveDigits",
                table: "Tickets");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Tickets_Number_FiveDigits",
                table: "Tickets",
                sql: "LEN([Number]) = 5 AND [Number] NOT LIKE '%[^0-9]%'");
        }
    }
}
