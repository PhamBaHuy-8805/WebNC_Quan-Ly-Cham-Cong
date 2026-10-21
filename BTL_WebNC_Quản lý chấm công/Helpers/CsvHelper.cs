using System.Text;

namespace BTL_WebNC_Quản_lý_chấm_công.Helpers;

public static class CsvHelper
{
    // Chặn CSV injection: ô bắt đầu bằng = + - @ sẽ bị Excel hiểu là công thức.
    public static string Escape(string? value)
    {
        value ??= string.Empty;
        if (value.Length > 0 && value[0] is '=' or '+' or '-' or '@' or '\t' or '\r')
            value = "'" + value;
        return value.IndexOfAny(new[] { ',', '"', '\n', '\r' }) >= 0
            ? "\"" + value.Replace("\"", "\"\"") + "\""
            : value;
    }

    // UTF-8 có BOM để Excel đọc đúng tiếng Việt.
    public static byte[] Build(IEnumerable<string> header, IEnumerable<IEnumerable<string>> rows)
    {
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(",", header.Select(Escape)));
        foreach (var row in rows) sb.AppendLine(string.Join(",", row.Select(Escape)));
        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
    }
}
