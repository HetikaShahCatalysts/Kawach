using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Api.Data;

public sealed class KawachDbContextFactory : IDesignTimeDbContextFactory<KawachDbContext>
{
    public KawachDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<KawachDbContext>()
            .UseSqlServer(
                "Server=localhost;Database=Kawach;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True")
            .Options;

        return new KawachDbContext(options);
    }
}
