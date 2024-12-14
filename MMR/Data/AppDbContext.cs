using System;
using System.IO;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using MMR.Models;

namespace MMR.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            var folder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var dbPath = Path.Combine(folder, "MMR", "mmr.db");
            Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);

            optionsBuilder.UseSqlite($"Data Source={dbPath}");
        }
    }

    public DbSet<Tag> Tags { get; set; } = null!;
    public DbSet<Contact> Contacts { get; set; } = null!;
    public DbSet<ContactTag> ContactTags { get; set; } = null!;
    public DbSet<Work> Works { get; set; }
    public DbSet<Expense> Expenses { get; set; }
    public DbSet<WorkContact> WorkContacts { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 配置所有继承自BaseModel的实体
        foreach (var entityType in modelBuilder.Model.GetEntityTypes()
                     .Where(e => e.ClrType.BaseType == typeof(BaseModel)))
        {
            modelBuilder.Entity(entityType.ClrType)
                .Property("Id")
                .ValueGeneratedOnAdd();
        }

        // ContactTag的配置
        modelBuilder.Entity<ContactTag>(entity =>
        {
            // 设置复合主键
            entity.HasKey(ct => new { ct.ContactId, ct.TagId });

            // 配置与Contact的关系
            entity.HasOne(ct => ct.Contact)
                .WithMany(c => c.ContactTags)
                .HasForeignKey(ct => ct.ContactId)
                .OnDelete(DeleteBehavior.Cascade);

            // 配置与Tag的关系
            entity.HasOne(ct => ct.Tag)
                .WithMany(t => t.ContactTags)
                .HasForeignKey(ct => ct.TagId)
                .OnDelete(DeleteBehavior.Cascade);

            // 配置时间戳字段
            entity.Property(ct => ct.CreatedAt)
                .IsRequired();
            entity.Property(ct => ct.UpdatedAt)
                .IsRequired();
        });

        // 配置 WorkContact 多对多关系
        modelBuilder.Entity<WorkContact>(entity =>
        {
            // 设置复合主键
            entity.HasKey(wc => new { wc.WorkId, wc.ContactId });

            // 配置与 Work 的关系
            entity.HasOne(wc => wc.Work)
                .WithMany(w => w.WorkContacts)
                .HasForeignKey(wc => wc.WorkId)
                .OnDelete(DeleteBehavior.Restrict);

            // 配置与 Contact 的关系
            entity.HasOne(wc => wc.Contact)
                .WithMany(c => c.WorkContacts)
                .HasForeignKey(wc => wc.ContactId)
                .OnDelete(DeleteBehavior.Restrict);

            // 创建索引
            entity.HasIndex(wc => wc.WorkId);
            entity.HasIndex(wc => wc.ContactId);
        });

        // 配置 Expense 关系
        modelBuilder.Entity<Expense>(entity =>
        {
            // 配置与 Work 的关系
            entity.HasOne(e => e.Work)
                .WithMany(w => w.Expenses)
                .HasForeignKey(e => e.WorkId)
                .OnDelete(DeleteBehavior.Restrict);

            // 配置与 Contact 的关系
            entity.HasOne(e => e.Contact)
                .WithMany(c => c.Expenses)
                .HasForeignKey(e => e.ContactId)
                .OnDelete(DeleteBehavior.Restrict);

            // 创建索引
            entity.HasIndex(e => e.WorkId);
            entity.HasIndex(e => e.ContactId);
            entity.HasIndex(e => e.Date);
        });

        // 配置 Work
        modelBuilder.Entity<Work>(entity =>
        {
            entity.Property(w => w.TotalMoney)
                .HasPrecision(18, 2);  // 设置金额精度
        });
    }
}

// 添加工厂类以支持设计时创建 DbContext
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        return new AppDbContext(optionsBuilder.Options);
    }
}