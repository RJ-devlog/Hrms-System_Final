using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRMS_System.Migrations
{
    /// <inheritdoc />
    public partial class AddEvaluationTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "OverallRating",
                table: "Evaluations",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(10)",
                oldMaxLength: 10,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Evaluations_UserId",
                table: "Evaluations",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Evaluations_UserInformation_UserId",
                table: "Evaluations",
                column: "UserId",
                principalTable: "UserInformation",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Evaluations_UserInformation_UserId",
                table: "Evaluations");

            migrationBuilder.DropIndex(
                name: "IX_Evaluations_UserId",
                table: "Evaluations");

            migrationBuilder.AlterColumn<string>(
                name: "OverallRating",
                table: "Evaluations",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);
        }
    }
}
