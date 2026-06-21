using LabViroMol.Modules.Assets.Domain.Equipments;
using LabViroMol.Modules.Assets.Domain.MaintenanceRequests;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LabViroMol.Modules.Assets.Infrastructure.Persistence.Configurations;

public class MaintenanceRequestConfiguration : IEntityTypeConfiguration<MaintenanceRequest>
{
    public void Configure(EntityTypeBuilder<MaintenanceRequest> builder)
    {
        builder.ToTable("MaintenanceRequests");

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id)
            .ValueGeneratedNever();

        builder.Property(m => m.EquipmentId).IsRequired();

        builder.HasOne<Equipment>()
            .WithMany()
            .HasForeignKey(m => m.EquipmentId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Property(m => m.Description).IsRequired();
        builder.Property(m => m.ProblemDescription).IsRequired();
        builder.Property(m => m.Status)
            .IsRequired().HasConversion<string>();
    }
}