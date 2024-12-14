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