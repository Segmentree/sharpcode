using Microsoft.EntityFrameworkCore;

namespace SharpCode.Api.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Problem> Problems => Set<Problem>();
    public DbSet<TestCase> TestCases => Set<TestCase>();
    public DbSet<Submission> Submissions => Set<Submission>();
    public DbSet<ProblemTranslation> ProblemTranslations => Set<ProblemTranslation>();
    public DbSet<User> Users => Set<User>();
    public DbSet<ProblemDraft> ProblemDrafts => Set<ProblemDraft>();
    public DbSet<SandboxScript> SandboxScripts => Set<SandboxScript>();
    public DbSet<SandboxDraft> SandboxDrafts => Set<SandboxDraft>();

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
        b.Entity<Submission>().HasOne(s => s.User).WithMany().HasForeignKey(s => s.UserId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<Submission>().HasIndex(s => new { s.UserId, s.ProblemId });

        b.Entity<User>().HasIndex(u => u.GoogleSubject).IsUnique();
        b.Entity<ProblemDraft>().HasIndex(d => new { d.UserId, d.ProblemId }).IsUnique();
        b.Entity<ProblemDraft>().HasOne(d => d.User).WithMany().HasForeignKey(d => d.UserId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<ProblemDraft>().HasOne(d => d.Problem).WithMany().HasForeignKey(d => d.ProblemId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<SandboxScript>().HasIndex(s => s.UserId);
        b.Entity<SandboxScript>().HasOne(s => s.User).WithMany().HasForeignKey(s => s.UserId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<SandboxDraft>().HasKey(d => d.UserId);
        b.Entity<SandboxDraft>().HasOne(d => d.User).WithOne().HasForeignKey<SandboxDraft>(d => d.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
