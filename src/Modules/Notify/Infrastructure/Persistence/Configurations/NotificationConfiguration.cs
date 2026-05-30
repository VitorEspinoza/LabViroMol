using LabViroMol.Modules.Notify.Domain.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LabViroMol.Modules.Notify.Infrastructure.Persistence.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {   
        builder.ToTable("Notifications");
        
        builder.HasKey(n => n.Id);
        builder.Property(n => n.Id)
            .ValueGeneratedNever();

        builder.Property(n => n.Title)
            .IsRequired();
        
        builder.Property(n => n.Message)
            .IsRequired();
        
        builder.Property(n => n.ReferenceId)
            .IsRequired(false);
        
        builder.Property(n => n.ReferenceModule)
            .IsRequired(false);
        
        builder.Property(n => n.Type)
            .IsRequired();
        
        builder.Property(n => n.TargetPermissionId)
            .IsRequired();
        
        builder.Property(n => n.ExpiresOn)
            .IsRequired();

        builder.OwnsMany(n => n.NotificationDismissals, p =>
        {
            p.ToTable("NotificationDismissals");
            
            p.WithOwner().HasForeignKey("NotificationId");
            
            p.Property(n => n.UserId)
                .HasColumnName("UserId");
            p.Property(n => n.DismissedOn)
                .HasColumnName("DismissedOn");
        });
    }
}