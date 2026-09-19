using NafasLand.Admin.Shared.Kernel.Errors;

namespace NafasLand.Admin.Modules.Identity.Persistence;

internal sealed class AppUser
{
    private readonly List<UserRole> _userRoles = [];
    private readonly List<UserPermission> _userPermissions = [];

    private AppUser()
    {
        Username = string.Empty;
        PasswordHash = string.Empty;
        PasswordAlgorithm = string.Empty;
    }

    public Guid Id { get; private set; }

    public string Username { get; private set; }

    public string PasswordHash { get; private set; }

    public string PasswordAlgorithm { get; private set; }

    public bool MustChangePassword { get; private set; }

    public bool IsActive { get; private set; }

    public bool IsProtected { get; private set; }

    public int FailedLoginCount { get; private set; }

    public DateTimeOffset? LockedUntil { get; private set; }

    public bool TwoFactorEnabled { get; private set; }

    public string? TwoFactorSecret { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public Guid? CreatedByUserId { get; private set; }

    public IReadOnlyCollection<UserRole> UserRoles => _userRoles;

    public IReadOnlyCollection<UserPermission> UserPermissions => _userPermissions;

    public static AppUser Create(
        string username,
        string passwordHash,
        string passwordAlgorithm,
        DateTimeOffset now,
        bool mustChangePassword,
        bool isProtected,
        Guid? createdByUserId)
    {
        return new AppUser
        {
            Id = Guid.NewGuid(),
            Username = username,
            PasswordHash = passwordHash,
            PasswordAlgorithm = passwordAlgorithm,
            IsActive = true,
            IsProtected = isProtected,
            MustChangePassword = mustChangePassword,
            CreatedAt = now,
            CreatedByUserId = createdByUserId,
        };
    }

    public bool IsLocked(DateTimeOffset now) => LockedUntil is not null && LockedUntil > now;

    /// <summary>Does not increment the counter — a locked-out or inactive-account attempt never does (ADR-023).</summary>
    public void RegisterFailedLogin(DateTimeOffset now, int maxFailedAttempts, TimeSpan lockDuration)
    {
        FailedLoginCount++;
        if (FailedLoginCount >= maxFailedAttempts)
        {
            LockedUntil = now.Add(lockDuration);
        }
    }

    public void RegisterSuccessfulLogin()
    {
        FailedLoginCount = 0;
        LockedUntil = null;
    }

    public void SetPassword(string passwordHash, string passwordAlgorithm, bool mustChangePassword)
    {
        PasswordHash = passwordHash;
        PasswordAlgorithm = passwordAlgorithm;
        MustChangePassword = mustChangePassword;
    }

    public void SetActive(bool isActive) => IsActive = isActive;

    /// <summary>ADR-022: no deactivate/demote/delete may ever touch the protected seed account, even from another SuperAdmin.</summary>
    public void EnsureNotProtected(string action)
    {
        if (IsProtected)
        {
            throw new AuthorizationDeniedException($"این حساب محافظت‌شده است؛ {action} روی آن مجاز نیست.");
        }
    }
}
