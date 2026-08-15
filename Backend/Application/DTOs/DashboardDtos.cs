using Domain.Enums;

namespace Application.DTOs;

public record DashboardSummaryDto(
    int TotalTaskCount,
    int TodoCount,
    int InProgressCount,
    int DoneCount,
    int OverdueCount,
    int DueTodayCount,
    IReadOnlyList<DashboardTaskSummaryDto> TodayTasks,
    IReadOnlyList<DashboardTaskSummaryDto> UpcomingTasks,
    IReadOnlyList<DashboardTrendPointDto> CreatedTaskTrend,
    IReadOnlyList<DashboardCategorySummaryDto> Categories);

public record DashboardTaskSummaryDto(
    Guid Id,
    string Title,
    Priority Priority,
    TodoStatus Status,
    DateTime? DueDate,
    string? CategoryName,
    string? CategoryColor);

public record DashboardTrendPointDto(DateOnly Date, int Count);

public record DashboardCategorySummaryDto(
    Guid Id,
    string Name,
    string Color,
    int TotalTaskCount,
    int OpenTaskCount);
