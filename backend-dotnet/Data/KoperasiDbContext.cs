using Microsoft.EntityFrameworkCore;
using MyKopdes.Models;

namespace MyKopdes.Data;

// DbContext that maps to the existing MySQL database WITHOUT migrations.
// No migrations are generated; the database schema is managed by the existing
// Laravel migrations and remains untouched by the .NET backend.
public class KoperasiDbContext : DbContext
{
    // No live version detection at startup. The provider version used by
    // MySql.EntityFrameworkCore is fixed; the docker stack still runs on
    // mysql:8.0.
    public KoperasiDbContext(DbContextOptions<KoperasiDbContext> options)
        : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Member> Members => Set<Member>();
    public DbSet<Item> Items => Set<Item>();
    public DbSet<ItemCategory> ItemCategories => Set<ItemCategory>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<TransactionItem> TransactionItems => Set<TransactionItem>();
    public DbSet<Discount> Discounts => Set<Discount>();
    public DbSet<Loan> Loans => Set<Loan>();
    public DbSet<LoanPayment> LoanPayments => Set<LoanPayment>();
    public DbSet<LoanProduct> LoanProducts => Set<LoanProduct>();
    public DbSet<LoanSetting> LoanSettings => Set<LoanSetting>();
    public DbSet<RestockingRecord> RestockingRecords => Set<RestockingRecord>();
    public DbSet<Setting> Settings => Set<Setting>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Shu> Shus => Set<Shu>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure table names to match the existing MySQL schema (snake_case)
        modelBuilder.Entity<User>().ToTable("users");
        modelBuilder.Entity<Member>().ToTable("members");
        modelBuilder.Entity<Item>().ToTable("items");
        modelBuilder.Entity<ItemCategory>().ToTable("item_categories");
        modelBuilder.Entity<Supplier>().ToTable("suppliers");
        modelBuilder.Entity<Transaction>().ToTable("transactions");
        modelBuilder.Entity<TransactionItem>().ToTable("transaction_items");
        modelBuilder.Entity<Discount>().ToTable("discounts");
        modelBuilder.Entity<Loan>().ToTable("loans");
        modelBuilder.Entity<LoanPayment>().ToTable("loan_payments");
        modelBuilder.Entity<LoanProduct>().ToTable("loan_products");
        modelBuilder.Entity<LoanSetting>().ToTable("loan_settings");
        modelBuilder.Entity<RestockingRecord>().ToTable("restocking_records");
        modelBuilder.Entity<Setting>().ToTable("settings");
        modelBuilder.Entity<AuditLog>().ToTable("audit_logs");
        modelBuilder.Entity<Shu>().ToTable("shu");

        // User -> users table
        modelBuilder.Entity<User>(entity =>
        {
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.Email).HasColumnName("email");
            entity.Property(e => e.Password).HasColumnName("password");
            entity.Property(e => e.Role).HasColumnName("role").HasDefaultValue("admin");
            entity.Property(e => e.Phone).HasColumnName("phone");
            entity.Property(e => e.ShiftLabel).HasColumnName("shift_label");
            entity.Property(e => e.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            entity.Property(e => e.EmailVerifiedAt).HasColumnName("email_verified_at");
            entity.Property(e => e.Avatar).HasColumnName("avatar_path");
            entity.Property(e => e.Locale).HasColumnName("locale").HasDefaultValue("en");
            entity.Property(e => e.RememberToken).HasColumnName("remember_token");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.HasIndex(e => e.Email).IsUnique();
        });

        // Member -> members table
        modelBuilder.Entity<Member>(entity =>
        {
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Identifier).HasColumnName("membership_id");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.Email).HasColumnName("email");
            entity.Property(e => e.Password).HasColumnName("password");
            entity.Property(e => e.Phone).HasColumnName("phone");
            entity.Property(e => e.Address).HasColumnName("address");
            entity.Property(e => e.Avatar).HasColumnName("avatar_path");
            entity.Property(e => e.Locale).HasColumnName("locale").HasDefaultValue("en");
            entity.Property(e => e.JoinDate).HasColumnName("join_date").HasColumnType("date");
            entity.Property(e => e.ShuBalance).HasColumnName("shu_balance").HasColumnType("decimal(12,2)");
            entity.Ignore(e => e.Balance);        // virtual, not a DB column
            entity.Ignore(e => e.LoanBalance);   // virtual, not a DB column
            entity.Property(e => e.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            entity.Property(e => e.RememberToken).HasColumnName("remember_token");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.HasIndex(e => e.Identifier).IsUnique();
        });

        // Item -> items table
        modelBuilder.Entity<Item>(entity =>
        {
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.Code).HasColumnName("sku");
            entity.Property(e => e.Barcode).HasColumnName("barcode");
            entity.Property(e => e.CategoryId).HasColumnName("category_id");
            entity.Property(e => e.Price).HasColumnName("unit_price").HasColumnType("decimal(12,2)");
            entity.Property(e => e.Cost).HasColumnName("cost_price").HasColumnType("decimal(12,2)");
            entity.Property(e => e.Stock).HasColumnName("current_stock");
            entity.Property(e => e.MinStock).HasColumnName("min_stock_threshold");
            entity.Property(e => e.Unit).HasColumnName("unit_of_measure").HasDefaultValue("pcs");
            entity.Property(e => e.ExpiryDate).HasColumnName("expiry_date").HasColumnType("date");
            entity.Property(e => e.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            entity.Ignore(e => e.Description);
            entity.Ignore(e => e.SupplierId);
            entity.Ignore(e => e.Supplier);
            entity.Property(e => e.Image).HasColumnName("image_path");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.HasIndex(e => e.Code).IsUnique();
        });

        // ItemCategory -> item_categories table
        modelBuilder.Entity<ItemCategory>(entity =>
        {
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            // items_count is computed in the service layer, not a column.
            entity.Ignore(e => e.ItemsCount);
        });

        // Supplier -> suppliers table
        modelBuilder.Entity<Supplier>(entity =>
        {
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.ContactPerson).HasColumnName("contact_person");
            entity.Property(e => e.Phone).HasColumnName("phone");
            entity.Property(e => e.Email).HasColumnName("email");
            entity.Property(e => e.Address).HasColumnName("address");
            entity.Property(e => e.Notes).HasColumnName("notes");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        });

        // Transaction -> transactions table
        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.TransactionNumber).HasColumnName("transaction_code");
            entity.Property(e => e.MemberId).HasColumnName("member_id");
            entity.Property(e => e.UserId).HasColumnName("cashier_id");
            entity.Property(e => e.DiscountId).HasColumnName("discount_id");
            entity.Ignore(e => e.Type);
            entity.Property(e => e.Subtotal).HasColumnName("subtotal").HasColumnType("decimal(12,2)");
            entity.Property(e => e.DiscountAmount).HasColumnName("discount_amount").HasColumnType("decimal(12,2)");
            entity.Property(e => e.TaxAmount).HasColumnName("tax_amount").HasColumnType("decimal(12,2)");
            entity.Property(e => e.TotalAmount).HasColumnName("total").HasColumnType("decimal(12,2)");
            entity.Property(e => e.PaymentMethod).HasColumnName("payment_method");
            entity.Property(e => e.PaymentStatus).HasColumnName("payment_status");
            entity.Property(e => e.PaidAt).HasColumnName("paid_at");
            entity.Ignore(e => e.PaidAmount); // not a DB column
            entity.Ignore(e => e.Notes); // not in transactions table
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        });

        // TransactionItem -> transaction_items table
        modelBuilder.Entity<TransactionItem>(entity =>
        {
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.TransactionId).HasColumnName("transaction_id");
            entity.Property(e => e.ItemId).HasColumnName("item_id");
            entity.Property(e => e.Quantity).HasColumnName("quantity");
            entity.Property(e => e.Price).HasColumnName("unit_price").HasColumnType("decimal(12,2)");
            entity.Property(e => e.Subtotal).HasColumnName("subtotal").HasColumnType("decimal(12,2)");
            entity.Ignore(e => e.DiscountAmount); // not in DB
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
        });

        // Discount -> discounts table
        modelBuilder.Entity<Discount>(entity =>
        {
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Ignore(e => e.ItemId);
            entity.Ignore(e => e.Item);
            entity.Ignore(e => e.MemberId);
            entity.Ignore(e => e.Member);
            entity.Property(e => e.DiscountRate).HasColumnName("value").HasColumnType("decimal(12,2)");
            entity.Property(e => e.BuyQty).HasColumnName("buy_qty");
            entity.Property(e => e.GetQty).HasColumnName("get_qty");
            entity.Property(e => e.MinPurchase).HasColumnName("min_purchase").HasColumnType("decimal(12,2)");
            entity.Ignore(e => e.DiscountAmount);
            entity.Property(e => e.Type).HasColumnName("type");
            entity.Property(e => e.StartDate).HasColumnName("starts_at");
            entity.Property(e => e.EndDate).HasColumnName("ends_at");
            entity.Property(e => e.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.Property(e => e.Scope).HasColumnName("scope").HasDefaultValue("general");
            entity.Property(e => e.CategoryId).HasColumnName("category_id");
            entity.HasOne(d => d.Category)
                .WithMany()
                .HasForeignKey(d => d.CategoryId);
        });

        // Loan -> loans table
        modelBuilder.Entity<Loan>(entity =>
        {
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.LoanCode).HasColumnName("loan_code");
            entity.Property(e => e.MemberId).HasColumnName("member_id");
            entity.Property(e => e.LoanProductId).HasColumnName("loan_product_id");
            entity.Property(e => e.UserId).HasColumnName("approved_by");
            entity.Property(e => e.PrincipalAmount).HasColumnName("principal").HasColumnType("decimal(12,2)");
            entity.Property(e => e.InterestAmount).HasColumnName("accrued_interest").HasColumnType("decimal(12,2)");
            entity.Property(e => e.InterestRate).HasColumnName("interest_rate").HasColumnType("decimal(5,4)");
            entity.Property(e => e.InterestPeriod).HasColumnName("interest_period");
            entity.Property(e => e.TermInMonths).HasColumnName("term_months");
            entity.Ignore(e => e.MonthlyPayment);
            entity.Property(e => e.ProcessingFee).HasColumnName("processing_fee").HasColumnType("decimal(12,2)");
            entity.Property(e => e.TotalAmount).HasColumnName("outstanding_balance").HasColumnType("decimal(14,2)");
            entity.Property(e => e.PaidAmount).HasColumnName("total_paid").HasColumnType("decimal(12,2)");
            entity.Ignore(e => e.RemainingAmount);
            entity.Property(e => e.LoanDate).HasColumnName("applied_at");
            entity.Property(e => e.ApprovedAt).HasColumnName("approved_at");
            entity.Property(e => e.FirstDueAt).HasColumnName("first_due_at");
            entity.Property(e => e.DueDate).HasColumnName("due_at");
            entity.Property(e => e.Status).HasColumnName("status").HasDefaultValue("pending");
            entity.Property(e => e.Notes).HasColumnName("notes");
            entity.Property(e => e.ApprovedAt).HasColumnName("approved_at");
            entity.Property(e => e.PaidAt).HasColumnName("paid_off_at");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.HasIndex(e => e.LoanCode).IsUnique();
        });

        // LoanPayment -> loan_payments table
        modelBuilder.Entity<LoanPayment>(entity =>
        {
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.LoanId).HasColumnName("loan_id");
            entity.Property(e => e.UserId).HasColumnName("recorded_by");
            entity.Property(e => e.Amount).HasColumnName("amount").HasColumnType("decimal(12,2)");
            entity.Ignore(e => e.InterestAmount); // not in loan_payments table
            entity.Ignore(e => e.PrincipalAmount); // not in loan_payments table
            entity.Property(e => e.PaymentDate).HasColumnName("paid_at");
            entity.Property(e => e.Notes).HasColumnName("notes");
            entity.Property(e => e.PaymentMethod).HasColumnName("payment_method");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        });

        // LoanProduct -> loan_products table
        modelBuilder.Entity<LoanProduct>(entity =>
        {
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.InterestRate).HasColumnName("interest_rate").HasColumnType("decimal(5,4)");
            entity.Property(e => e.InterestPeriod).HasColumnName("interest_period").HasDefaultValue("monthly");
            entity.Property(e => e.MinTermMonths).HasColumnName("min_term_months").HasDefaultValue(1);
            entity.Property(e => e.MaxTermMonths).HasColumnName("max_term_months").HasDefaultValue(12);
            entity.Property(e => e.MinAmount).HasColumnName("min_amount").HasColumnType("decimal(12,2)");
            entity.Property(e => e.MaxAmount).HasColumnName("max_amount").HasColumnType("decimal(12,2)");
            entity.Property(e => e.ProcessingFeeRate).HasColumnName("processing_fee_rate").HasColumnType("decimal(5,4)");
            entity.Property(e => e.EligibilityRule).HasColumnName("eligibility_rule");
            entity.Property(e => e.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        });

        // LoanSetting -> loan_settings table (real table has `key`/`value`/
        // `group` — there is no `type` column, so map across to `group`
        // instead of querying a non-existent column and 500-ing).
        modelBuilder.Entity<LoanSetting>(entity =>
        {
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Key).HasColumnName("key");
            entity.Property(e => e.Value).HasColumnName("value");
            entity.Property(e => e.Type).HasColumnName("group");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.HasIndex(e => e.Key).IsUnique();
        });

        // RestockingRecord -> restocking_records table
        modelBuilder.Entity<RestockingRecord>(entity =>
        {
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ItemId).HasColumnName("item_id");
            entity.Property(e => e.SupplierId).HasColumnName("supplier_id");
            entity.Property(e => e.UserId).HasColumnName("submitted_by");
            entity.Property(e => e.Quantity).HasColumnName("quantity");
            entity.Property(e => e.CostPerUnit).HasColumnName("cost_per_unit").HasColumnType("decimal(12,2)");
            entity.Property(e => e.TotalCost).HasColumnName("total_cost").HasColumnType("decimal(12,2)");
            entity.Ignore(e => e.TransactionId);
            entity.Property(e => e.Notes).HasColumnName("notes");
            entity.Property(e => e.RestockDate).HasColumnName("restocked_at");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        });

        modelBuilder.Entity<Supplier>()
            .HasMany(s => s.RestockingRecords)
            .WithOne(r => r.Supplier)
            .HasForeignKey(r => r.SupplierId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Transaction>()
            .HasOne(t => t.Discount)
            .WithMany()
            .HasForeignKey(t => t.DiscountId)
            .OnDelete(DeleteBehavior.SetNull);

        // Setting -> settings table
        modelBuilder.Entity<Setting>(entity =>
        {
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Key).HasColumnName("key");
            entity.Property(e => e.Value).HasColumnName("value");
            entity.Property(e => e.Group).HasColumnName("group");
            entity.Property(e => e.IsPublic).HasColumnName("is_public");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.HasIndex(e => e.Key).IsUnique();
        });

        // AuditLog -> audit_logs table
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.UserId).HasColumnName("actor_id");
            entity.Property(e => e.UserType).HasColumnName("actor_type");
            entity.Property(e => e.Action).HasColumnName("action");
            entity.Property(e => e.EntityType).HasColumnName("auditable_type");
            entity.Property(e => e.EntityId).HasColumnName("auditable_id");
            entity.Property(e => e.Details).HasColumnName("changes");
            entity.Property(e => e.IpAddress).HasColumnName("ip_address");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.HasOne(a => a.User)
                .WithMany()
                .HasForeignKey(a => a.UserId);
        });

        // Shu -> shu table
        modelBuilder.Entity<Shu>(entity =>
        {
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.MemberId).HasColumnName("member_id");
            entity.Property(e => e.Amount).HasColumnName("amount").HasColumnType("decimal(12,2)");
            entity.Property(e => e.Year).HasColumnName("year");
            entity.Property(e => e.Type).HasColumnName("type");
            entity.Property(e => e.Notes).HasColumnName("notes");
            entity.Property(e => e.IsDistributed).HasColumnName("is_distributed");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        });

        // Relationship configurations
        modelBuilder.Entity<Transaction>()
            .HasMany(t => t.Items)
            .WithOne(ti => ti.Transaction)
            .HasForeignKey(ti => ti.TransactionId);

        modelBuilder.Entity<Loan>()
            .HasMany(l => l.Payments)
            .WithOne(lp => lp.Loan)
            .HasForeignKey(lp => lp.LoanId);
    }

    // Override to prevent migrations from being used
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING");
                if (string.IsNullOrEmpty(connectionString))
                    throw new InvalidOperationException(
                        "FATAL: DB_CONNECTION_STRING environment variable is not set.");
                optionsBuilder.UseMySQL(connectionString);
            }
        }
}
