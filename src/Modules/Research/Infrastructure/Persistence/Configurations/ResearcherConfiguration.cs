namespace LabViroMol.Modules.Research.Infrastructure.Persistence.Configurations;

using LabViroMol.Modules.Research.Domain.Positions;
using LabViroMol.Modules.Research.Domain.Researchers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class ResearcherConfiguration : IEntityTypeConfiguration<Researcher>
{
    public void Configure(EntityTypeBuilder<Researcher> builder)
    {
        builder.ToTable("Researchers");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();

        builder.HasOne<Position>()
            .WithMany()
            .HasForeignKey(r => r.PositionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.OwnsOne(r => r.Name, n =>
        {
            n.Property(rn => rn.FirstName)
                .HasColumnName("FirstName")
                .HasMaxLength(100)
                .IsRequired();

            n.Property(rn => rn.LastName)
                .HasColumnName("LastName")
                .HasMaxLength(100)
                .IsRequired();

            n.Property(rn => rn.CitationName)
                .HasColumnName("CitationName")
                .HasMaxLength(50);
        });
        
        builder.Property(r => r.LattesUrl)
            .HasMaxLength(500);

        builder.OwnsOne(r => r.AcademicBackground, ab =>
        {
            ab.Property(a => a.DegreeLevel)
                .HasMaxLength(50)
                .HasColumnName("DegreeLevel")
                .IsRequired();

            ab.Property(a => a.FieldOfStudy)
                .HasMaxLength(300)
                .HasColumnName("FieldOfStudy");
        });
    }
}
