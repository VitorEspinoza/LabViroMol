using System;
using LabViroMol.Modules.Scheduling.Domain.Schedules;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LabViroMol.Modules.Scheduling.Infrastructure.Persistence.Configurations;

public class ScheduleConfiguration : IEntityTypeConfiguration<Schedule>
{
    public void Configure(EntityTypeBuilder<Schedule> builder)
    {
        builder.ToTable("Schedules");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id)
            .ValueGeneratedNever();

        builder.OwnsOne(s => s.Scheduler, p =>
        {
            p.Property(x => x.Name)
                .HasColumnName("SchedulerName");

            p.Property(x => x.Course)
                .HasColumnName("SchedulerCourse");

            p.Property(x => x.Email)
                .HasColumnName("SchedulerEmail");
        });

        builder.OwnsOne(s => s.Scheduling, p =>
        {
            p.Property(x => x.Date)
                .HasColumnName("SchedulingDate");
            
            p.Property(x => x.StartDateHour)
                .HasColumnName("SchedulingStartHour");
            
            p.Property(x => x.EndDateHour)
                .HasColumnName("SchedulingEndHour");
        });
            
        builder.Property(s => s.AcceptTerm)
            .HasColumnName("AcceptTerm");
        
        builder.Property(s => s.AdvisorProfessor)
            .HasColumnName("AdvisorProfessor");
        
        builder.Property(s => s.ProjectTitle)
            .HasColumnName("ProjectTitle");
        
        builder.Property(s => s.Description)
            .HasColumnName("Description");

        builder.Property(s => s.Status)
            .HasConversion<string>();
        
        builder.Property(s => s.ApprovedBy)
            .HasColumnName("ApprovedBy");
        
        builder.Property(s => s.RefusedBy)
            .HasColumnName("RefusedBy");
        
        builder.Property(s => s.TermUrl)
            .HasColumnName("TermUrl")
            .IsRequired(false);
        
        builder.OwnsMany(s => s.Equipments, p =>
        {
            p.ToTable("ScheduleEquipments");

            p.WithOwner().HasForeignKey("ScheduleId");

            p.Property<Guid>("Id");
            p.HasKey("Id");

            p.Property(e => e.EquipmentId)
                .HasColumnName("EquipmentId")
                .IsRequired();
            
            p.Property(e => e.Name)
                .HasColumnName("EquipmentName")
                .IsRequired();
        });

    }
}