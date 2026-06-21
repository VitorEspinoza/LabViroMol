using LabViroMol.Modules.Research.Domain.Projects;
using LabViroMol.Modules.Research.Domain.Researchers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LabViroMol.Modules.Research.Infrastructure.Persistence.Configurations;

public class ProjectMemberConfiguration : IEntityTypeConfiguration<ProjectMember>
{
    public void Configure(EntityTypeBuilder<ProjectMember> builder)
    {
        builder.ToTable("ProjectMembers");

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).ValueGeneratedNever();

        builder.Property(m => m.ResearcherId).IsRequired();

        builder.HasOne<Researcher>()
            .WithMany()
            .HasForeignKey(m => m.ResearcherId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(m => m.Role)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(m => m.JoinedAt).IsRequired();
        builder.Property(m => m.LeftAt).IsRequired(false);
    }
}
