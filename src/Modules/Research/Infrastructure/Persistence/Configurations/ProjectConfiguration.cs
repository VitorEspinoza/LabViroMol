using System.Text.Json;
using LabViroMol.Modules.Shared.Infrastructure.Persistence.Extensions;

namespace LabViroMol.Modules.Research.Infrastructure.Persistence.Configurations;

using LabViroMol.Modules.Research.Domain.Partners;
using LabViroMol.Modules.Research.Domain.Projects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.ToTable("Projects");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();

        builder.HasOne<Partner>()
            .WithMany()
            .HasForeignKey(p => p.PartnerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(p => p.Title)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(p => p.Description)
            .HasMaxLength(2000);

        builder.HasMany(p => p.Members)
            .WithOne()
            .HasForeignKey("ProjectId")
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(p => p.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Metadata.FindNavigation(nameof(Project.Members))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.ConfigureTranslations<
            Project,
            ProjectTranslation>();
    }
}
