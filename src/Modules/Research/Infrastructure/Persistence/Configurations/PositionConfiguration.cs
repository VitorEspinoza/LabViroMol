using System.Text.Json;
using LabViroMol.Modules.Shared.Infrastructure.Persistence.Extensions;

namespace LabViroMol.Modules.Research.Infrastructure.Persistence.Configurations;

using LabViroMol.Modules.Research.Domain.Positions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class PositionConfiguration : IEntityTypeConfiguration<Position>
{
    public void Configure(EntityTypeBuilder<Position> builder)
    {
        builder.ToTable("Positions");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();

        builder.Property(p => p.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(p => p.Description)
            .HasMaxLength(2000);

        builder.ConfigureTranslations<
            Position,
            PositionTranslation>();
    }
}
