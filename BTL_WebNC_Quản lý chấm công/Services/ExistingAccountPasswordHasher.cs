using System.Security.Cryptography;
using System.Text;
using BTL_WebNC_Quản_lý_chấm_công.Models;
using Microsoft.AspNetCore.Identity;

namespace BTL_WebNC_Quản_lý_chấm_công.Services;

// Tương thích dữ liệu cũ: mật khẩu thường chỉ được chấp nhận một lần và được
// UserManager tự động nâng cấp thành hash Identity sau khi đăng nhập thành công.
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

        if (!string.IsNullOrEmpty(hashedPassword) && CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(hashedPassword), Encoding.UTF8.GetBytes(providedPassword)))
            return PasswordVerificationResult.SuccessRehashNeeded;

        return PasswordVerificationResult.Failed;
    }
}
