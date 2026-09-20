using FluentValidation;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NafasLand.Admin.Modules.Identity.Configuration;
using NafasLand.Admin.Modules.Identity.Contracts;
using NafasLand.Admin.Modules.Identity.Features.ChangePassword;
using NafasLand.Admin.Modules.Identity.Features.CreateUser;
using NafasLand.Admin.Modules.Identity.Features.Login;
using NafasLand.Admin.Modules.Identity.Features.Logout;
using NafasLand.Admin.Modules.Identity.Features.Queries;
using NafasLand.Admin.Modules.Identity.Features.ResetPassword;
using NafasLand.Admin.Modules.Identity.Features.SetRolePermissions;
using NafasLand.Admin.Modules.Identity.Features.SetUserPermission;
using NafasLand.Admin.Modules.Identity.Features.SetUserRoles;
using NafasLand.Admin.Modules.Identity.Features.ToggleUserActive;
using NafasLand.Admin.Modules.Identity.Persistence;
using NafasLand.Admin.Modules.Identity.Security;
using NafasLand.Admin.Shared.Infrastructure.Configuration;
using NafasLand.Admin.Shared.Infrastructure.Persistence;
using NafasLand.Admin.Shared.Kernel.Messaging;
using NafasLand.Admin.Shared.Kernel.Modules;
using NafasLand.Admin.Shared.Kernel.Persistence;
using NafasLand.Admin.Shared.Kernel.Permissions;

namespace NafasLand.Admin.Modules.Identity;

/// <summary>Real user/role/permission model replacing step 0's fake user (this step's goal).</summary>
internal sealed class IdentityModule : IModule
{
    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<IdentityDbContext>((serviceProvider, options) =>
        {
            var databaseOptions = serviceProvider.GetRequiredService<IOptions<DatabaseOptions>>().Value;
            options.UseSqlServer(
                databaseOptions.ConnectionString,
                sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", IdentityDbContext.SchemaName));
        });

        services.AddKeyedScoped<IUnitOfWork>(
            "Identity",
            (serviceProvider, _) => serviceProvider.GetRequiredService<IdentityDbContext>());

        services.AddScoped<IMigrationCheck, EfCoreMigrationCheck<IdentityDbContext>>();

        services.AddHealthChecks()
            .AddCheck<EfCoreDatabaseHealthCheck<IdentityDbContext>>("identity-database");

        services.AddOptions<SuperAdminSeedOptions>()
            .Bind(configuration.GetSection(SuperAdminSeedOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddScoped<IPasswordHasher, Argon2PasswordHasher>();
        services.AddScoped<IEffectivePermissionsProvider, EffectivePermissionsProvider>();
        services.AddScoped<IIdentityBootstrapper, IdentityBootstrapper>();

        services.AddScoped<IValidator<LoginCommand>, LoginCommandValidator>();
        services.AddScoped<ICommandHandler<LoginCommand, LoginResult>, LoginCommandHandler>();

        services.AddScoped<ICommandHandler<LogoutCommand, LogoutResult>, LogoutCommandHandler>();

        services.AddScoped<IValidator<ChangePasswordCommand>, ChangePasswordCommandValidator>();
        services.AddScoped<ICommandHandler<ChangePasswordCommand, ChangePasswordResult>, ChangePasswordCommandHandler>();

        services.AddScoped<IValidator<ResetPasswordCommand>, ResetPasswordCommandValidator>();
        services.AddScoped<ICommandHandler<ResetPasswordCommand, ResetPasswordResult>, ResetPasswordCommandHandler>();

        services.AddScoped<IValidator<CreateUserCommand>, CreateUserCommandValidator>();
        services.AddScoped<ICommandHandler<CreateUserCommand, CreateUserResult>, CreateUserCommandHandler>();

        services.AddScoped<IValidator<SetUserRolesCommand>, SetUserRolesCommandValidator>();
        services.AddScoped<ICommandHandler<SetUserRolesCommand, SetUserRolesResult>, SetUserRolesCommandHandler>();

        services.AddScoped<IValidator<SetUserPermissionCommand>, SetUserPermissionCommandValidator>();
        services.AddScoped<ICommandHandler<SetUserPermissionCommand, SetUserPermissionResult>, SetUserPermissionCommandHandler>();

        services.AddScoped<IValidator<SetRolePermissionsCommand>, SetRolePermissionsCommandValidator>();
        services.AddScoped<ICommandHandler<SetRolePermissionsCommand, SetRolePermissionsResult>, SetRolePermissionsCommandHandler>();

        services.AddScoped<ICommandHandler<ToggleUserActiveCommand, ToggleUserActiveResult>, ToggleUserActiveCommandHandler>();
    }

    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        LoginEndpoint.Map(app);
        LogoutEndpoint.Map(app);
        ChangePasswordEndpoint.Map(app);
        ResetPasswordEndpoint.Map(app);
        CreateUserEndpoint.Map(app);
        SetUserRolesEndpoint.Map(app);
        SetUserPermissionEndpoint.Map(app);
        SetRolePermissionsEndpoint.Map(app);
        ToggleUserActiveEndpoint.Map(app);

        GetMeEndpoint.Map(app);
        ListUsersEndpoint.Map(app);
        GetUserEndpoint.Map(app);
        ListPermissionsEndpoint.Map(app);
        ListRolesEndpoint.Map(app);
        ListRoleSummariesEndpoint.Map(app);
    }

    public IReadOnlyList<PermissionDefinition> Permissions { get; } =
    [
        new PermissionDefinition(IdentityPermissions.UsersManage, "مدیریت کاربران"),
        new PermissionDefinition(IdentityPermissions.AccessManage, "مدیریت دسترسی‌ها", IsSuperAdminOnly: true),
    ];
}
