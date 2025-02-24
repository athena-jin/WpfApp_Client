using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Machines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    StrConnectType = table.Column<string>(type: "TEXT", nullable: false),
                    Endpoints = table.Column<string>(type: "TEXT", nullable: false),
                    MachineStatus = table.Column<int>(type: "INTEGER", nullable: false),
                    MachineType = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Machines", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Messages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    MachineId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Content = table.Column<string>(type: "TEXT", nullable: false),
                    Time = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Messages", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Machines",
                columns: new[] { "Id", "Endpoints", "MachineStatus", "MachineType", "Name", "StrConnectType" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-111111111111"), "HostName:127.0.0.1;Port:12345", 0, 0, "Machine A", "Socket" },
                    { new Guid("22222222-2222-2222-2222-222222222222"), "HostName:localhost;UserName:guest;Password:guest;QueueName:hello", 0, 0, "Machine B", "RabbitMQ" },
                    { new Guid("33333333-3333-3333-3333-333333333333"), "HostName:COM4", 0, 1, "Machine Port", "SerialPort" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Machines");

            migrationBuilder.DropTable(
                name: "Messages");
        }
    }
}
