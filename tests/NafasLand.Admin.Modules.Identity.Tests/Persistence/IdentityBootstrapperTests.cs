using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NafasLand.Admin.Modules.Identity.Configuration;
using NafasLand.Admin.Modules.Identity.Contracts;
using NafasLand.Admin.Modules.Identity.Persistence;
using NafasLand.Admin.Modules.Identity.Security;
using NafasLand.Admin.Shared.Kernel.Permissions;

namespace NafasLand.Admin.Modules.Identity.Tests.Persistence;

public sealed class IdentityBootstrapperTests
{
    [Fact]
    public async Task Bootstrap_برچسب_تعریف_شده_در_ماژول_را_ذخیره_می‌کند()
    {
        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var dbContext = new IdentityDbContext(options);
        var bootstrapper = new IdentityBootstrapper(
            dbContext,
            new FakePasswordHasher(),
            TimeProvider.System,
            Options.Create(new SuperAdminSeedOptions
            {
                Username = "bootstrap-test-admin",
                Password = "bootstrap-test-password",
            }));
        var definitions = new[]
        {
            new ModulePermissionDefinition(
                "Identity",
                new PermissionDefinition("identity.users.manage", "مدیریت کاربران")),
        };

        await bootstrapper.BootstrapAsync(definitions, CancellationToken.None);

        var permission = await dbContext.Permissions.SingleAsync();
        Assert.Equal("مدیریت کاربران", permission.DisplayName);
    }

    private sealed class FakePasswordHasher : IPasswordHasher
    {
        public string Algorithm => "test";

        public string Hash(string password) => $"hash:{password.Length}";

        public bool Verify(string password, string encodedHash) => encodedHash == Hash(password);
    }
}
