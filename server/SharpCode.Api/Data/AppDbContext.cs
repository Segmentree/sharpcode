using Microsoft.EntityFrameworkCore;

namespace SharpCode.Api.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Problem> Problems => Set<Problem>();
    public DbSet<TestCase> TestCases => Set<TestCase>();
    public DbSet<Submission> Submissions => Set<Submission>();
    public DbSet<ProblemTranslation> ProblemTranslations => Set<ProblemTranslation>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Problem>().HasIndex(p => p.Slug).IsUnique();
        b.Entity<Problem>().HasMany(p => p.TestCases).WithOne(t => t.Problem).HasForeignKey(t => t.ProblemId)
            .OnDelete(DeleteBehavior.Cascade);
        b.Entity<Submission>().HasOne(s => s.Problem).WithMany().HasForeignKey(s => s.ProblemId)
            .OnDelete(DeleteBehavior.Cascade);
        b.Entity<Problem>().HasMany(p => p.Translations).WithOne(t => t.Problem).HasForeignKey(t => t.ProblemId)
            .OnDelete(DeleteBehavior.Cascade);
        b.Entity<ProblemTranslation>().HasIndex(t => new { t.ProblemId, t.Locale }).IsUnique();
        b.Entity<Problem>().Property(p => p.Difficulty).HasConversion<string>();
        b.Entity<Problem>().Property(p => p.Category).HasConversion<string>();
        b.Entity<Submission>().Property(s => s.Verdict).HasConversion<string>();
    }
}
