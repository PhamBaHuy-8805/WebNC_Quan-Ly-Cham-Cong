namespace BTL_WebNC_Quản_lý_chấm_công.ViewModels;

public class HomeVisitViewModel
{
    public long VisitCount { get; init; }
    public DateTimeOffset? PreviousVisit { get; init; }
    public DateTimeOffset CurrentVisit { get; init; }
    public HomeProfileSummaryViewModel? Profile { get; init; }
}

public class HomeProfileSummaryViewModel
{
    public int EmployeeID { get; init; }
    public string EmployeeCode { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string? Phone { get; init; }
    public string? Email { get; init; }
    public string? DepartmentName { get; init; }
    public string? PositionName { get; init; }
    public bool HasPendingProfileRequest { get; init; }
}
