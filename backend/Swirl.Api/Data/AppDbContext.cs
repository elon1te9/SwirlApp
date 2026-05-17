using Microsoft.EntityFrameworkCore;
using Swirl.Api.Models;

namespace Swirl.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; } = null!;

    public DbSet<UserProfile> UserProfiles { get; set; } = null!;

    public DbSet<Avatar> Avatars { get; set; } = null!;

    public DbSet<Section> Sections { get; set; } = null!;

    public DbSet<Level> Levels { get; set; } = null!;

    public DbSet<Word> Words { get; set; } = null!;

    public DbSet<Exercise> Exercises { get; set; } = null!;

    public DbSet<ExerciseOption> ExerciseOptions { get; set; } = null!;

    public DbSet<UserLevelProgress> UserLevelProgresses { get; set; } = null!;

    public DbSet<UserWordProgress> UserWordProgresses { get; set; } = null!;

    public DbSet<LevelAttempt> LevelAttempts { get; set; } = null!;

    public DbSet<UserAnswer> UserAnswers { get; set; } = null!;

    public DbSet<DailyTest> DailyTests { get; set; } = null!;

    public DbSet<DailyTestAnswer> DailyTestAnswers { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .HasColumnType("uuid")
                .ValueGeneratedNever();

            entity.Property(e => e.Email)
                .HasColumnName("email")
                .HasMaxLength(255)
                .IsRequired();

            entity.Property(e => e.PasswordHash)
                .HasColumnName("password_hash")
                .HasColumnType("text")
                .IsRequired();

            entity.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .HasColumnType("timestamp without time zone")
                .IsRequired();

            entity.Property(e => e.UpdatedAt)
                .HasColumnName("updated_at")
                .HasColumnType("timestamp without time zone");

            entity.HasIndex(e => e.Email)
                .IsUnique();
        });

        modelBuilder.Entity<UserProfile>(entity =>
        {
            entity.ToTable("user_profiles");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .HasColumnType("uuid")
                .ValueGeneratedNever();

            entity.Property(e => e.UserId)
                .HasColumnName("user_id")
                .HasColumnType("uuid")
                .IsRequired();

            entity.Property(e => e.Name)
                .HasColumnName("name")
                .HasMaxLength(255)
                .IsRequired();

            entity.Property(e => e.AvatarId)
                .HasColumnName("avatar_id")
                .IsRequired();

            entity.Property(e => e.CurrentStreak)
                .HasColumnName("current_streak")
                .HasDefaultValue(0)
                .IsRequired();

            entity.Property(e => e.BestStreak)
                .HasColumnName("best_streak")
                .HasDefaultValue(0)
                .IsRequired();

            entity.Property(e => e.LastActivityDate)
                .HasColumnName("last_activity_date")
                .HasColumnType("date");

            entity.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .HasColumnType("timestamp without time zone")
                .IsRequired();

            entity.Property(e => e.UpdatedAt)
                .HasColumnName("updated_at")
                .HasColumnType("timestamp without time zone");

            entity.HasIndex(e => e.UserId)
                .IsUnique();

            entity.HasOne(e => e.User)
                .WithOne(e => e.UserProfile)
                .HasForeignKey<UserProfile>(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Avatar)
                .WithMany(e => e.UserProfiles)
                .HasForeignKey(e => e.AvatarId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Avatar>(entity =>
        {
            entity.ToTable("avatars");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .ValueGeneratedOnAdd();

            entity.Property(e => e.Name)
                .HasColumnName("name")
                .HasMaxLength(255)
                .IsRequired();

            entity.Property(e => e.ImageUrl)
                .HasColumnName("image_url")
                .HasColumnType("text")
                .IsRequired();

            entity.Property(e => e.IsActive)
                .HasColumnName("is_active")
                .HasDefaultValue(true)
                .IsRequired();
        });

        modelBuilder.Entity<Section>(entity =>
        {
            entity.ToTable("sections");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .ValueGeneratedOnAdd();

            entity.Property(e => e.Title)
                .HasColumnName("title")
                .HasMaxLength(255)
                .IsRequired();

            entity.Property(e => e.Description)
                .HasColumnName("description")
                .HasColumnType("text");

            entity.Property(e => e.ImageUrl)
                .HasColumnName("image_url")
                .HasColumnType("text");

            entity.Property(e => e.SortOrder)
                .HasColumnName("sort_order")
                .IsRequired();

            entity.Property(e => e.IsActive)
                .HasColumnName("is_active")
                .HasDefaultValue(true)
                .IsRequired();

            entity.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .HasColumnType("timestamp without time zone")
                .IsRequired();

            entity.Property(e => e.UpdatedAt)
                .HasColumnName("updated_at")
                .HasColumnType("timestamp without time zone");
        });

        modelBuilder.Entity<Level>(entity =>
        {
            entity.ToTable("levels");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .ValueGeneratedOnAdd();

            entity.Property(e => e.SectionId)
                .HasColumnName("section_id")
                .IsRequired();

            entity.Property(e => e.Title)
                .HasColumnName("title")
                .HasMaxLength(255)
                .IsRequired();

            entity.Property(e => e.Description)
                .HasColumnName("description")
                .HasColumnType("text");

            entity.Property(e => e.LevelNumber)
                .HasColumnName("level_number")
                .IsRequired();

            entity.Property(e => e.CefrLevel)
                .HasColumnName("cefr_level")
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(e => e.IsFinalTest)
                .HasColumnName("is_final_test")
                .HasDefaultValue(false)
                .IsRequired();

            entity.Property(e => e.SortOrder)
                .HasColumnName("sort_order")
                .IsRequired();

            entity.Property(e => e.IsActive)
                .HasColumnName("is_active")
                .HasDefaultValue(true)
                .IsRequired();

            entity.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .HasColumnType("timestamp without time zone")
                .IsRequired();

            entity.Property(e => e.UpdatedAt)
                .HasColumnName("updated_at")
                .HasColumnType("timestamp without time zone");

            entity.HasOne(e => e.Section)
                .WithMany(e => e.Levels)
                .HasForeignKey(e => e.SectionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Word>(entity =>
        {
            entity.ToTable("words");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .ValueGeneratedOnAdd();

            entity.Property(e => e.LevelId)
                .HasColumnName("level_id")
                .IsRequired();

            entity.Property(e => e.English)
                .HasColumnName("english")
                .HasMaxLength(255)
                .IsRequired();

            entity.Property(e => e.Russian)
                .HasColumnName("russian")
                .HasMaxLength(255)
                .IsRequired();

            entity.Property(e => e.Transcription)
                .HasColumnName("transcription")
                .HasMaxLength(255);

            entity.Property(e => e.PartOfSpeech)
                .HasColumnName("part_of_speech")
                .HasMaxLength(100);

            entity.Property(e => e.ImageUrl)
                .HasColumnName("image_url")
                .HasColumnType("text");

            entity.Property(e => e.AudioUrl)
                .HasColumnName("audio_url")
                .HasColumnType("text");

            entity.Property(e => e.CefrLevel)
                .HasColumnName("cefr_level")
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(e => e.IsActive)
                .HasColumnName("is_active")
                .HasDefaultValue(true)
                .IsRequired();

            entity.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .HasColumnType("timestamp without time zone")
                .IsRequired();

            entity.Property(e => e.UpdatedAt)
                .HasColumnName("updated_at")
                .HasColumnType("timestamp without time zone");

            entity.HasOne(e => e.Level)
                .WithMany(e => e.Words)
                .HasForeignKey(e => e.LevelId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Exercise>(entity =>
        {
            entity.ToTable("exercises");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .ValueGeneratedOnAdd();

            entity.Property(e => e.LevelId)
                .HasColumnName("level_id")
                .IsRequired();

            entity.Property(e => e.WordId)
                .HasColumnName("word_id")
                .IsRequired();

            entity.Property(e => e.Type)
                .HasColumnName("type")
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(e => e.QuestionText)
                .HasColumnName("question_text")
                .HasColumnType("text");

            entity.Property(e => e.CorrectAnswer)
                .HasColumnName("correct_answer")
                .HasColumnType("text")
                .IsRequired();

            entity.Property(e => e.SortOrder)
                .HasColumnName("sort_order");

            entity.Property(e => e.IsActive)
                .HasColumnName("is_active")
                .HasDefaultValue(true)
                .IsRequired();

            entity.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .HasColumnType("timestamp without time zone")
                .IsRequired();

            entity.Property(e => e.UpdatedAt)
                .HasColumnName("updated_at")
                .HasColumnType("timestamp without time zone");

            entity.HasOne(e => e.Level)
                .WithMany(e => e.Exercises)
                .HasForeignKey(e => e.LevelId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Word)
                .WithMany(e => e.Exercises)
                .HasForeignKey(e => e.WordId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ExerciseOption>(entity =>
        {
            entity.ToTable("exercise_options");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .ValueGeneratedOnAdd();

            entity.Property(e => e.ExerciseId)
                .HasColumnName("exercise_id")
                .IsRequired();

            entity.Property(e => e.OptionText)
                .HasColumnName("option_text")
                .HasColumnType("text")
                .IsRequired();

            entity.Property(e => e.IsCorrect)
                .HasColumnName("is_correct")
                .IsRequired();

            entity.Property(e => e.SortOrder)
                .HasColumnName("sort_order");

            entity.HasOne(e => e.Exercise)
                .WithMany(e => e.ExerciseOptions)
                .HasForeignKey(e => e.ExerciseId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UserLevelProgress>(entity =>
        {
            entity.ToTable("user_level_progress");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .ValueGeneratedOnAdd();

            entity.Property(e => e.UserId)
                .HasColumnName("user_id")
                .HasColumnType("uuid")
                .IsRequired();

            entity.Property(e => e.LevelId)
                .HasColumnName("level_id")
                .IsRequired();

            entity.Property(e => e.Status)
                .HasColumnName("status")
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(e => e.WordsLearned)
                .HasColumnName("words_learned")
                .HasDefaultValue(false)
                .IsRequired();

            entity.Property(e => e.CompletedAt)
                .HasColumnName("completed_at")
                .HasColumnType("timestamp without time zone");

            entity.Property(e => e.UnlockedAt)
                .HasColumnName("unlocked_at")
                .HasColumnType("timestamp without time zone");

            entity.Property(e => e.AttemptsCount)
                .HasColumnName("attempts_count")
                .HasDefaultValue(0)
                .IsRequired();

            entity.HasIndex(e => new { e.UserId, e.LevelId })
                .IsUnique();

            entity.HasOne(e => e.User)
                .WithMany(e => e.UserLevelProgresses)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Level)
                .WithMany(e => e.UserLevelProgresses)
                .HasForeignKey(e => e.LevelId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UserWordProgress>(entity =>
        {
            entity.ToTable("user_word_progress");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .ValueGeneratedOnAdd();

            entity.Property(e => e.UserId)
                .HasColumnName("user_id")
                .HasColumnType("uuid")
                .IsRequired();

            entity.Property(e => e.WordId)
                .HasColumnName("word_id")
                .IsRequired();

            entity.Property(e => e.LearnedAt)
                .HasColumnName("learned_at")
                .HasColumnType("timestamp without time zone")
                .IsRequired();

            entity.HasIndex(e => new { e.UserId, e.WordId })
                .IsUnique();

            entity.HasOne(e => e.User)
                .WithMany(e => e.UserWordProgresses)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Word)
                .WithMany(e => e.UserWordProgresses)
                .HasForeignKey(e => e.WordId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<LevelAttempt>(entity =>
        {
            entity.ToTable("level_attempts");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .ValueGeneratedOnAdd();

            entity.Property(e => e.UserId)
                .HasColumnName("user_id")
                .HasColumnType("uuid")
                .IsRequired();

            entity.Property(e => e.LevelId)
                .HasColumnName("level_id")
                .IsRequired();

            entity.Property(e => e.StartedAt)
                .HasColumnName("started_at")
                .HasColumnType("timestamp without time zone")
                .IsRequired();

            entity.Property(e => e.CompletedAt)
                .HasColumnName("completed_at")
                .HasColumnType("timestamp without time zone");

            entity.Property(e => e.MistakesCount)
                .HasColumnName("mistakes_count")
                .HasDefaultValue(0)
                .IsRequired();

            entity.Property(e => e.IsSuccessful)
                .HasColumnName("is_successful")
                .HasDefaultValue(false)
                .IsRequired();

            entity.HasOne(e => e.User)
                .WithMany(e => e.LevelAttempts)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Level)
                .WithMany(e => e.LevelAttempts)
                .HasForeignKey(e => e.LevelId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UserAnswer>(entity =>
        {
            entity.ToTable("user_answers");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .ValueGeneratedOnAdd();

            entity.Property(e => e.AttemptId)
                .HasColumnName("attempt_id")
                .IsRequired();

            entity.Property(e => e.ExerciseId)
                .HasColumnName("exercise_id")
                .IsRequired();

            entity.Property(e => e.UserAnswerText)
                .HasColumnName("user_answer")
                .HasColumnType("text")
                .IsRequired();

            entity.Property(e => e.IsCorrect)
                .HasColumnName("is_correct")
                .IsRequired();

            entity.Property(e => e.AnsweredAt)
                .HasColumnName("answered_at")
                .HasColumnType("timestamp without time zone")
                .IsRequired();

            entity.Property(e => e.TimeSpentMs)
                .HasColumnName("time_spent_ms");

            entity.HasOne(e => e.Attempt)
                .WithMany(e => e.UserAnswers)
                .HasForeignKey(e => e.AttemptId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Exercise)
                .WithMany(e => e.UserAnswers)
                .HasForeignKey(e => e.ExerciseId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DailyTest>(entity =>
        {
            entity.ToTable("daily_tests");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .ValueGeneratedOnAdd();

            entity.Property(e => e.UserId)
                .HasColumnName("user_id")
                .HasColumnType("uuid")
                .IsRequired();

            entity.Property(e => e.TestDate)
                .HasColumnName("test_date")
                .HasColumnType("date")
                .IsRequired();

            entity.Property(e => e.StartedAt)
                .HasColumnName("started_at")
                .HasColumnType("timestamp without time zone")
                .IsRequired();

            entity.Property(e => e.CompletedAt)
                .HasColumnName("completed_at")
                .HasColumnType("timestamp without time zone");

            entity.Property(e => e.TotalQuestions)
                .HasColumnName("total_questions")
                .HasDefaultValue(0)
                .IsRequired();

            entity.Property(e => e.CorrectAnswers)
                .HasColumnName("correct_answers")
                .HasDefaultValue(0)
                .IsRequired();

            entity.Property(e => e.IsCompleted)
                .HasColumnName("is_completed")
                .HasDefaultValue(false)
                .IsRequired();

            entity.HasIndex(e => new { e.UserId, e.TestDate })
                .IsUnique();

            entity.HasOne(e => e.User)
                .WithMany(e => e.DailyTests)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DailyTestAnswer>(entity =>
        {
            entity.ToTable("daily_test_answers");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .ValueGeneratedOnAdd();

            entity.Property(e => e.DailyTestId)
                .HasColumnName("daily_test_id")
                .IsRequired();

            entity.Property(e => e.WordId)
                .HasColumnName("word_id")
                .IsRequired();

            entity.Property(e => e.ExerciseType)
                .HasColumnName("exercise_type")
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(e => e.UserAnswerText)
                .HasColumnName("user_answer")
                .HasColumnType("text")
                .IsRequired();

            entity.Property(e => e.IsCorrect)
                .HasColumnName("is_correct")
                .IsRequired();

            entity.Property(e => e.AnsweredAt)
                .HasColumnName("answered_at")
                .HasColumnType("timestamp without time zone")
                .IsRequired();

            entity.HasOne(e => e.DailyTest)
                .WithMany(e => e.DailyTestAnswers)
                .HasForeignKey(e => e.DailyTestId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Word)
                .WithMany(e => e.DailyTestAnswers)
                .HasForeignKey(e => e.WordId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
