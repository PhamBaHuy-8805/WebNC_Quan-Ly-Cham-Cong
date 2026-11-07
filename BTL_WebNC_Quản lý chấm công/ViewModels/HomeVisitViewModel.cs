namespace BTL_WebNC_Quản_lý_chấm_công.ViewModels;

public class HomeVisitViewModel
{
    public long VisitCount { get; init; }
    public DateTimeOffset? PreviousVisit { get; init; }
    public DateTimeOffset CurrentVisit { get; init; }
}
