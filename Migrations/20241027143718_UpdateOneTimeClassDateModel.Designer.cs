﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using WebAppsMoodle.Models;

#nullable disable

namespace WebAppsMoodle.Migrations
{
    [DbContext(typeof(DataContext))]
    [Migration("20241027143718_UpdateOneTimeClassDateModel")]
    partial class UpdateOneTimeClassDateModel
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "8.0.10");

            modelBuilder.Entity("WebAppsMoodle.Models.Classes", b =>
                {
                    b.Property<string>("ClassesId")
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
#pragma warning restore 612, 618
        }
    }
}
