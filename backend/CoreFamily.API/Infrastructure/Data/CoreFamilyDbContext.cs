using CoreFamily.API.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CoreFamily.API.Infrastructure.Data;

public class CoreFamilyDbContext : DbContext
{
    public CoreFamilyDbContext(DbContextOptions<CoreFamilyDbContext> options) : base(options) { }

    // ── Users ────────────────────────────────────────────────────
    public DbSet<User> Users => Set<User>();
    public DbSet<UserRole_> UserRoles => Set<UserRole_>();
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    // ── Content ──────────────────────────────────────────────────
    public DbSet<Content> Contents => Set<Content>();
    public DbSet<VideoContent> VideoContents => Set<VideoContent>();
    public DbSet<Program_> Programs => Set<Program_>();
    public DbSet<Lesson> Lessons => Set<Lesson>();
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();
    public DbSet<ProgressEntry> ProgressEntries => Set<ProgressEntry>();

    // ── Counselors & Sessions ─────────────────────────────────────
    public DbSet<CounselorProfile> CounselorProfiles => Set<CounselorProfile>();
    public DbSet<Session> Sessions => Set<Session>();
    public DbSet<Review> Reviews => Set<Review>();

    // ── Payments ──────────────────────────────────────────────────
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<Certificate> Certificates => Set<Certificate>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── Global soft-delete filter ─────────────────────────────
        modelBuilder.Entity<User>().HasQueryFilter(u => !u.IsDeleted);
        modelBuilder.Entity<Content>().HasQueryFilter(c => !c.IsDeleted);
        modelBuilder.Entity<Program_>().HasQueryFilter(p => !p.IsDeleted);
        modelBuilder.Entity<CounselorProfile>().HasQueryFilter(cp => !cp.IsDeleted);

        // ── Users ─────────────────────────────────────────────────
        modelBuilder.Entity<User>(e =>
        {
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.Email).HasMaxLength(256).IsRequired();
            e.Property(u => u.PasswordHash).IsRequired();
        });

        modelBuilder.Entity<UserRole_>(e =>
        {
            e.HasOne(r => r.User).WithMany(u => u.Roles)
             .HasForeignKey(r => r.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(r => new { r.UserId, r.Role }).IsUnique();
        });

        modelBuilder.Entity<UserProfile>(e =>
        {
            e.HasOne(p => p.User).WithOne(u => u.Profile)
             .HasForeignKey<UserProfile>(p => p.UserId).OnDelete(DeleteBehavior.Cascade);
            e.Property(p => p.FirstName).HasMaxLength(100).IsRequired();
            e.Property(p => p.LastName).HasMaxLength(100).IsRequired();
            e.Property(p => p.PreferredLanguage).HasMaxLength(10).HasDefaultValue("en");
        });

        // ── Content ───────────────────────────────────────────────
        modelBuilder.Entity<Content>(e =>
        {
            e.HasIndex(c => c.Slug).IsUnique();
            e.Property(c => c.Title).HasMaxLength(300).IsRequired();
            e.Property(c => c.Slug).HasMaxLength(350).IsRequired();
            e.Property(c => c.Price).HasPrecision(10, 2);
        });

        modelBuilder.Entity<VideoContent>(e =>
        {
            e.HasOne(v => v.Content).WithOne(c => c.Video)
             .HasForeignKey<VideoContent>(v => v.ContentId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Program_>(e =>
        {
            e.Property(p => p.Title).HasMaxLength(300).IsRequired();
            e.Property(p => p.Price).HasPrecision(10, 2);
        });

        modelBuilder.Entity<Enrollment>(e =>
        {
            e.HasIndex(en => new { en.ProgramId, en.UserId }).IsUnique();
            e.HasOne(en => en.Program).WithMany(p => p.Enrollments)
             .HasForeignKey(en => en.ProgramId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(en => en.User).WithMany()
             .HasForeignKey(en => en.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ProgressEntry>(e =>
        {
            e.HasIndex(pe => new { pe.UserId, pe.ContentId }).IsUnique();
        });

        // ── Counselors & Sessions ─────────────────────────────────
        modelBuilder.Entity<CounselorProfile>(e =>
        {
            e.HasOne(cp => cp.User).WithOne()
             .HasForeignKey<CounselorProfile>(cp => cp.UserId).OnDelete(DeleteBehavior.Cascade);
            e.Property(cp => cp.HourlyRateUsd).HasPrecision(8, 2);
        });

        modelBuilder.Entity<Session>(e =>
        {
            e.Property(s => s.AmountPaid).HasPrecision(10, 2);
            e.Property(s => s.PlatformCommission).HasPrecision(10, 2);
            e.HasOne(s => s.Counselor).WithMany(cp => cp.Sessions)
             .HasForeignKey(s => s.CounselorId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(s => s.Client).WithMany()
             .HasForeignKey(s => s.ClientId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Review>(e =>
        {
            e.Property(r => r.Rating).IsRequired();
            e.HasOne(r => r.Reviewer).WithMany()
             .HasForeignKey(r => r.ReviewerId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(r => r.Counselor).WithMany(cp => cp.Reviews)
             .HasForeignKey(r => r.CounselorId).OnDelete(DeleteBehavior.SetNull)
             .IsRequired(false);
            e.HasOne(r => r.Session).WithOne(s => s.Review)
             .HasForeignKey<Review>(r => r.SessionId).OnDelete(DeleteBehavior.SetNull)
             .IsRequired(false);
        });

        // ── Payments ──────────────────────────────────────────────
        modelBuilder.Entity<Transaction>(e =>
        {
            e.Property(t => t.Amount).HasPrecision(10, 2);
            e.Property(t => t.Currency).HasMaxLength(3).HasDefaultValue("USD");
            e.HasOne(t => t.User).WithMany()
             .HasForeignKey(t => t.UserId).OnDelete(DeleteBehavior.Restrict);
        });

        // ── Refresh Tokens ────────────────────────────────────────
        modelBuilder.Entity<RefreshToken>(e =>
        {
            e.HasIndex(rt => rt.Token).IsUnique();
            e.HasOne(rt => rt.User).WithMany()
             .HasForeignKey(rt => rt.UserId).OnDelete(DeleteBehavior.Cascade);
        });
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Auto-update UpdatedAt on every save
        foreach (var entry in ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Modified))
        {
            if (entry.Entity is Domain.Entities.BaseEntity entity)
                entity.UpdatedAt = DateTime.UtcNow;
        }
        return base.SaveChangesAsync(cancellationToken);
    }
}
