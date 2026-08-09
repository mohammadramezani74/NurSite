using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NurSite.Domain.Entities;

public class UserQuestionConfiguration : IEntityTypeConfiguration<UserQuestion>
{
    public void Configure(EntityTypeBuilder<UserQuestion> b)
    {
        b.Property(x => x.Body).HasMaxLength(4000).IsRequired();
        b.Property(x => x.SenderName).HasMaxLength(150);
        b.Property(x => x.SenderMobile).HasMaxLength(15);
        b.Property(x => x.SenderEmail).HasMaxLength(200);
        b.Property(x => x.AssignedToUserId).HasMaxLength(450);
        b.HasIndex(x => x.Status);
        // صف پرسش‌های ارجاع‌شده به هر پاسخگو
        b.HasIndex(x => new { x.AssignedToUserId, x.Status });

        b.HasOne(x => x.PublishedRuling)
         .WithMany()
         .HasForeignKey(x => x.PublishedRulingId)
         .OnDelete(DeleteBehavior.SetNull);
    }
}