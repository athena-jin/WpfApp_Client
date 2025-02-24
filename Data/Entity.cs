using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Data;
public enum MachineStatus
{
	Init = 0,
	Running = 1,
	Stopped = 2,
}
public enum MachineType
{
	Sender = 0,
	Receiver = 1,
}
public class Machine
{
	[Key]
	public Guid Id { get; set; }
	public string Name { get; set; }
	public string StrConnectType { get; set; }
	public string Endpoints { get; set; }
	//public uint Status { get; set; }
	//public uint Type { get; set; }
	public MachineStatus MachineStatus { get; set; }
	public MachineType MachineType { get; set; }
}
public class Message
{
	public Guid Id { get; set; }
	public Guid MachineId { get; set; }
	public string Content { get; set; }
	public DateTime Time { get; set; }
}
public class HJ_DbContextFactory : IDesignTimeDbContextFactory<HJ_DbContext>
{
	public HJ_DbContext CreateDbContext(string[] args)
	{
		var optionsBuilder = new DbContextOptionsBuilder<HJ_DbContext>();
		optionsBuilder.UseSqlite("Data Source=mydatabase.db");

		return new HJ_DbContext(optionsBuilder.Options);
	}
}
//dotnet ef database update --project Data --startup-project WpfApp_Client
//dotnet ef migrations add InitialCreate --project Data --startup-project WpfApp_Client
public class HJ_DbContext : DbContext
{
	public HJ_DbContext(DbContextOptions<HJ_DbContext> options)
		: base(options)
	{
	}
	public DbSet<Machine> Machines { get; set; } = null!;
	public DbSet<Message> Messages { get; set; } = null!;

	protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
	{
		optionsBuilder.UseSqlite("Data Source=mydatabase.db");
	}

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		base.OnModelCreating(modelBuilder);

		modelBuilder.Entity<Machine>()
		.Property(m => m.MachineStatus)
		.HasConversion<int>();

		modelBuilder.Entity<Machine>()
			.Property(m => m.MachineType)
			.HasConversion<int>();

		// Seed data for Machines
		modelBuilder.Entity<Machine>().HasData(
			new Machine
			{
				Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
				Name = "Machine A",
				Endpoints = "HostName:127.0.0.1;Port:12345",
				StrConnectType = "Socket",
				MachineStatus = MachineStatus.Init,
				MachineType = MachineType.Sender
			},
		new Machine
		{
			Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
			Name = "Machine B",
			Endpoints = "HostName:localhost;UserName:guest;Password:guest;QueueName:hello",
			StrConnectType = "RabbitMQ",
			MachineStatus = MachineStatus.Init,
			MachineType = MachineType.Sender
		},
		new Machine
		{
			Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
			Name = "Machine Port",
			Endpoints = "HostName:COM4",
			StrConnectType = "SerialPort",
			MachineStatus = MachineStatus.Init,
			MachineType = MachineType.Receiver
		}
	   );
	}
	public void EnsureMigrations()
	{
		var pendingMigrations = this.Database.GetPendingMigrations();
		if (pendingMigrations.Any())
		{
			this.Database.Migrate();
		}
	}
}
