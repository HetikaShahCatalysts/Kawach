using Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Api.Data;

public sealed class KawachDbContext(DbContextOptions<KawachDbContext> options)
    : DbContext(options)
{
    public DbSet<Participant> Participants => Set<Participant>();
    public DbSet<AssessmentSession> AssessmentSessions => Set<AssessmentSession>();
    public DbSet<AssessmentStepTracking> AssessmentStepTracking => Set<AssessmentStepTracking>();
    public DbSet<AssessmentAnswer> AssessmentAnswers => Set<AssessmentAnswer>();
    public DbSet<AssessmentResult> AssessmentResults => Set<AssessmentResult>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Participant>(entity =>
        {
            entity.ToTable("Participant");
            entity.HasKey(item => item.UserId);
            entity.Property(item => item.UserId).ValueGeneratedOnAdd();
            entity.Property(item => item.FullName).HasMaxLength(150).IsRequired();
            entity.Property(item => item.Gender).HasMaxLength(30);
            entity.Property(item => item.Phone).HasMaxLength(20);
            entity.Property(item => item.Email).HasMaxLength(254);
            entity.Property(item => item.Location).HasMaxLength(200);
            entity.Property(item => item.CreatedOn)
                .HasDefaultValueSql("GETUTCDATE()");
        });

        modelBuilder.Entity<AssessmentSession>(entity =>
        {
            entity.ToTable("AssessmentSession");
            entity.HasKey(item => item.Id);
            entity.HasAlternateKey(item => item.AssessmentId);
            entity.HasIndex(item => item.UserId);
            entity.Property(item => item.AssessmentCode)
                .HasMaxLength(50)
                .IsUnicode(false)
                .IsRequired();
            entity.Property(item => item.LanguageCode)
                .HasMaxLength(10)
                .IsUnicode(false)
                .IsRequired();
            entity.Property(item => item.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .IsRequired();
            entity.Property(item => item.StartedOn)
                .HasDefaultValueSql("GETUTCDATE()");

            entity.HasOne(item => item.Participant)
                .WithMany(item => item.Assessments)
                .HasForeignKey(item => item.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AssessmentStepTracking>(entity =>
        {
            entity.ToTable("AssessmentStepTracking");
            entity.HasKey(item => item.TrackingId);
            entity.HasIndex(item => new { item.UserId, item.AssessmentId });
            entity.Property(item => item.StepCode)
                .HasMaxLength(100)
                .IsUnicode(false)
                .IsRequired();
            entity.Property(item => item.EventType)
                .HasMaxLength(20)
                .IsUnicode(false)
                .IsRequired();
            entity.Property(item => item.PageVersion)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(item => item.RecordedOn)
                .HasDefaultValueSql("GETUTCDATE()");

            entity.HasOne(item => item.Assessment)
                .WithMany(item => item.StepTracking)
                .HasPrincipalKey(item => item.AssessmentId)
                .HasForeignKey(item => item.AssessmentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AssessmentAnswer>(entity =>
        {
            entity.ToTable("AssessmentAnswer");
            entity.HasKey(item => item.AnswerId);
            entity.HasIndex(item => new { item.UserId, item.AssessmentId });
            entity.Property(item => item.StepName).HasMaxLength(200).IsRequired();
            entity.Property(item => item.ModuleName).HasMaxLength(200).IsRequired();
            entity.Property(item => item.Question).IsRequired();
            entity.Property(item => item.AnswerText).IsRequired();
            entity.Property(item => item.Score).HasPrecision(18, 2);
            entity.Property(item => item.AnsweredOn)
                .HasDefaultValueSql("GETUTCDATE()");

            entity.HasOne(item => item.Assessment)
                .WithMany(item => item.Answers)
                .HasPrincipalKey(item => item.AssessmentId)
                .HasForeignKey(item => item.AssessmentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AssessmentResult>(entity =>
        {
            entity.ToTable("AssessmentResult");
            entity.HasKey(item => item.Id);
            entity.HasIndex(item => item.AssessmentId).IsUnique();
            entity.Property(item => item.Score).HasPrecision(18, 2);
            entity.Property(item => item.RiskLevel)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(item => item.DecisionPathway)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(item => item.CreatedOn)
                .HasDefaultValueSql("GETUTCDATE()");

            entity.HasOne(item => item.Assessment)
                .WithOne(item => item.Result)
                .HasPrincipalKey<AssessmentSession>(item => item.AssessmentId)
                .HasForeignKey<AssessmentResult>(item => item.AssessmentId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
