using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NurSite.Domain.Entities;

namespace NurSite.Infrastructure.Persistence.Configurations;

public class SiteSettingConfiguration : IEntityTypeConfiguration<SiteSetting>
{
    public void Configure(EntityTypeBuilder<SiteSetting> b)
    {
        b.Property(x => x.SiteName).HasMaxLength(150).IsRequired();
        b.Property(x => x.CanonicalBaseUrl).HasMaxLength(250).IsRequired();
        b.Property(x => x.DefaultMetaTitle).HasMaxLength(70);
        b.Property(x => x.DefaultMetaDescription).HasMaxLength(170);
        b.Property(x => x.ContactPhone).HasMaxLength(30);
        b.Property(x => x.ContactEmail).HasMaxLength(200);

        b.Property(x => x.Tagline).HasMaxLength(300);
        b.Property(x => x.LogoPath).HasMaxLength(400);
        b.Property(x => x.FaviconPath).HasMaxLength(400);
        b.Property(x => x.DefaultOgImagePath).HasMaxLength(400);
        b.Property(x => x.ContactAddress).HasMaxLength(500);
        b.Property(x => x.WorkingHours).HasMaxLength(200);
        b.Property(x => x.TelegramUrl).HasMaxLength(300);
        b.Property(x => x.InstagramUrl).HasMaxLength(300);
        b.Property(x => x.AparatUrl).HasMaxLength(300);

        b.HasOne(x => x.DefaultCity).WithMany()
         .HasForeignKey(x => x.DefaultCityId).OnDelete(DeleteBehavior.SetNull);

        // این جدول باید همیشه دقیقاً یک رکورد داشته باشد
        b.ToTable(t => t.HasCheckConstraint("CK_SiteSetting_SingleRow", "[Id] = 1"));
    }
}
