﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Sierra.Model;

namespace Sierra.Model.Migrations
{
    [DbContext(typeof(SierraDbContext))]
    partial class SierraDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "2.1.1-rtm-30846")
                .HasAnnotation("Relational:MaxIdentifierLength", 128)
                .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

            modelBuilder.Entity("Sierra.Model.Fork", b =>
                {
                    b.Property<string>("SourceRepositoryName");

                    b.Property<string>("TenantCode")
                        .HasMaxLength(6);

                    b.Property<Guid>("ForkVstsId");

                    b.Property<string>("State")
                        .IsRequired()
                        .HasMaxLength(20);

                    b.HasKey("SourceRepositoryName", "TenantCode");

                    b.HasIndex("TenantCode");

                    b.ToTable("Forks");
                });

            modelBuilder.Entity("Sierra.Model.Tenant", b =>
                {
                    b.Property<string>("Code")
                        .ValueGeneratedOnAdd()
                        .HasMaxLength(6);

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(100);

                    b.HasKey("Code");

                    b.ToTable("Tenants");
                });

            modelBuilder.Entity("Sierra.Model.Fork", b =>
                {
                    b.HasOne("Sierra.Model.Tenant")
                        .WithMany("CustomSourceRepos")
                        .HasForeignKey("TenantCode")
                        .OnDelete(DeleteBehavior.Cascade);
                });
#pragma warning restore 612, 618
        }
    }
}
