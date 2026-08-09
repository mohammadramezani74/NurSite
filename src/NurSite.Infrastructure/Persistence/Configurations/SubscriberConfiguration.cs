using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NurSite.Domain.Entities;

namespace NurSite.Infrastructure.Persistence.Configurations;

public class SubscriberConfiguration : IEntityTypeConfiguration<Subscriber>
{
    public void Configure(EntityTypeBuilder<Subscriber> b)
    {
        b.Property(x => x.Mobile).HasMaxLength(15).IsRequired();
        b.Property(x => x.DisplayName).HasMaxLength(150);
        b.Property(x => x.ConfirmationToken).HasMaxLength(128);
        b.HasIndex(x => x.Mobile).IsUnique();
        b.Ignore(x => x.IsActive);
    }
}
