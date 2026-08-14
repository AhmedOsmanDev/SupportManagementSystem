using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SMS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ConvertTicketNumbersToIntegers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF EXISTS (SELECT 1 FROM [Tickets] WHERE TRY_CONVERT(int, [Number]) IS NULL OR TRY_CONVERT(int, [Number]) <= 0)
                    THROW 50003, 'Tickets contain a number that cannot be converted to a positive integer.', 1;
                IF EXISTS (SELECT 1 FROM [Comments] WHERE TRY_CONVERT(int, [TicketNumber]) IS NULL OR TRY_CONVERT(int, [TicketNumber]) <= 0)
                    THROW 50004, 'Comments contain a ticket number that cannot be converted to a positive integer.', 1;
                IF EXISTS (SELECT 1 FROM [TicketActivities] WHERE TRY_CONVERT(int, [TicketNumber]) IS NULL OR TRY_CONVERT(int, [TicketNumber]) <= 0)
                    THROW 50005, 'Ticket activities contain a ticket number that cannot be converted to a positive integer.', 1;
                IF EXISTS (SELECT 1 FROM [TimeEntries] WHERE TRY_CONVERT(int, [TicketNumber]) IS NULL OR TRY_CONVERT(int, [TicketNumber]) <= 0)
                    THROW 50006, 'Time entries contain a ticket number that cannot be converted to a positive integer.', 1;
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_Comments_Tickets_TicketNumber",
                table: "Comments");

            migrationBuilder.DropForeignKey(
                name: "FK_TicketActivities_Tickets_TicketNumber",
                table: "TicketActivities");

            migrationBuilder.DropForeignKey(
                name: "FK_TimeEntries_Tickets_TicketNumber",
                table: "TimeEntries");

            migrationBuilder.DropIndex(
                name: "IX_Comments_TicketNumber_CreatedAt",
                table: "Comments");

            migrationBuilder.DropIndex(
                name: "IX_TicketActivities_TicketNumber_CreatedAt",
                table: "TicketActivities");

            migrationBuilder.DropIndex(
                name: "IX_TimeEntries_TicketNumber_WorkDate",
                table: "TimeEntries");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Tickets",
                table: "Tickets");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Tickets_Number_FiveDigits",
                table: "Tickets");

            migrationBuilder.AlterColumn<int>(
                name: "TicketNumber",
                table: "TimeEntries",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "char(5)",
                oldUnicode: false,
                oldFixedLength: true,
                oldMaxLength: 5);

            migrationBuilder.AlterColumn<int>(
                name: "Number",
                table: "Tickets",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "char(5)",
                oldUnicode: false,
                oldFixedLength: true,
                oldMaxLength: 5);

            migrationBuilder.AlterColumn<int>(
                name: "TicketNumber",
                table: "TicketActivities",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "char(5)",
                oldUnicode: false,
                oldFixedLength: true,
                oldMaxLength: 5);

            migrationBuilder.AlterColumn<int>(
                name: "TicketNumber",
                table: "Comments",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "char(5)",
                oldUnicode: false,
                oldFixedLength: true,
                oldMaxLength: 5);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Tickets_Number_Positive",
                table: "Tickets",
                sql: "[Number] > 0");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Tickets",
                table: "Tickets",
                column: "Number");

            migrationBuilder.CreateIndex(
                name: "IX_Comments_TicketNumber_CreatedAt",
                table: "Comments",
                columns: new[] { "TicketNumber", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TicketActivities_TicketNumber_CreatedAt",
                table: "TicketActivities",
                columns: new[] { "TicketNumber", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TimeEntries_TicketNumber_WorkDate",
                table: "TimeEntries",
                columns: new[] { "TicketNumber", "WorkDate" });

            migrationBuilder.AddForeignKey(
                name: "FK_Comments_Tickets_TicketNumber",
                table: "Comments",
                column: "TicketNumber",
                principalTable: "Tickets",
                principalColumn: "Number",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TicketActivities_Tickets_TicketNumber",
                table: "TicketActivities",
                column: "TicketNumber",
                principalTable: "Tickets",
                principalColumn: "Number",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TimeEntries_Tickets_TicketNumber",
                table: "TimeEntries",
                column: "TicketNumber",
                principalTable: "Tickets",
                principalColumn: "Number",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.Sql(
                """
                DECLARE @SequenceWasExhausted bit =
                    (SELECT [is_exhausted] FROM sys.sequences WHERE [object_id] = OBJECT_ID(N'[dbo].[TicketNumberSequence]'));

                ALTER SEQUENCE [dbo].[TicketNumberSequence] NO MAXVALUE;

                IF @SequenceWasExhausted = 1
                    ALTER SEQUENCE [dbo].[TicketNumberSequence] RESTART WITH 100000;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF EXISTS (SELECT 1 FROM [Tickets] WHERE [Number] > 99999)
                    THROW 50007, 'Ticket numbers above 99999 cannot be downgraded to the former five-character format.', 1;

                IF EXISTS
                (
                    SELECT 1
                    FROM sys.sequences
                    WHERE [object_id] = OBJECT_ID(N'[dbo].[TicketNumberSequence]')
                      AND
                      (
                          TRY_CONVERT(bigint, [current_value]) > 99999
                          OR TRY_CONVERT(bigint, [last_used_value]) > 99999
                          OR ([last_used_value] IS NULL AND TRY_CONVERT(bigint, [start_value]) > 99999)
                      )
                )
                    THROW 50008, 'The ticket number sequence has advanced beyond 99999 and cannot be downgraded safely.', 1;
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_Comments_Tickets_TicketNumber",
                table: "Comments");

            migrationBuilder.DropForeignKey(
                name: "FK_TicketActivities_Tickets_TicketNumber",
                table: "TicketActivities");

            migrationBuilder.DropForeignKey(
                name: "FK_TimeEntries_Tickets_TicketNumber",
                table: "TimeEntries");

            migrationBuilder.DropIndex(
                name: "IX_Comments_TicketNumber_CreatedAt",
                table: "Comments");

            migrationBuilder.DropIndex(
                name: "IX_TicketActivities_TicketNumber_CreatedAt",
                table: "TicketActivities");

            migrationBuilder.DropIndex(
                name: "IX_TimeEntries_TicketNumber_WorkDate",
                table: "TimeEntries");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Tickets",
                table: "Tickets");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Tickets_Number_Positive",
                table: "Tickets");

            migrationBuilder.Sql(
                """
                ALTER TABLE [Comments] ALTER COLUMN [TicketNumber] varchar(5) NOT NULL;
                UPDATE [Comments] SET [TicketNumber] = RIGHT('00000' + [TicketNumber], 5);
                ALTER TABLE [Comments] ALTER COLUMN [TicketNumber] char(5) NOT NULL;

                ALTER TABLE [TicketActivities] ALTER COLUMN [TicketNumber] varchar(5) NOT NULL;
                UPDATE [TicketActivities] SET [TicketNumber] = RIGHT('00000' + [TicketNumber], 5);
                ALTER TABLE [TicketActivities] ALTER COLUMN [TicketNumber] char(5) NOT NULL;

                ALTER TABLE [TimeEntries] ALTER COLUMN [TicketNumber] varchar(5) NOT NULL;
                UPDATE [TimeEntries] SET [TicketNumber] = RIGHT('00000' + [TicketNumber], 5);
                ALTER TABLE [TimeEntries] ALTER COLUMN [TicketNumber] char(5) NOT NULL;

                ALTER TABLE [Tickets] ALTER COLUMN [Number] varchar(5) NOT NULL;
                UPDATE [Tickets] SET [Number] = RIGHT('00000' + [Number], 5);
                ALTER TABLE [Tickets] ALTER COLUMN [Number] char(5) NOT NULL;
                """);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Tickets_Number_FiveDigits",
                table: "Tickets",
                sql: "LEN([Number]) = 5 AND [Number] NOT LIKE '%[^0-9]%' AND [Number] <> '00000'");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Tickets",
                table: "Tickets",
                column: "Number");

            migrationBuilder.CreateIndex(
                name: "IX_Comments_TicketNumber_CreatedAt",
                table: "Comments",
                columns: new[] { "TicketNumber", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TicketActivities_TicketNumber_CreatedAt",
                table: "TicketActivities",
                columns: new[] { "TicketNumber", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TimeEntries_TicketNumber_WorkDate",
                table: "TimeEntries",
                columns: new[] { "TicketNumber", "WorkDate" });

            migrationBuilder.AddForeignKey(
                name: "FK_Comments_Tickets_TicketNumber",
                table: "Comments",
                column: "TicketNumber",
                principalTable: "Tickets",
                principalColumn: "Number",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TicketActivities_Tickets_TicketNumber",
                table: "TicketActivities",
                column: "TicketNumber",
                principalTable: "Tickets",
                principalColumn: "Number",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TimeEntries_Tickets_TicketNumber",
                table: "TimeEntries",
                column: "TicketNumber",
                principalTable: "Tickets",
                principalColumn: "Number",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.Sql(
                "ALTER SEQUENCE [dbo].[TicketNumberSequence] MAXVALUE 99999 NO CYCLE;");
        }
    }
}
