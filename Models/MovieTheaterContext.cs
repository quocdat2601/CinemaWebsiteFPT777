using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace MovieTheater.Models;

public partial class MovieTheaterContext : DbContext
{
    public MovieTheaterContext()
    {
    }

    public MovieTheaterContext(DbContextOptions<MovieTheaterContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Account> Accounts { get; set; }

    public virtual DbSet<CinemaRoom> CinemaRooms { get; set; }

    public virtual DbSet<ConditionType> ConditionTypes { get; set; }

    public virtual DbSet<CoupleSeat> CoupleSeats { get; set; }

    public virtual DbSet<Employee> Employees { get; set; }

    public virtual DbSet<Food> Foods { get; set; }

    public virtual DbSet<FoodInvoice> FoodInvoices { get; set; }

    public virtual DbSet<Invoice> Invoices { get; set; }

    public virtual DbSet<Member> Members { get; set; }

    public virtual DbSet<Movie> Movies { get; set; }

    public virtual DbSet<MovieShow> MovieShows { get; set; }

    public virtual DbSet<Promotion> Promotions { get; set; }

    public virtual DbSet<PromotionCondition> PromotionConditions { get; set; }

    public virtual DbSet<Rank> Ranks { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<Schedule> Schedules { get; set; }

    public virtual DbSet<ScheduleSeat> ScheduleSeats { get; set; }

    public virtual DbSet<Seat> Seats { get; set; }

    public virtual DbSet<SeatStatus> SeatStatuses { get; set; }

    public virtual DbSet<SeatType> SeatTypes { get; set; }

    public virtual DbSet<Type> Types { get; set; }

    public virtual DbSet<Version> Versions { get; set; }

    public virtual DbSet<Voucher> Vouchers { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=(local);uid=sa;pwd=12345;database=MovieTheater;TrustServerCertificate=True;");

#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=(local);uid=sa;pwd=12345;database=MovieTheater;TrustServerCertificate=True;");

#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=(local);uid=sa;pwd=12345;database=MovieTheater;TrustServerCertificate=True;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Account>(entity =>
        {
            entity.HasKey(e => e.AccountId).HasName("PK__Account__B19E45C9F4AF9F6D");

            entity.ToTable("Account");

            entity.HasIndex(e => e.Email, "UQ__Account__A9D105347F891A66").IsUnique();

            entity.HasIndex(e => e.Username, "UQ__Account__B15BE12E3F26DFE1").IsUnique();

            entity.Property(e => e.AccountId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("Account_ID");
            entity.Property(e => e.Address)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.DateOfBirth).HasColumnName("Date_Of_Birth");
            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.FullName)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("Full_Name");
            entity.Property(e => e.Gender)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.IdentityCard)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("Identity_Card");
            entity.Property(e => e.Image)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.Password)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.PhoneNumber)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("Phone_Number");
            entity.Property(e => e.RankId).HasColumnName("Rank_ID");
            entity.Property(e => e.RegisterDate).HasColumnName("Register_Date");
            entity.Property(e => e.RoleId).HasColumnName("Role_ID");
            entity.Property(e => e.Status).HasColumnName("STATUS");
            entity.Property(e => e.Username)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("USERNAME");

            entity.HasOne(d => d.Rank).WithMany(p => p.Accounts)
                .HasForeignKey(d => d.RankId)
                .HasConstraintName("FK_Account_Rank");

            entity.HasOne(d => d.Role).WithMany(p => p.Accounts)
                .HasForeignKey(d => d.RoleId)
                .HasConstraintName("FK_Account_Role");

            entity.HasMany(d => d.Movies).WithMany(p => p.Accounts)
                .UsingEntity<Dictionary<string, object>>(
                    "Wishlist",
                    r => r.HasOne<Movie>().WithMany()
                        .HasForeignKey("MovieId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK__Wishlist__Movie___10566F31"),
                    l => l.HasOne<Account>().WithMany()
                        .HasForeignKey("AccountId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK__Wishlist__Accoun__0F624AF8"),
                    j =>
                    {
                        j.HasKey("AccountId", "MovieId").HasName("PK__Wishlist__F636C589C1DDC923");
                        j.ToTable("Wishlist");
                        j.IndexerProperty<string>("AccountId")
                            .HasMaxLength(10)
                            .IsUnicode(false)
                            .HasColumnName("Account_ID");
                        j.IndexerProperty<string>("MovieId")
                            .HasMaxLength(10)
                            .IsUnicode(false)
                            .HasColumnName("Movie_ID");
                    });
        });

        modelBuilder.Entity<CinemaRoom>(entity =>
        {
            entity.HasKey(e => e.CinemaRoomId).HasName("PK__Cinema_R__E15FECAA6894FA88");

            entity.ToTable("Cinema_Room");

            entity.Property(e => e.CinemaRoomId).HasColumnName("Cinema_Room_ID");
            entity.Property(e => e.CinemaRoomName)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("Cinema_Room_Name");
            entity.Property(e => e.SeatLength).HasColumnName("Seat_Length");
            entity.Property(e => e.SeatQuantity)
                .HasComputedColumnSql("([Seat_Width]*[Seat_Length])", false)
                .HasColumnName("Seat_Quantity");
            entity.Property(e => e.SeatWidth).HasColumnName("Seat_Width");
            entity.Property(e => e.VersionId).HasColumnName("Version_ID");

            entity.HasOne(d => d.Version).WithMany(p => p.CinemaRooms)
                .HasForeignKey(d => d.VersionId)
                .HasConstraintName("FK__Cinema_Ro__Versi__5BE2A6F2");
        });

        modelBuilder.Entity<ConditionType>(entity =>
        {
            entity.HasKey(e => e.ConditionTypeId).HasName("PK__Conditio__8DF87998E8DB3A89");

            entity.ToTable("ConditionType");

            entity.Property(e => e.ConditionTypeId)
                .ValueGeneratedNever()
                .HasColumnName("ConditionType_ID");
            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<CoupleSeat>(entity =>
        {
            entity.HasKey(e => e.CoupleSeatId).HasName("PK__CoupleSe__B9EB02081A74D869");

            entity.ToTable("CoupleSeat");

            entity.HasIndex(e => e.FirstSeatId, "UQ_CoupleSeat_First").IsUnique();

            entity.HasIndex(e => e.SecondSeatId, "UQ_CoupleSeat_Second").IsUnique();

            entity.HasOne(d => d.FirstSeat).WithOne(p => p.CoupleSeatFirstSeat)
                .HasForeignKey<CoupleSeat>(d => d.FirstSeatId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__CoupleSea__First__02FC7413");

            entity.HasOne(d => d.SecondSeat).WithOne(p => p.CoupleSeatSecondSeat)
                .HasForeignKey<CoupleSeat>(d => d.SecondSeatId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__CoupleSea__Secon__03F0984C");
        });

        modelBuilder.Entity<Employee>(entity =>
        {
            entity.HasKey(e => e.EmployeeId).HasName("PK__Employee__781134815068EC2A");

            entity.ToTable("Employee");

            entity.Property(e => e.EmployeeId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("Employee_ID");
            entity.Property(e => e.AccountId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("Account_ID");

            entity.HasOne(d => d.Account).WithMany(p => p.Employees)
                .HasForeignKey(d => d.AccountId)
                .HasConstraintName("FK_Employee_Account");
        });

        modelBuilder.Entity<Food>(entity =>
        {
            entity.HasKey(e => e.FoodId).HasName("PK__Food__856DB3EBAADC4A0F");

            entity.ToTable("Food");

            entity.Property(e => e.Category)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Description)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.Image)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.Price).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Status).HasDefaultValue(true);
            entity.Property(e => e.UpdatedDate).HasColumnType("datetime");
        });

        modelBuilder.Entity<FoodInvoice>(entity =>
        {
            entity.HasKey(e => e.FoodInvoiceId).HasName("PK__FoodInvo__D92943142F9DBE7E");

            entity.ToTable("FoodInvoice");

            entity.HasIndex(e => e.FoodId, "IX_FoodInvoice_FoodID");

            entity.HasIndex(e => e.InvoiceId, "IX_FoodInvoice_InvoiceID");

            entity.Property(e => e.FoodInvoiceId).HasColumnName("FoodInvoice_ID");
            entity.Property(e => e.FoodId).HasColumnName("Food_ID");
            entity.Property(e => e.InvoiceId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("Invoice_ID");
            entity.Property(e => e.Price).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.Food).WithMany(p => p.FoodInvoices)
                .HasForeignKey(d => d.FoodId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_FoodInvoice_Food");

            entity.HasOne(d => d.Invoice).WithMany(p => p.FoodInvoices)
                .HasForeignKey(d => d.InvoiceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_FoodInvoice_Invoice");
        });

        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.HasKey(e => e.InvoiceId).HasName("PK__Invoice__0DE60494D04E25B4");

            entity.ToTable("Invoice");

            entity.Property(e => e.InvoiceId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("Invoice_ID");
            entity.Property(e => e.AccountId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("Account_ID");
            entity.Property(e => e.AddScore).HasColumnName("Add_Score");
            entity.Property(e => e.BookingDate).HasColumnType("datetime");
            entity.Property(e => e.MovieShowId).HasColumnName("Movie_Show_Id");
            entity.Property(e => e.PromotionDiscount)
                .HasDefaultValue(0)
                .HasColumnName("Promotion_Discount");
            entity.Property(e => e.RankDiscountPercentage).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.Seat)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.SeatIds).HasColumnName("Seat_IDs");
            entity.Property(e => e.TotalMoney)
                .HasColumnType("decimal(18, 0)")
                .HasColumnName("Total_Money");
            entity.Property(e => e.UseScore).HasColumnName("Use_Score");
            entity.Property(e => e.VoucherId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("Voucher_ID");

            entity.HasOne(d => d.Account).WithMany(p => p.Invoices)
                .HasForeignKey(d => d.AccountId)
                .HasConstraintName("FK_Invoice_Account");

            entity.HasOne(d => d.MovieShow).WithMany(p => p.Invoices)
                .HasForeignKey(d => d.MovieShowId)
                .HasConstraintName("FK__Invoice__Movie_S__75A278F5");

            entity.HasOne(d => d.Voucher).WithMany(p => p.Invoices)
                .HasForeignKey(d => d.VoucherId)
                .HasConstraintName("FK__Invoice__Voucher__76969D2E");
        });

        modelBuilder.Entity<Member>(entity =>
        {
            entity.HasKey(e => e.MemberId).HasName("PK__Member__42A68F27E5D35AD3");

            entity.ToTable("Member");

            entity.Property(e => e.MemberId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("Member_ID");
            entity.Property(e => e.AccountId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("Account_ID");
            entity.Property(e => e.TotalPoints).HasColumnName("Total_Points");

            entity.HasOne(d => d.Account).WithMany(p => p.Members)
                .HasForeignKey(d => d.AccountId)
                .HasConstraintName("FK_Member_Account");
        });

        modelBuilder.Entity<Movie>(entity =>
        {
            entity.HasKey(e => e.MovieId).HasName("PK__Movie__7A88040515AE3768");

            entity.ToTable("Movie");

            entity.Property(e => e.MovieId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("Movie_ID");
            entity.Property(e => e.Actor)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.CinemaRoomId).HasColumnName("Cinema_Room_ID");
            entity.Property(e => e.Content)
                .HasMaxLength(1000)
                .IsUnicode(false);
            entity.Property(e => e.Director)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.FromDate).HasColumnName("From_Date");
            entity.Property(e => e.LargeImage)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("Large_Image");
            entity.Property(e => e.MovieNameEnglish)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("Movie_Name_English");
            entity.Property(e => e.MovieNameVn)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("Movie_Name_VN");
            entity.Property(e => e.MovieProductionCompany)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("Movie_Production_Company");
            entity.Property(e => e.SmallImage)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("Small_Image");
            entity.Property(e => e.ToDate).HasColumnName("To_Date");
            entity.Property(e => e.TrailerUrl)
                .HasMaxLength(255)
                .IsUnicode(false);

            entity.HasMany(d => d.Types).WithMany(p => p.Movies)
                .UsingEntity<Dictionary<string, object>>(
                    "MovieType",
                    r => r.HasOne<Type>().WithMany()
                        .HasForeignKey("TypeId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK__Movie_Typ__Type___59063A47"),
                    l => l.HasOne<Movie>().WithMany()
                        .HasForeignKey("MovieId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK__Movie_Typ__Movie__5812160E"),
                    j =>
                    {
                        j.HasKey("MovieId", "TypeId").HasName("PK__Movie_Ty__856109DAD3614F01");
                        j.ToTable("Movie_Type");
                        j.IndexerProperty<string>("MovieId")
                            .HasMaxLength(10)
                            .IsUnicode(false)
                            .HasColumnName("Movie_ID");
                        j.IndexerProperty<int>("TypeId").HasColumnName("Type_ID");
                    });

            entity.HasMany(d => d.Versions).WithMany(p => p.Movies)
                .UsingEntity<Dictionary<string, object>>(
                    "MovieVersion",
                    r => r.HasOne<Version>().WithMany()
                        .HasForeignKey("VersionId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK__Movie_Ver__Versi__5535A963"),
                    l => l.HasOne<Movie>().WithMany()
                        .HasForeignKey("MovieId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK__Movie_Ver__Movie__5441852A"),
                    j =>
                    {
                        j.HasKey("MovieId", "VersionId").HasName("PK__Movie_Ve__9A8607D556C753FE");
                        j.ToTable("Movie_Version");
                        j.IndexerProperty<string>("MovieId")
                            .HasMaxLength(10)
                            .IsUnicode(false)
                            .HasColumnName("Movie_ID");
                        j.IndexerProperty<int>("VersionId").HasColumnName("Version_ID");
                    });
        });

        modelBuilder.Entity<MovieShow>(entity =>
        {
            entity.HasKey(e => e.MovieShowId).HasName("PK__Movie_Sh__7616F8A01A075CE5");

            entity.ToTable("Movie_Show");

            entity.Property(e => e.MovieShowId).HasColumnName("Movie_Show_ID");
            entity.Property(e => e.CinemaRoomId).HasColumnName("Cinema_Room_ID");
            entity.Property(e => e.MovieId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("Movie_ID");
            entity.Property(e => e.ScheduleId).HasColumnName("Schedule_ID");
            entity.Property(e => e.ShowDate).HasColumnName("Show_Date");
            entity.Property(e => e.VersionId).HasColumnName("Version_ID");

            entity.HasOne(d => d.CinemaRoom).WithMany(p => p.MovieShows)
                .HasForeignKey(d => d.CinemaRoomId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Movie_Sho__Cinem__60A75C0F");

            entity.HasOne(d => d.Movie).WithMany(p => p.MovieShows)
                .HasForeignKey(d => d.MovieId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Movie_Sho__Movie__5FB337D6");

            entity.HasOne(d => d.Schedule).WithMany(p => p.MovieShows)
                .HasForeignKey(d => d.ScheduleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Movie_Sho__Sched__619B8048");

            entity.HasOne(d => d.Version).WithMany(p => p.MovieShows)
                .HasForeignKey(d => d.VersionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Movie_Sho__Versi__5EBF139D");
        });

        modelBuilder.Entity<Promotion>(entity =>
        {
            entity.HasKey(e => e.PromotionId).HasName("PK__Promotio__DAF79AFBF7708C0C");

            entity.ToTable("Promotion");

            entity.Property(e => e.PromotionId)
                .ValueGeneratedNever()
                .HasColumnName("Promotion_ID");
            entity.Property(e => e.Detail)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.DiscountLevel).HasColumnName("Discount_Level");
            entity.Property(e => e.EndTime)
                .HasColumnType("datetime")
                .HasColumnName("End_Time");
            entity.Property(e => e.Image)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("Is_Active");
            entity.Property(e => e.StartTime)
                .HasColumnType("datetime")
                .HasColumnName("Start_Time");
            entity.Property(e => e.Title)
                .HasMaxLength(255)
                .IsUnicode(false);
        });

        modelBuilder.Entity<PromotionCondition>(entity =>
        {
            entity.HasKey(e => e.ConditionId).HasName("PK__Promotio__D4F58B851D8AE2E5");

            entity.ToTable("PromotionCondition");

            entity.Property(e => e.ConditionId).HasColumnName("Condition_ID");
            entity.Property(e => e.ConditionTypeId).HasColumnName("ConditionType_ID");
            entity.Property(e => e.Operator)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.PromotionId).HasColumnName("Promotion_ID");
            entity.Property(e => e.TargetEntity)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("Target_Entity");
            entity.Property(e => e.TargetField)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("Target_Field");
            entity.Property(e => e.TargetValue)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("Target_Value");

            entity.HasOne(d => d.ConditionType).WithMany(p => p.PromotionConditions)
                .HasForeignKey(d => d.ConditionTypeId)
                .HasConstraintName("FK__Promotion__Condi__0C85DE4D");

            entity.HasOne(d => d.Promotion).WithMany(p => p.PromotionConditions)
                .HasForeignKey(d => d.PromotionId)
                .HasConstraintName("FK__Promotion__Promo__0B91BA14");
        });

        modelBuilder.Entity<Rank>(entity =>
        {
            entity.ToTable("Rank");

            entity.HasIndex(e => e.RankName, "UQ_Rank_RankName").IsUnique();

            entity.Property(e => e.RankId).HasColumnName("Rank_ID");
            entity.Property(e => e.ColorGradient)
                .HasMaxLength(200)
                .HasDefaultValue("linear-gradient(135deg, #4e54c8 0%, #6c63ff 50%, #8f94fb 100%)");
            entity.Property(e => e.DiscountPercentage)
                .HasColumnType("decimal(5, 2)")
                .HasColumnName("Discount_Percentage");
            entity.Property(e => e.IconClass)
                .HasMaxLength(50)
                .HasDefaultValue("fa-crown");
            entity.Property(e => e.PointEarningPercentage).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.RankName)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("Rank_Name");
            entity.Property(e => e.RequiredPoints).HasColumnName("Required_Points");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RoleId).HasName("PK__Roles__D80AB49B003D89D6");

            entity.Property(e => e.RoleId)
                .ValueGeneratedNever()
                .HasColumnName("Role_ID");
            entity.Property(e => e.RoleName)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("Role_Name");
        });

        modelBuilder.Entity<Schedule>(entity =>
        {
            entity.HasKey(e => e.ScheduleId).HasName("PK__Schedule__8C4D3BBB594F5961");

            entity.ToTable("Schedule");

            entity.Property(e => e.ScheduleId).HasColumnName("Schedule_ID");
            entity.Property(e => e.ScheduleTime).HasColumnName("Schedule_Time");
        });

        modelBuilder.Entity<ScheduleSeat>(entity =>
        {
            entity.HasKey(e => e.ScheduleSeatId).HasName("PK__Schedule__C3F9AE859E5A0AA5");

            entity.ToTable("Schedule_Seat");

            entity.Property(e => e.ScheduleSeatId).HasColumnName("Schedule_Seat_ID");
            entity.Property(e => e.BookedPrice)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("Booked_Price");
            entity.Property(e => e.HoldBy).HasMaxLength(100);
            entity.Property(e => e.HoldUntil).HasColumnType("datetime");
            entity.Property(e => e.InvoiceId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("Invoice_ID");
            entity.Property(e => e.MovieShowId).HasColumnName("Movie_Show_ID");
            entity.Property(e => e.SeatId).HasColumnName("Seat_ID");
            entity.Property(e => e.SeatStatusId).HasColumnName("Seat_Status_ID");

            entity.HasOne(d => d.Invoice).WithMany(p => p.ScheduleSeats)
                .HasForeignKey(d => d.InvoiceId)
                .HasConstraintName("FK__Schedule___Invoi__7B5B524B");

            entity.HasOne(d => d.MovieShow).WithMany(p => p.ScheduleSeats)
                .HasForeignKey(d => d.MovieShowId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK__Schedule___Movie__797309D9");

            entity.HasOne(d => d.Seat).WithMany(p => p.ScheduleSeats)
                .HasForeignKey(d => d.SeatId)
                .HasConstraintName("FK__Schedule___Seat___7A672E12");

            entity.HasOne(d => d.SeatStatus).WithMany(p => p.ScheduleSeats)
                .HasForeignKey(d => d.SeatStatusId)
                .HasConstraintName("FK__Schedule___Seat___7C4F7684");
        });

        modelBuilder.Entity<Seat>(entity =>
        {
            entity.HasKey(e => e.SeatId).HasName("PK__Seat__8B2CE7B64F9F3931");

            entity.ToTable("Seat");

            entity.Property(e => e.SeatId).HasColumnName("Seat_ID");
            entity.Property(e => e.CinemaRoomId).HasColumnName("Cinema_Room_ID");
            entity.Property(e => e.SeatColumn)
                .HasMaxLength(5)
                .IsUnicode(false)
                .HasColumnName("Seat_Column");
            entity.Property(e => e.SeatName)
                .HasMaxLength(5)
                .IsUnicode(false);
            entity.Property(e => e.SeatRow).HasColumnName("Seat_Row");
            entity.Property(e => e.SeatStatusId).HasColumnName("Seat_Status_ID");
            entity.Property(e => e.SeatTypeId).HasColumnName("Seat_Type_ID");

            entity.HasOne(d => d.CinemaRoom).WithMany(p => p.Seats)
                .HasForeignKey(d => d.CinemaRoomId)
                .HasConstraintName("FK__Seat__Cinema_Roo__6EF57B66");

            entity.HasOne(d => d.SeatStatus).WithMany(p => p.Seats)
                .HasForeignKey(d => d.SeatStatusId)
                .HasConstraintName("FK__Seat__Seat_Statu__6FE99F9F");

            entity.HasOne(d => d.SeatType).WithMany(p => p.Seats)
                .HasForeignKey(d => d.SeatTypeId)
                .HasConstraintName("FK__Seat__Seat_Type___70DDC3D8");
        });

        modelBuilder.Entity<SeatStatus>(entity =>
        {
            entity.HasKey(e => e.SeatStatusId).HasName("PK__Seat_Sta__228AAF67504A4597");

            entity.ToTable("Seat_Status");

            entity.Property(e => e.SeatStatusId).HasColumnName("Seat_Status_ID");
            entity.Property(e => e.StatusName)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("Status_Name");
        });

        modelBuilder.Entity<SeatType>(entity =>
        {
            entity.HasKey(e => e.SeatTypeId).HasName("PK__Seat_Typ__BDB07EDCF203D5AD");

            entity.ToTable("Seat_Type");

            entity.Property(e => e.SeatTypeId).HasColumnName("Seat_Type_ID");
            entity.Property(e => e.ColorHex)
                .HasMaxLength(7)
                .IsUnicode(false)
                .HasDefaultValue("#FFFFFF");
            entity.Property(e => e.PricePercent)
                .HasDefaultValue(100m)
                .HasColumnType("decimal(18, 0)")
                .HasColumnName("Price_Percent");
            entity.Property(e => e.TypeName)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("Type_Name");
        });

        modelBuilder.Entity<Type>(entity =>
        {
            entity.HasKey(e => e.TypeId).HasName("PK__Type__FE90DDFE16833EEB");

            entity.ToTable("Type");

            entity.Property(e => e.TypeId)
                .ValueGeneratedNever()
                .HasColumnName("Type_ID");
            entity.Property(e => e.TypeName)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("Type_Name");
        });

        modelBuilder.Entity<Version>(entity =>
        {
            entity.HasKey(e => e.VersionId).HasName("PK__Version__00E03D0F2CBE0D29");

            entity.ToTable("Version");

            entity.Property(e => e.VersionId).HasColumnName("Version_ID");
            entity.Property(e => e.Multi).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.VersionName)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("Version_Name");
        });

        modelBuilder.Entity<Voucher>(entity =>
        {
            entity.HasKey(e => e.VoucherId).HasName("PK__Voucher__D753929C572C5BC4");

            entity.ToTable("Voucher");

            entity.HasIndex(e => e.Code, "UQ__Voucher__A25C5AA776943F15").IsUnique();

            entity.Property(e => e.VoucherId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("Voucher_ID");
            entity.Property(e => e.AccountId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("Account_ID");
            entity.Property(e => e.Code).HasMaxLength(20);
            entity.Property(e => e.CreatedDate).HasColumnType("datetime");
            entity.Property(e => e.ExpiryDate).HasColumnType("datetime");
            entity.Property(e => e.Image)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.IsUsed).HasDefaultValue(false);
            entity.Property(e => e.Value).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.Account).WithMany(p => p.Vouchers)
                .HasForeignKey(d => d.AccountId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Voucher_Account");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
