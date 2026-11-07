using System.Globalization;
using BTL_WebNC_Quản_lý_chấm_công.Data;
using BTL_WebNC_Quản_lý_chấm_công.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BTL_WebNC_Quản_lý_chấm_công.Services;


public sealed class ExistingAccountUserStore(ApplicationDbContext db)
    : IUserPasswordStore<Account>
{
    public void Dispose() { }

    public Task<string> GetUserIdAsync(Account user, CancellationToken ct) =>
        Task.FromResult(user.UserID.ToString(CultureInfo.InvariantCulture));

    public Task<string?> GetUserNameAsync(Account user, CancellationToken ct) =>
        Task.FromResult<string?>(user.Username);

    public Task SetUserNameAsync(Account user, string? userName, CancellationToken ct)
    {
        user.Username = userName ?? string.Empty;
        return Task.CompletedTask;
    }

    public Task<string?> GetNormalizedUserNameAsync(Account user, CancellationToken ct) =>
        Task.FromResult<string?>(user.Username.ToUpperInvariant());

    // Accounts hiện không có cột NormalizedUserName.
    public Task SetNormalizedUserNameAsync(Account user, string? normalizedName, CancellationToken ct)
        => Task.CompletedTask;

    public async Task<Account?> FindByIdAsync(string userId, CancellationToken ct)
    {
        if (!int.TryParse(userId, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id))
            return null;
        return await db.Accounts.Include(a => a.Role).Include(a => a.Employee)
            .FirstOrDefaultAsync(a => a.UserID == id, ct);
    }

    public Task<Account?> FindByNameAsync(string normalizedUserName, CancellationToken ct) =>
        db.Accounts.Include(a => a.Role).Include(a => a.Employee)
            .FirstOrDefaultAsync(a => a.Username.ToUpper() == normalizedUserName, ct);

    public Task<string?> GetPasswordHashAsync(Account user, CancellationToken ct) =>
        Task.FromResult<string?>(user.PasswordHash);

    public Task<bool> HasPasswordAsync(Account user, CancellationToken ct) =>
        Task.FromResult(!string.IsNullOrEmpty(user.PasswordHash));

    public Task SetPasswordHashAsync(Account user, string? passwordHash, CancellationToken ct)
    {
        user.PasswordHash = passwordHash ?? string.Empty;
        return Task.CompletedTask;
    }

    public Task<IdentityResult> CreateAsync(Account user, CancellationToken ct) =>
        Task.FromResult(IdentityResult.Failed(new IdentityError {
            Code = "UnsupportedOperation",
            Description = "Tạo tài khoản bằng Identity UserManager chưa được triển khai."
        }));

    public Task<IdentityResult> DeleteAsync(Account user, CancellationToken ct) =>
        Task.FromResult(IdentityResult.Failed(new IdentityError {
            Code = "UnsupportedOperation",
            Description = "Xóa tài khoản bằng Identity UserManager chưa được triển khai."
        }));

    // Cho phép Identity nâng cấp hash nếu thuật toán/chi phí hiện tại đã cũ.
    // Chỉ cập nhật PasswordHash, không ghi đè Role/Employee/Status.
    public async Task<IdentityResult> UpdateAsync(Account user, CancellationToken ct)
    {
        var account = await db.Accounts.FindAsync(new object[] { user.UserID }, ct);
        if (account is null)
            return IdentityResult.Failed(new IdentityError { Code = "NotFound", Description = "Không tìm thấy tài khoản." });

        account.PasswordHash = user.PasswordHash;
        await db.SaveChangesAsync(ct);
        return IdentityResult.Success;
    }
}
