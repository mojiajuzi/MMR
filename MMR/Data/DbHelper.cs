using Microsoft.EntityFrameworkCore;

namespace MMR.Data;

public static class DbHelper
{
    private static AppDbContext? _context;
    
    public static AppDbContext Db
    {
        get
        {
            if (_context == null)
            {
                var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
                _context = new AppDbContext(optionsBuilder.Options);
                _context.Database.EnsureCreated();
            }
            return _context;
        }
    }
} 