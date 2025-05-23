﻿// <auto-generated />
using System;
using Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace Data.Migrations
{
    [DbContext(typeof(HJ_DbContext))]
    [Migration("20250220051652_InitialCreate")]
    partial class InitialCreate
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "9.0.2");

            modelBuilder.Entity("Data.Machine", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<string>("Endpoints")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<int>("MachineStatus")
                        .HasColumnType("INTEGER");

                    b.Property<int>("MachineType")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("StrConnectType")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("Machines");

                    b.HasData(
                        new
                        {
                            Id = new Guid("11111111-1111-1111-1111-111111111111"),
                            Endpoints = "HostName:127.0.0.1;Port:12345",
                            MachineStatus = 0,
                            MachineType = 0,
                            Name = "Machine A",
                            StrConnectType = "Socket"
                        },
                        new
                        {
                            Id = new Guid("22222222-2222-2222-2222-222222222222"),
                            Endpoints = "HostName:localhost;UserName:guest;Password:guest;QueueName:hello",
                            MachineStatus = 0,
                            MachineType = 0,
                            Name = "Machine B",
                            StrConnectType = "RabbitMQ"
                        },
                        new
                        {
                            Id = new Guid("33333333-3333-3333-3333-333333333333"),
                            Endpoints = "HostName:COM4",
                            MachineStatus = 0,
                            MachineType = 1,
                            Name = "Machine Port",
                            StrConnectType = "SerialPort"
                        });
                });

            modelBuilder.Entity("Data.Message", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<string>("Content")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<Guid>("MachineId")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("Time")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("Messages");
                });
#pragma warning restore 612, 618
        }
    }
}
