using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Infrastructure.Data;

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer("Server=localhost,1433;Database=TodoFlowDb;User Id=sa;Password=Admin123A@;TrustServerCertificate=True;MultipleActiveResultSets=True")
            .Options;

        return new AppDbContext(options);
    }
}
