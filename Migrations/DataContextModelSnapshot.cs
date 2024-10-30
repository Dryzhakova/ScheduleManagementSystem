﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using WebAppsMoodle.Models;

#nullable disable

namespace WebAppsMoodle.Migrations
{
    [DbContext(typeof(DataContext))]
    partial class DataContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "8.0.10");

            modelBuilder.Entity("WebAppsMoodle.Models.Classes", b =>
                {
                    b.Property<string>("ClassesId")
                        .HasColumnType("TEXT");

                    b.Property<string>("ClassesDescriptionId")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<bool>("IsCanceled")
                        .HasColumnType("INTEGER");

                    b.Property<string>("RoomId")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("TeacherId")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("ClassesId");

                    b.HasIndex("ClassesDescriptionId");

                    b.HasIndex("RoomId");

                    b.HasIndex("TeacherId");

                    b.ToTable("Classes");
                });

            modelBuilder.Entity("WebAppsMoodle.Models.ClassesDescription", b =>
                {
                    b.Property<string>("ClassesDescriptionId")
                        .HasColumnType("TEXT");

                    b.Property<string>("Description")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("ClassesDescriptionId");

                    b.ToTable("ClassesDescription");
                });

            modelBuilder.Entity("WebAppsMoodle.Models.OneTimeClassDate", b =>
                {
                    b.Property<string>("OneTimeClassDateId")
                        .HasColumnType("TEXT");

                    b.Property<string>("ClassesId")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<TimeSpan>("OneTimeClassEndTime")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("OneTimeClassFullDate")
                        .HasColumnType("TEXT");

                    b.Property<TimeSpan>("OneTimeClassStartTime")
                        .HasColumnType("TEXT");

                    b.HasKey("OneTimeClassDateId");

                    b.ToTable("OneTimeClasses");
                });

            modelBuilder.Entity("WebAppsMoodle.Models.RecurringClassDate", b =>
                {
                    b.Property<string>("RecurringClassDateId")
                        .HasColumnType("TEXT");

                    b.Property<string>("ClassesId")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<bool>("IsEven")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("IsEveryWeek")
                        .HasColumnType("INTEGER");

                    b.Property<int>("RecurrenceDay")
                        .HasColumnType("INTEGER");

                    b.Property<TimeSpan>("RecurrenceEndTime")
                        .HasColumnType("TEXT");

                    b.Property<TimeSpan>("RecurrenceStartTime")
                        .HasColumnType("TEXT");

                    b.HasKey("RecurringClassDateId");

                    b.ToTable("RecurringClasses");
                });

            modelBuilder.Entity("WebAppsMoodle.Models.Room", b =>
                {
                    b.Property<string>("RoomId")
                        .HasColumnType("TEXT");

                    b.Property<string>("RoomNumber")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("RoomId");

                    b.ToTable("Rooms");
                });

            modelBuilder.Entity("WebAppsMoodle.Models.Teacher", b =>
                {
                    b.Property<string>("TeacherId")
                        .HasColumnType("TEXT");

                    b.Property<string>("Password")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("Username")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("TeacherId");

                    b.ToTable("Teachers");
                });

            modelBuilder.Entity("WebAppsMoodle.Models.Classes", b =>
                {
                    b.HasOne("WebAppsMoodle.Models.ClassesDescription", "ClassesDescription")
                        .WithMany("Classes")
                        .HasForeignKey("ClassesDescriptionId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("WebAppsMoodle.Models.Room", "Room")
                        .WithMany("Classes")
                        .HasForeignKey("RoomId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("WebAppsMoodle.Models.Teacher", "Teacher")
                        .WithMany("Classes")
                        .HasForeignKey("TeacherId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("ClassesDescription");

                    b.Navigation("Room");

                    b.Navigation("Teacher");
                });

            modelBuilder.Entity("WebAppsMoodle.Models.ClassesDescription", b =>
                {
                    b.Navigation("Classes");
                });

            modelBuilder.Entity("WebAppsMoodle.Models.Room", b =>
                {
                    b.Navigation("Classes");
                });

            modelBuilder.Entity("WebAppsMoodle.Models.Teacher", b =>
                {
                    b.Navigation("Classes");
                });
#pragma warning restore 612, 618
        }
    }
}
