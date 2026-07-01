using System.Text.Json;
using LabViroMol.Modules.Research.Domain.Positions;
using LabViroMol.Modules.Shared.Infrastructure.Persistence.Extensions;

namespace LabViroMol.Modules.Research.Infrastructure.Persistence.Configurations;

using LabViroMol.Modules.Research.Domain.Publications;
using LabViroMol.Modules.Research.Domain.Researchers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class PublicationConfiguration : IEntityTypeConfiguration<Publication>
{
    public void Configure(EntityTypeBuilder<Publication> builder)
    {
        builder.ToTable("Publications");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();

        builder.Property(p => p.Title)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(p => p.Description)
            .HasMaxLength(5000);

        builder.Property(p => p.Doi)
            .HasMaxLength(200);

        builder.Property(p => p.PublishedOn)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(p => p.PublishUrl)
            .HasMaxLength(2000);

        builder.Property(p => p.PublicationDate)
            .HasColumnType("date");

        builder.OwnsMany(p => p.Researchers, r =>
        {
            r.ToTable("PublicationResearchers");

            r.HasKey("PublicationId", "ResearcherId");
            r.WithOwner().HasForeignKey("PublicationId");

            r.Property(r => r.ResearcherId)
                .IsRequired();

            r.HasOne<Researcher>()
                .WithMany()
                .HasForeignKey(r => r.ResearcherId)
                .OnDelete(DeleteBehavior.Restrict);

            r.Property(r => r.Order)
                .IsRequired();
        });

        builder.Metadata.FindNavigation(nameof(Publication.Researchers))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.ConfigureTranslations<
            Publication,
            PublicationTranslation>();
    }
}
