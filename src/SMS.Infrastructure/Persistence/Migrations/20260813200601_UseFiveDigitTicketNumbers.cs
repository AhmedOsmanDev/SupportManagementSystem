using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SMS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UseFiveDigitTicketNumbers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Comments_Tickets_TicketNumber",
                table: "Comments");

            migrationBuilder.DropForeignKey(
                name: "FK_TicketActivities_Tickets_TicketNumber",
                table: "TicketActivities");

            migrationBuilder.DropForeignKey(
                name: "FK_TimeEntries_Tickets_TicketNumber",
                table: "TimeEntries");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Tickets",
                table: "Tickets");

            migrationBuilder.DropIndex(
                name: "IX_Comments_TicketNumber_CreatedAt",
                table: "Comments");

            migrationBuilder.DropIndex(
                name: "IX_TicketActivities_TicketNumber_CreatedAt",
                table: "TicketActivities");

            migrationBuilder.DropIndex(
                name: "IX_TimeEntries_TicketNumber_WorkDate",
                table: "TimeEntries");

            migrationBuilder.Sql(
                """
                IF (SELECT COUNT_BIG(*) FROM [Tickets]) > 99999
                    THROW 50001, 'Ticket numbers cannot be converted because the database contains more than 99,999 tickets.', 1;

                CREATE TABLE #TicketNumberMap
                (
                    [OldNumber] nvarchar(32) NOT NULL PRIMARY KEY,
                    [NewNumber] char(5) NOT NULL UNIQUE
                );

                INSERT INTO #TicketNumberMap ([OldNumber], [NewNumber])
                SELECT [Number], CONVERT(char(5), [Number])
                FROM [Tickets]
                WHERE LEN([Number]) = 5
                  AND [Number] NOT LIKE '%[^0-9]%'
                  AND [Number] <> '00000';

                ;WITH Digits AS
                (
                    SELECT [Value]
                    FROM (VALUES (0), (1), (2), (3), (4), (5), (6), (7), (8), (9)) values_table([Value])
                ),
                CandidateNumbers AS
                (
                    SELECT ones.[Value]
                         + tens.[Value] * 10
                         + hundreds.[Value] * 100
                         + thousands.[Value] * 1000
                         + tenThousands.[Value] * 10000 AS [Value]
                    FROM Digits ones
                    CROSS JOIN Digits tens
                    CROSS JOIN Digits hundreds
                    CROSS JOIN Digits thousands
                    CROSS JOIN Digits tenThousands
                ),
                AvailableNumbers AS
                (
                    SELECT candidate.[Value], ROW_NUMBER() OVER (ORDER BY candidate.[Value]) AS [RowNumber]
                    FROM CandidateNumbers candidate
                    WHERE candidate.[Value] BETWEEN 1 AND 99999
                      AND NOT EXISTS
                      (
                          SELECT 1
                          FROM #TicketNumberMap map
                          WHERE map.[NewNumber] = RIGHT('00000' + CONVERT(varchar(5), candidate.[Value]), 5)
                      )
                ),
                LegacyTickets AS
                (
                    SELECT ticket.[Number] AS [OldNumber],
                           ROW_NUMBER() OVER (ORDER BY ticket.[CreatedAt], ticket.[Number]) AS [RowNumber]
                    FROM [Tickets] ticket
                    WHERE NOT EXISTS
                    (
                        SELECT 1
                        FROM #TicketNumberMap map
                        WHERE map.[OldNumber] = ticket.[Number]
                    )
                )
                INSERT INTO #TicketNumberMap ([OldNumber], [NewNumber])
                SELECT legacy.[OldNumber], RIGHT('00000' + CONVERT(varchar(5), available.[Value]), 5)
                FROM LegacyTickets legacy
                INNER JOIN AvailableNumbers available ON available.[RowNumber] = legacy.[RowNumber];

                UPDATE child
                SET child.[TicketNumber] = map.[NewNumber]
                FROM [Comments] child
                INNER JOIN #TicketNumberMap map ON map.[OldNumber] = child.[TicketNumber];

                UPDATE child
                SET child.[TicketNumber] = map.[NewNumber]
                FROM [TicketActivities] child
                INNER JOIN #TicketNumberMap map ON map.[OldNumber] = child.[TicketNumber];

                UPDATE child
                SET child.[TicketNumber] = map.[NewNumber]
                FROM [TimeEntries] child
                INNER JOIN #TicketNumberMap map ON map.[OldNumber] = child.[TicketNumber];

                UPDATE ticket
                SET ticket.[Number] = map.[NewNumber]
                FROM [Tickets] ticket
                INNER JOIN #TicketNumberMap map ON map.[OldNumber] = ticket.[Number];
                """);

            migrationBuilder.AlterColumn<string>(
                name: "TicketNumber",
                table: "TimeEntries",
                type: "char(5)",
                unicode: false,
                fixedLength: true,
                maxLength: 5,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(32)",
                oldMaxLength: 32);

            migrationBuilder.AlterColumn<string>(
                name: "Number",
                table: "Tickets",
                type: "char(5)",
                unicode: false,
                fixedLength: true,
                maxLength: 5,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(32)",
                oldMaxLength: 32);

            migrationBuilder.AlterColumn<string>(
                name: "TicketNumber",
                table: "TicketActivities",
                type: "char(5)",
                unicode: false,
                fixedLength: true,
                maxLength: 5,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(32)",
                oldMaxLength: 32);

            migrationBuilder.AlterColumn<string>(
                name: "TicketNumber",
                table: "Comments",
                type: "char(5)",
                unicode: false,
                fixedLength: true,
                maxLength: 5,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(32)",
                oldMaxLength: 32);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Tickets_Number_FiveDigits",
                table: "Tickets",
                sql: "LEN([Number]) = 5 AND [Number] NOT LIKE '%[^0-9]%'");

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
                DECLARE @NextTicketNumber int;
                ;WITH Digits AS
                (
                    SELECT [Value]
                    FROM (VALUES (0), (1), (2), (3), (4), (5), (6), (7), (8), (9)) values_table([Value])
                ),
                CandidateNumbers AS
                (
                    SELECT ones.[Value]
                         + tens.[Value] * 10
                         + hundreds.[Value] * 100
                         + thousands.[Value] * 1000
                         + tenThousands.[Value] * 10000 AS [Value]
                    FROM Digits ones
                    CROSS JOIN Digits tens
                    CROSS JOIN Digits hundreds
                    CROSS JOIN Digits thousands
                    CROSS JOIN Digits tenThousands
                )
                SELECT @NextTicketNumber = MIN(candidate.[Value])
                FROM CandidateNumbers candidate
                WHERE candidate.[Value] BETWEEN 1 AND 99999
                  AND NOT EXISTS
                  (
                      SELECT 1
                      FROM [Tickets] ticket
                      WHERE ticket.[Number] = RIGHT('00000' + CONVERT(varchar(5), candidate.[Value]), 5)
                  );

                IF @NextTicketNumber IS NOT NULL
                BEGIN
                    DECLARE @CreateSequenceSql nvarchar(max) = N'CREATE SEQUENCE [dbo].[TicketNumberSequence] AS int START WITH '
                        + CONVERT(nvarchar(10), @NextTicketNumber)
                        + N' INCREMENT BY 1 MINVALUE 1 MAXVALUE 99999 NO CYCLE;';
                    EXEC sys.sp_executesql @CreateSequenceSql;
                END
                ELSE
                BEGIN
                    CREATE SEQUENCE [dbo].[TicketNumberSequence] AS int
                        START WITH 99999 INCREMENT BY 1 MINVALUE 1 MAXVALUE 99999 NO CYCLE;
                    DECLARE @ConsumedTicketNumber int = NEXT VALUE FOR [dbo].[TicketNumberSequence];
                END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Tickets_Number_FiveDigits",
                table: "Tickets");

            migrationBuilder.DropSequence(
                name: "TicketNumberSequence");

            migrationBuilder.DropForeignKey(
                name: "FK_Comments_Tickets_TicketNumber",
                table: "Comments");

            migrationBuilder.DropForeignKey(
                name: "FK_TicketActivities_Tickets_TicketNumber",
                table: "TicketActivities");

            migrationBuilder.DropForeignKey(
                name: "FK_TimeEntries_Tickets_TicketNumber",
                table: "TimeEntries");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Tickets",
                table: "Tickets");

            migrationBuilder.DropIndex(
                name: "IX_Comments_TicketNumber_CreatedAt",
                table: "Comments");

            migrationBuilder.DropIndex(
                name: "IX_TicketActivities_TicketNumber_CreatedAt",
                table: "TicketActivities");

            migrationBuilder.DropIndex(
                name: "IX_TimeEntries_TicketNumber_WorkDate",
                table: "TimeEntries");

            migrationBuilder.AlterColumn<string>(
                name: "TicketNumber",
                table: "TimeEntries",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "char(5)",
                oldUnicode: false,
                oldFixedLength: true,
                oldMaxLength: 5);

            migrationBuilder.AlterColumn<string>(
                name: "Number",
                table: "Tickets",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "char(5)",
                oldUnicode: false,
                oldFixedLength: true,
                oldMaxLength: 5);

            migrationBuilder.AlterColumn<string>(
                name: "TicketNumber",
                table: "TicketActivities",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "char(5)",
                oldUnicode: false,
                oldFixedLength: true,
                oldMaxLength: 5);

            migrationBuilder.AlterColumn<string>(
                name: "TicketNumber",
                table: "Comments",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "char(5)",
                oldUnicode: false,
                oldFixedLength: true,
                oldMaxLength: 5);

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
        }
    }
}
