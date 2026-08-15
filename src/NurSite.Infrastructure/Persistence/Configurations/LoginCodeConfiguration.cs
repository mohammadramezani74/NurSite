using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NurSite.Domain.Entities;

namespace NurSite.Infrastructure.Persistence.Configurations;

public class LoginCodeConfiguration : IEntityTypeConfiguration<LoginCode>
{
    public void Configure(EntityTypeBuilder<LoginCode> b)
    {
        b.Property(x => x.Mobile).HasMaxLength(15).IsRequired();
        b.Property(x => x.CodeHash).HasMaxLength(100).IsRequired();
        b.Property(x => x.IpHash).HasMaxLength(64);

        // هر بار ورود، آخرین کد همان شماره خوانده می‌شود
        b.HasIndex(x => new { x.Mobile, x.CreatedAtUtc });

        // برای پاک‌سازی دوره‌ای کدهای منقضی
        b.HasIndex(x => x.ExpiresAtUtc);
    }
}