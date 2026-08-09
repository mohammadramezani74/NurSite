using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NurSite.Domain.Entities;

namespace NurSite.Infrastructure.Persistence.Configurations;

public class UrlRedirectConfiguration : IEntityTypeConfiguration<UrlRedirect>
{
    public void Configure(EntityTypeBuilder<UrlRedirect> b)
    {
        b.Property(x => x.FromPath).HasMaxLength(400).IsRequired();
        b.Property(x => x.ToPath).HasMaxLength(400).IsRequired();
        b.HasIndex(x => x.FromPath).IsUnique();
    }
}