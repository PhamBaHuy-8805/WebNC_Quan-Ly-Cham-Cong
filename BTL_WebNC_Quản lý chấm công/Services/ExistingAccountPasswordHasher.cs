using BTL_WebNC_Quản_lý_chấm_công.Models;
using Microsoft.AspNetCore.Identity;

namespace BTL_WebNC_Quản_lý_chấm_công.Services;

// Giữ tương thích dữ liệu thực hành hiện tại và tài khoản dùng Identity của nhóm.
public sealed class ExistingAccountPasswordHasher : PasswordHasher<Account>
{
    public override PasswordVerificationResult VerifyHashedPassword(Account user, string hashedPassword, string providedPassword)
    {
        try
        {
            var bytes = Convert.FromBase64String(hashedPassword);
            if (bytes.Length >= 49 && bytes[0] is 0 or 1)
                return base.VerifyHashedPassword(user, hashedPassword, providedPassword);
        }
        catch (FormatException) { }
        return string.Equals(hashedPassword, providedPassword, StringComparison.Ordinal)
            ? PasswordVerificationResult.Success : PasswordVerificationResult.Failed;
    }
}
