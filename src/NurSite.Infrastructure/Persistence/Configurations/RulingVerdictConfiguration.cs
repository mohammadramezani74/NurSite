using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NurSite.Domain.Entities;

namespace NurSite.Infrastructure.Persistence.Configurations;

public class RulingVerdictConfiguration : IEntityTypeConfiguration<RulingVerdict>
{
    public void Configure(EntityTypeBuilder<RulingVerdict> b)
    {
        b.Property(x => x.Text).HasMaxLength(400).IsRequired();
        b.Property(x => x.SourceNote).HasMaxLength(400);

        b.HasIndex(x => new { x.RulingNodeId, x.SortOrder });

        b.HasOne(x => x.RulingNode)
         .WithMany(n => n.Verdicts)
         .HasForeignKey(x => x.RulingNodeId)
         .OnDelete(DeleteBehavior.Cascade);
    }
}
