using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NurSite.Domain.Entities;

namespace NurSite.Infrastructure.Persistence.Configurations;

public class RulingVerdictMarjaConfiguration : IEntityTypeConfiguration<RulingVerdictMarja>
{
    public void Configure(EntityTypeBuilder<RulingVerdictMarja> b)
    {
        b.HasKey(x => new { x.RulingVerdictId, x.MarjaId });

        b.HasOne(x => x.RulingVerdict)
         .WithMany(v => v.Marjas)
         .HasForeignKey(x => x.RulingVerdictId)
         .OnDelete(DeleteBehavior.Cascade);

        b.HasOne(x => x.Marja)
         .WithMany()
         .HasForeignKey(x => x.MarjaId)
         .OnDelete(DeleteBehavior.Cascade);
    }
}