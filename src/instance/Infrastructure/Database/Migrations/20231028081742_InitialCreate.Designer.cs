﻿// <auto-generated />
using System;
using DatabaseLib;
using Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace DatabaseLib.Migrations
{
    [DbContext(typeof(InstanceDbContext))]
    [Migration("20231028081742_InitialCreate")]
    partial class InitialCreate
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "8.0.0-rc.2.23480.1");

            modelBuilder.Entity("DatabaseLib.Models.EventLog", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("CreatedAtUtc")
                        .HasColumnType("TEXT");

                    b.Property<string>("DataJson")
                        .HasColumnType("TEXT");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("TriggeredAtUtc")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("EvenLogs");
                });

            modelBuilder.Entity("DatabaseLib.Models.ServerLog", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("CreatedAtUtc")
                        .HasColumnType("TEXT");

                    b.Property<string>("Message")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<Guid>("ServerStartId")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("ServerStartId");

                    b.ToTable("ServerLogs");
                });

            modelBuilder.Entity("DatabaseLib.Models.ServerStart", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("CreatedAtUtc")
                        .HasColumnType("TEXT");

                    b.Property<string>("StartParameters")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("StartedAtUtc")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("ServerStarts");
                });

            modelBuilder.Entity("DatabaseLib.Models.UpdateOrInstallLog", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("CreatedAtUtc")
                        .HasColumnType("TEXT");

                    b.Property<string>("Message")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<Guid>("UpdateOrInstallStartId")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("UpdateOrInstallStartId");

                    b.ToTable("UpdateOrInstallLogs");
                });

            modelBuilder.Entity("DatabaseLib.Models.UpdateOrInstallStart", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("CreatedAtUtc")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("StartedAtUtc")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("UpdateOrInstallStarts");
                });

            modelBuilder.Entity("DatabaseLib.Models.ServerLog", b =>
                {
                    b.HasOne("DatabaseLib.Models.ServerStart", "ServerStart")
                        .WithMany()
                        .HasForeignKey("ServerStartId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("ServerStart");
                });

            modelBuilder.Entity("DatabaseLib.Models.UpdateOrInstallLog", b =>
                {
                    b.HasOne("DatabaseLib.Models.UpdateOrInstallStart", "UpdateOrInstallStart")
                        .WithMany()
                        .HasForeignKey("UpdateOrInstallStartId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("UpdateOrInstallStart");
                });
#pragma warning restore 612, 618
        }
    }
}
