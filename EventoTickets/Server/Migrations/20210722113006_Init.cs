using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace EventoTickets.Server.Migrations
{
    public partial class Init : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Eventos",
                columns: table => new
                {
                    EventoId = table.Column<string>(type: "VARCHAR(50)", maxLength: 50, nullable: false),
                    NomeEvento = table.Column<string>(type: "VARCHAR(50)", maxLength: 100, nullable: true),
                    Descricao = table.Column<string>(type: "VARCHAR(50)", maxLength: 200, nullable: true),
                    DataRealizacao = table.Column<DateTime>(type: "DATETIME", nullable: false),
                    EventoPadrao = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Eventos", x => x.EventoId);
                });

            migrationBuilder.CreateTable(
                name: "Taloes",
                columns: table => new
                {
                    TalaoId = table.Column<string>(type: "VARCHAR(50)", maxLength: 50, nullable: false),
                    EventoId = table.Column<string>(type: "VARCHAR(50)", nullable: false),
                    NumeroTalao = table.Column<int>(type: "INTEGER", nullable: false),
                    ResponsavelTalao = table.Column<string>(type: "VARCHAR(50)", maxLength: 50, nullable: true),
                    DataEntrega = table.Column<DateTime>(type: "DATE", nullable: true),
                    NumeroInicial = table.Column<int>(type: "INTEGER", nullable: false),
                    NumeroFinal = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Taloes", x => x.TalaoId);
                    table.ForeignKey(
                        name: "FK_Taloes_Eventos_EventoId",
                        column: x => x.EventoId,
                        principalTable: "Eventos",
                        principalColumn: "EventoId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Tickets",
                columns: table => new
                {
                    TicketId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EventoId = table.Column<string>(type: "VARCHAR(50)", maxLength: 50, nullable: false),
                    TalaoId = table.Column<string>(type: "VARCHAR(50)", maxLength: 50, nullable: false),
                    NumeroTicket = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    DataConfirmacao = table.Column<DateTime>(type: "DATETIME", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tickets", x => x.TicketId);
                    table.ForeignKey(
                        name: "FK_Tickets_Eventos_EventoId",
                        column: x => x.EventoId,
                        principalTable: "Eventos",
                        principalColumn: "EventoId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Tickets_Taloes_TalaoId",
                        column: x => x.TalaoId,
                        principalTable: "Taloes",
                        principalColumn: "TalaoId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Taloes_EventoId_NumeroTalao",
                table: "Taloes",
                columns: new[] { "EventoId", "NumeroTalao" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_EventoId",
                table: "Tickets",
                column: "EventoId");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_EventoId_NumeroTicket",
                table: "Tickets",
                columns: new[] { "EventoId", "NumeroTicket" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_Status",
                table: "Tickets",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_TalaoId",
                table: "Tickets",
                column: "TalaoId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Tickets");

            migrationBuilder.DropTable(
                name: "Taloes");

            migrationBuilder.DropTable(
                name: "Eventos");
        }
    }
}
