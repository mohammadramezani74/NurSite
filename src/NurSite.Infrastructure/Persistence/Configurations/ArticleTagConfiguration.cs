using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NurSite.Domain.Entities;

public class ArticleTagConfiguration : IEntityTypeConfiguration<ArticleTag>
{
    public void Configure(EntityTypeBuilder<ArticleTag> b)
    {
        b.HasKey(x => new { x.ArticleId, x.TagId });
        b.HasOne(x => x.Article).WithMany(a => a.ArticleTags).HasForeignKey(x => x.ArticleId);
        b.HasOne(x => x.Tag).WithMany(t => t.ArticleTags).HasForeignKey(x => x.TagId);
    }
}
