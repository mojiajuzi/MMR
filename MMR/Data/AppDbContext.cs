using System;
using System.IO;
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