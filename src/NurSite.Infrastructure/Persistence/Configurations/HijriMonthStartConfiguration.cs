using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NurSite.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NurSite.Infrastructure.Persistence.Configurations
{
    public class HijriMonthStartConfiguration : IEntityTypeConfiguration<HijriMonthStart>
    {
        public void Configure(EntityTypeBuilder<HijriMonthStart> b)
        {
            b.Property(x => x.Note).HasMaxLength(250);
            b.Property(x => x.CreatedById).HasMaxLength(450);

            // هر ماه از هر سال قمری فقط یک بار ثبت می‌شود
            b.HasIndex(x => new { x.HijriYear, x.HijriMonth }).IsUnique();

            b.ToTable(t =>
            {
                t.HasCheckConstraint("CK_HijriMonthStart_Month", "[HijriMonth] BETWEEN 1 AND 12");
                t.HasCheckConstraint("CK_HijriMonthStart_Year", "[HijriYear] BETWEEN 1300 AND 1600");
            });
        }
    }
}
