
using Microsoft.AspNetCore.Identity;

namespace BTL_WebNC_Quản_lý_chấm_công.Helpers
{
    public static class PasswordHelper
    {
        private static readonly PasswordHasher<object> Hasher = new();

        // Mã hóa mật khẩu để lưu vào database
        public static string Hash(string password)
        {
            return Hasher.HashPassword(new object(), password);
        }

        // Kiểm tra mật khẩu khi đăng nhập
        public static bool Verify(string password, string passwordHash)
        {
            var result = Hasher.VerifyHashedPassword(
                new object(),
                passwordHash,
                password
            );

            return result != PasswordVerificationResult.Failed;
        }
    }
}
