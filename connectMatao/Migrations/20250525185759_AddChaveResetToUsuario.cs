using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace connectMatao.Migrations
{
    /// <inheritdoc />
    public partial class AddChaveResetToUsuario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ChaveReset",
                table: "TB_Usuario",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ChaveReset",
                table: "TB_Usuario");
        }
    }
}
