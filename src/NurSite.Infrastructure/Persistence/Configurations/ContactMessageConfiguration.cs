using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NurSite.Domain.Entities;

namespace NurSite.Infrastructure.Persistence.Configurations;

public class ContactMessageConfiguration : IEntityTypeConfiguration<ContactMessage>
{
    public void Configure(EntityTypeBuilder<ContactMessage> b)
    {
        b.Property(x => x.SenderName).HasMaxLength(150).IsRequired();
        b.Property(x => x.SenderMobile).HasMaxLength(15);
        b.Property(x => x.SenderEmail).HasMaxLength(200);
        b.Property(x => x.Subject).HasMaxLength(250);
        b.Property(x => x.Body).HasMaxLength(4000).IsRequired();
        b.Property(x => x.SenderIpHash).HasMaxLength(64);
        b.Property(x => x.AdminNote).HasMaxLength(2000);
        b.HasIndex(x => x.IsRead);

    }
}
