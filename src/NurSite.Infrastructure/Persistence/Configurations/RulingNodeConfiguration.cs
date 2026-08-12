using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NurSite.Domain.Entities;

namespace NurSite.Infrastructure.Persistence.Configurations;

public class RulingNodeConfiguration : IEntityTypeConfiguration<RulingNode>
{
    public void Configure(EntityTypeBuilder<RulingNode> b)
    {
        b.Property(x => x.Text).HasMaxLength(600).IsRequired();
        b.Property(x => x.Note).HasMaxLength(600);

        b.HasIndex(x => new { x.RulingId, x.ParentId, x.SortOrder });

        b.HasOne(x => x.Ruling)
         .WithMany(r => r.Nodes)
         .HasForeignKey(x => x.RulingId)
         .OnDelete(DeleteBehavior.Cascade);

        // حذف آبشاری روی رابطه خودارجاع در SQL Server مجاز نیست،
        // چون موتور نمی‌تواند چرخه را تشخیص دهد. حذف فرزندان در کد انجام می‌شود.
        b.HasOne(x => x.Parent)
         .WithMany(x => x.Children)
         .HasForeignKey(x => x.ParentId)
         .OnDelete(DeleteBehavior.Restrict);
    }
}
