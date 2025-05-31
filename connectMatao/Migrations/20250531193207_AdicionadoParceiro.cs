using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace connectMatao.Migrations
{
    /// <inheritdoc />
    public partial class AdicionadoParceiro : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FormsUsuarioParceiro");

            migrationBuilder.CreateTable(
                name: "TB_Parceiro",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    UsuarioId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    NomeCompleto = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Cpf = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Telefone = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FlagAprovado = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DataEnvio = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TB_Parceiro", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TB_Parceiro_TB_Usuario_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "TB_Usuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_TB_Parceiro_UsuarioId",
                table: "TB_Parceiro",
                column: "UsuarioId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TB_Parceiro");

            migrationBuilder.CreateTable(
                name: "FormsUsuarioParceiro",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    UsuarioId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Cpf = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DataEnvio = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    FlagAprovado = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    NomeCompleto = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Telefone = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FormsUsuarioParceiro", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FormsUsuarioParceiro_TB_Usuario_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "TB_Usuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_FormsUsuarioParceiro_UsuarioId",
                table: "FormsUsuarioParceiro",
                column: "UsuarioId",
                unique: true);
        }
    }
}
