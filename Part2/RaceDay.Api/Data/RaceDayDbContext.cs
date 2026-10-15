using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data;

/// <summary>
/// EF Core (code-first) model. Table names, column types, keys, defaults and constraints
/// are configured to match the Part 1 ERD and raceday_schema.sql.
/// </summary>
public class RaceDayDbContext : DbContext
{
    public RaceDayDbContext(DbContextOptions<RaceDayDbContext> options) : base(options) { }

    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<RouteInfo> RouteInfos => Set<RouteInfo>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<EventEnrolment> EventEnrolments => Set<EventEnrolment>();
    public DbSet<RaceResult> Results => Set<RaceResult>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // ---------- Users ----------
        modelBuilder.Entity<AppUser>(e =>
        {
            e.ToTable("Users", t => t.HasCheckConstraint("CK_Users_Role", "[Role] IN ('Organiser', 'Participant')"));
            e.HasKey(x => x.UserId).HasName("PK_Users");
            e.Property(x => x.FullName).HasMaxLength(100).IsRequired();
            e.Property(x => x.Email).HasMaxLength(150).IsRequired();
            e.Property(x => x.PasswordHash).HasMaxLength(255).IsRequired();
            e.Property(x => x.Role).HasMaxLength(20).IsRequired();
            e.Property(x => x.PhoneNumber).HasMaxLength(20);
            e.Property(x => x.CreatedAt).HasColumnType("datetime").HasDefaultValueSql("GETDATE()");
            e.HasIndex(x => x.Email).IsUnique().HasDatabaseName("UQ_Users_Email");
        });

        // ---------- Events ----------
        modelBuilder.Entity<Event>(e =>
        {
            e.ToTable("Events", t => t.HasCheckConstraint("CK_Events_Type", "[EventType] IN ('Running', 'Walking', 'Cycling')"));
            e.HasKey(x => x.EventId).HasName("PK_Events");
            e.Property(x => x.EventName).HasMaxLength(150).IsRequired();
            e.Property(x => x.EventType).HasMaxLength(20).IsRequired();
            e.Property(x => x.EventDate).HasColumnType("datetime");
            e.Property(x => x.Location).HasMaxLength(150).IsRequired();
            e.Property(x => x.Description).HasColumnType("nvarchar(max)");
            e.Property(x => x.CreatedAt).HasColumnType("datetime").HasDefaultValueSql("GETDATE()");

            e.HasOne(x => x.Organiser)
                .WithMany(u => u.OrganisedEvents)
                .HasForeignKey(x => x.OrganiserId)
                .HasConstraintName("FK_Events_Organiser")
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ---------- RouteInfo (one-to-one with Events) ----------
        modelBuilder.Entity<RouteInfo>(e =>
        {
            e.ToTable("RouteInfo");
            e.HasKey(x => x.RouteId).HasName("PK_RouteInfo");
            e.Property(x => x.RouteDescription).HasColumnType("nvarchar(max)");
            e.Property(x => x.DistanceKm).HasColumnType("decimal(5,2)");
            e.Property(x => x.ElevationGainM).HasDefaultValue(0);
            e.Property(x => x.StartPoint).HasMaxLength(150);
            e.Property(x => x.EndPoint).HasMaxLength(150);
            e.Property(x => x.MapUrl).HasMaxLength(255);
            e.HasIndex(x => x.EventId).IsUnique().HasDatabaseName("UQ_RouteInfo_EventId");

            e.HasOne(x => x.Event)
                .WithOne(ev => ev.Route)
                .HasForeignKey<RouteInfo>(x => x.EventId)
                .HasConstraintName("FK_RouteInfo_Event")
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ---------- Categories ----------
        modelBuilder.Entity<Category>(e =>
        {
            e.ToTable("Categories");
            e.HasKey(x => x.CategoryId).HasName("PK_Categories");
            e.Property(x => x.CategoryName).HasMaxLength(100).IsRequired();
            e.Property(x => x.DistanceKm).HasColumnType("decimal(5,2)");
            e.Property(x => x.EntryFee).HasColumnType("decimal(8,2)").HasDefaultValue(0m);
            e.Property(x => x.MaxParticipants).HasDefaultValue(500);

            e.HasOne(x => x.Event)
                .WithMany(ev => ev.Categories)
                .HasForeignKey(x => x.EventId)
                .HasConstraintName("FK_Categories_Event")
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ---------- EventEnrolments ----------
        modelBuilder.Entity<EventEnrolment>(e =>
        {
            e.ToTable("EventEnrolments", t => t.HasCheckConstraint("CK_Enrolments_PaymentStatus", "[PaymentStatus] IN ('Pending', 'Paid', 'Cancelled')"));
            e.HasKey(x => x.EnrolmentId).HasName("PK_EventEnrolments");
            e.Property(x => x.BibNumber).HasMaxLength(10).IsRequired();
            e.Property(x => x.EnrolmentDate).HasColumnType("datetime").HasDefaultValueSql("GETDATE()");
            e.Property(x => x.PaymentStatus).HasMaxLength(20).IsRequired().HasDefaultValue("Pending");
            e.HasIndex(x => x.BibNumber).IsUnique().HasDatabaseName("UQ_Enrolments_BibNumber");
            e.HasIndex(x => new { x.ParticipantId, x.CategoryId }).IsUnique().HasDatabaseName("UQ_Enrolments_ParticipantCategory");

            e.HasOne(x => x.Participant)
                .WithMany(u => u.Enrolments)
                .HasForeignKey(x => x.ParticipantId)
                .HasConstraintName("FK_Enrolments_Participant")
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Category)
                .WithMany(c => c.Enrolments)
                .HasForeignKey(x => x.CategoryId)
                .HasConstraintName("FK_Enrolments_Category")
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ---------- Results (one-to-one, optional, with EventEnrolments) ----------
        modelBuilder.Entity<RaceResult>(e =>
        {
            e.ToTable("Results", t => t.HasCheckConstraint("CK_Results_Status", "[Status] IN ('Finished', 'DNF', 'DSQ')"));
            e.HasKey(x => x.ResultId).HasName("PK_Results");
            e.Property(x => x.Status).HasMaxLength(20).IsRequired().HasDefaultValue("Finished");
            e.HasIndex(x => x.EnrolmentId).IsUnique().HasDatabaseName("UQ_Results_EnrolmentId");

            e.HasOne(x => x.Enrolment)
                .WithOne(en => en.Result)
                .HasForeignKey<RaceResult>(x => x.EnrolmentId)
                .HasConstraintName("FK_Results_Enrolment")
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
