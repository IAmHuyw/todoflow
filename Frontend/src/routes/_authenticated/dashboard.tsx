import { Link, createFileRoute } from "@tanstack/react-router";
import { format, isBefore, isToday, startOfDay } from "date-fns";
import { vi } from "date-fns/locale";
import {
  AlertTriangle,
  ArrowRight,
  CalendarDays,
  CheckCircle2,
  CircleDashed,
  Clock3,
  ListTodo,
  Plus,
  RefreshCw,
  TrendingUp,
} from "lucide-react";
import { useCallback, useEffect, useMemo, useState } from "react";
import { Bar, BarChart, Cell, Pie, PieChart, ResponsiveContainer, Tooltip, XAxis, YAxis } from "recharts";
import { toast } from "sonner";
import { TaskDialog } from "@/components/task/TaskDialog";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Checkbox } from "@/components/ui/checkbox";
import { cn } from "@/lib/utils";
import { useCurrentUser, useTodoStore } from "@/lib/todo-store";
import type { DashboardSummary, DashboardTaskSummary, Priority, Status } from "@/lib/todo-types";

export const Route = createFileRoute("/_authenticated/dashboard")({
  component: Dashboard,
  head: () => ({ meta: [{ title: "Tổng quan — TodoFlow" }] }),
});

const statusLabels: Record<Status, string> = {
  todo: "Cần làm",
  in_progress: "Đang làm",
  done: "Hoàn thành",
};

const priorityLabels: Record<Priority, string> = {
  low: "Thấp",
  medium: "Trung bình",
  high: "Cao",
};

const priorityClasses: Record<Priority, string> = {
  low: "border-slate-200 bg-slate-50 text-slate-700",
  medium: "border-amber-200 bg-amber-50 text-amber-800",
  high: "border-red-200 bg-red-50 text-red-700",
};

function Dashboard() {
  const currentUser = useCurrentUser();
  const loadDashboardSummary = useTodoStore((state) => state.loadDashboardSummary);
  const setTaskStatus = useTodoStore((state) => state.setTaskStatus);
  const [summary, setSummary] = useState<DashboardSummary | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [creating, setCreating] = useState(false);
  const [completingId, setCompletingId] = useState<string | null>(null);

  const refreshSummary = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      setSummary(await loadDashboardSummary());
    } catch (requestError) {
      setError(requestError instanceof Error ? requestError.message : "Không tải được tổng quan.");
    } finally {
      setLoading(false);
    }
  }, [loadDashboardSummary]);

  useEffect(() => {
    void refreshSummary();
  }, [refreshSummary]);

  const completeTask = async (taskId: string) => {
    setCompletingId(taskId);
    try {
      await setTaskStatus(taskId, "done");
      await refreshSummary();
      toast.success("Đã hoàn thành công việc");
    } catch (requestError) {
      toast.error(requestError instanceof Error ? requestError.message : "Không thể cập nhật công việc");
    } finally {
      setCompletingId(null);
    }
  };

  const handleCreateDialog = (open: boolean) => {
    setCreating(open);
    if (!open) void refreshSummary();
  };

  if (loading && !summary) return <DashboardLoading />;
  if (!summary) return <DashboardError message={error} onRetry={refreshSummary} />;

  const displayName = currentUser?.fullName || currentUser?.username || "bạn";
  const createdToday = summary.createdTaskTrend.at(-1)?.count ?? 0;
  const completionRate = summary.totalTaskCount
    ? Math.round((summary.doneCount / summary.totalTaskCount) * 100)
    : 0;
  const statusData = [
    { name: "Cần làm", value: summary.todoCount, color: "#f59e0b" },
    { name: "Đang làm", value: summary.inProgressCount, color: "#38bdf8" },
    { name: "Hoàn thành", value: summary.doneCount, color: "#22c55e" },
  ];
  const metricCards = [
    {
      label: "Cần làm",
      value: summary.todoCount,
      helper: summary.dueTodayCount > 0 ? `${summary.dueTodayCount} đến hạn hôm nay` : "Chưa có hạn hôm nay",
      icon: ListTodo,
      className: "border-blue-200 bg-blue-50/70",
      iconClassName: "bg-blue-100 text-blue-700",
    },
    {
      label: "Đang làm",
      value: summary.inProgressCount,
      helper: "Công việc bạn đang tập trung",
      icon: Clock3,
      className: "border-amber-200 bg-amber-50/70",
      iconClassName: "bg-amber-100 text-amber-700",
    },
    {
      label: "Hoàn thành",
      value: summary.doneCount,
      helper: `${completionRate}% tổng công việc`,
      icon: CheckCircle2,
      className: "border-emerald-200 bg-emerald-50/70",
      iconClassName: "bg-emerald-100 text-emerald-700",
    },
    {
      label: "Quá hạn",
      value: summary.overdueCount,
      helper: summary.overdueCount > 0 ? "Cần xử lý sớm" : "Mọi việc đang đúng hạn",
      icon: AlertTriangle,
      className: "border-rose-200 bg-rose-50/70",
      iconClassName: "bg-rose-100 text-rose-700",
    },
  ];

  return (
    <div className="mx-auto w-full max-w-7xl p-4 sm:p-6 lg:p-8">
      <header className="mb-7 flex flex-col gap-4 sm:flex-row sm:items-end sm:justify-between">
        <div>
          <p className="mb-2 flex items-center gap-2 text-sm text-muted-foreground">
            <CalendarDays className="h-4 w-4" />
            {format(new Date(), "EEEE, dd 'tháng' MM, yyyy", { locale: vi })}
          </p>
          <h1 className="text-2xl font-semibold sm:text-3xl">{getGreeting()}, {displayName}</h1>
          <p className="mt-1 text-sm text-muted-foreground">
            {summary.dueTodayCount > 0
              ? `Bạn có ${summary.dueTodayCount} công việc đến hạn hôm nay.`
              : "Hôm nay chưa có công việc nào đến hạn."}
          </p>
        </div>
        <div className="flex gap-2">
          <Button variant="outline" onClick={() => void refreshSummary()} disabled={loading}>
            <RefreshCw className={cn("mr-2 h-4 w-4", loading && "animate-spin")} /> Làm mới
          </Button>
          <Button onClick={() => setCreating(true)}>
            <Plus className="mr-2 h-4 w-4" /> Công việc mới
          </Button>
        </div>
      </header>

      {error && (
        <div className="mb-5 flex items-center justify-between gap-3 rounded-lg border border-rose-200 bg-rose-50 px-4 py-3 text-sm text-rose-800">
          <span>{error}</span>
          <Button variant="outline" size="sm" onClick={() => void refreshSummary()}>
            Thử lại
          </Button>
        </div>
      )}

      <section className="mb-6 grid gap-3 sm:grid-cols-2 xl:grid-cols-4">
        {metricCards.map((metric) => {
          const Icon = metric.icon;
          return (
            <div key={metric.label} className={cn("rounded-lg border p-4", metric.className)}>
              <div className="flex items-start justify-between gap-3">
                <div>
                  <p className="text-sm font-medium text-muted-foreground">{metric.label}</p>
                  <p className="mt-2 text-3xl font-semibold tabular-nums">{metric.value}</p>
                </div>
                <span className={cn("flex h-9 w-9 items-center justify-center rounded-lg", metric.iconClassName)}>
                  <Icon className="h-4 w-4" />
                </span>
              </div>
              <p className="mt-3 text-xs text-muted-foreground">{metric.helper}</p>
            </div>
          );
        })}
      </section>

      <section className="mb-6 grid gap-6 xl:grid-cols-[minmax(0,1.65fr)_minmax(300px,0.85fr)]">
        <div className="rounded-lg border border-border bg-card p-5">
          <div className="mb-5 flex items-start justify-between gap-3">
            <div>
              <h2 className="font-semibold">Công việc 7 ngày qua</h2>
              <p className="mt-1 text-sm text-muted-foreground">
                {createdToday} công việc được tạo hôm nay
              </p>
            </div>
            <TrendingUp className="h-5 w-5 text-muted-foreground" />
          </div>
          <div className="h-64">
            <ResponsiveContainer width="100%" height="100%">
              <BarChart data={summary.createdTaskTrend} margin={{ top: 8, right: 4, left: -20, bottom: 0 }}>
                <XAxis
                  dataKey="date"
                  axisLine={false}
                  tickLine={false}
                  tick={{ fontSize: 12, fill: "hsl(var(--muted-foreground))" }}
                  tickFormatter={formatTrendDay}
                />
                <YAxis
                  allowDecimals={false}
                  axisLine={false}
                  tickLine={false}
                  tick={{ fontSize: 12, fill: "hsl(var(--muted-foreground))" }}
                />
                <Tooltip
                  cursor={{ fill: "hsl(var(--muted) / 0.55)" }}
                  contentStyle={{ borderRadius: 8, borderColor: "hsl(var(--border))" }}
                  labelFormatter={(value) => formatTrendDate(String(value))}
                  formatter={(value) => [value, "Công việc"]}
                />
                <Bar dataKey="count" fill="#60a5fa" radius={[4, 4, 0, 0]} maxBarSize={32} />
              </BarChart>
            </ResponsiveContainer>
          </div>
        </div>

        <div className="rounded-lg border border-border bg-card p-5">
          <div className="mb-4">
            <h2 className="font-semibold">Phân bổ trạng thái</h2>
            <p className="mt-1 text-sm text-muted-foreground">Toàn bộ công việc của bạn</p>
          </div>
          <div className="relative h-48">
            <ResponsiveContainer width="100%" height="100%">
              <PieChart>
                <Pie
                  data={statusData}
                  dataKey="value"
                  nameKey="name"
                  innerRadius={56}
                  outerRadius={80}
                  paddingAngle={3}
                  stroke="transparent"
                >
                  {statusData.map((item) => (
                    <Cell key={item.name} fill={item.color} />
                  ))}
                </Pie>
                <Tooltip
                  contentStyle={{ borderRadius: 8, borderColor: "hsl(var(--border))" }}
                  formatter={(value) => [value, "Công việc"]}
                />
              </PieChart>
            </ResponsiveContainer>
            <div className="pointer-events-none absolute inset-0 flex flex-col items-center justify-center">
              <span className="text-2xl font-semibold tabular-nums">{summary.totalTaskCount}</span>
              <span className="text-xs text-muted-foreground">Tổng việc</span>
            </div>
          </div>
          <div className="mt-2 grid grid-cols-3 gap-2 text-center text-xs">
            {statusData.map((item) => (
              <div key={item.name}>
                <span className="mx-auto mb-1 block h-2 w-2 rounded-full" style={{ backgroundColor: item.color }} />
                <span className="block text-muted-foreground">{item.name}</span>
                <span className="font-medium tabular-nums">{item.value}</span>
              </div>
            ))}
          </div>
        </div>
      </section>

      <section className="grid gap-6 xl:grid-cols-[minmax(0,1.4fr)_minmax(300px,0.8fr)]">
        <div className="rounded-lg border border-border bg-card">
          <div className="flex items-center justify-between border-b border-border px-5 py-4">
            <div>
              <h2 className="font-semibold">Việc cần xử lý</h2>
              <p className="mt-1 text-sm text-muted-foreground">Quá hạn và đến hạn hôm nay</p>
            </div>
            <Button variant="ghost" size="sm" asChild>
              <Link to="/tasks">
                Xem bảng việc <ArrowRight className="ml-1.5 h-4 w-4" />
              </Link>
            </Button>
          </div>
          {summary.todayTasks.length === 0 ? (
            <EmptyTaskState text="Không có việc nào cần xử lý hôm nay." />
          ) : (
            <div className="divide-y divide-border">
              {summary.todayTasks.map((task) => (
                <DashboardTaskRow
                  key={task.id}
                  task={task}
                  completing={completingId === task.id}
                  onComplete={completeTask}
                />
              ))}
            </div>
          )}
        </div>

        <div className="rounded-lg border border-border bg-card">
          <div className="flex items-center justify-between border-b border-border px-5 py-4">
            <div>
              <h2 className="font-semibold">Sắp tới</h2>
              <p className="mt-1 text-sm text-muted-foreground">Trong 7 ngày tiếp theo</p>
            </div>
            <CalendarDays className="h-5 w-5 text-muted-foreground" />
          </div>
          {summary.upcomingTasks.length === 0 ? (
            <EmptyTaskState text="Chưa có công việc sắp tới." />
          ) : (
            <div className="divide-y divide-border">
              {summary.upcomingTasks.map((task) => (
                <DashboardTaskRow
                  key={task.id}
                  task={task}
                  completing={completingId === task.id}
                  onComplete={completeTask}
                />
              ))}
            </div>
          )}
        </div>
      </section>

      <section className="mt-6 rounded-lg border border-border bg-card">
        <div className="flex items-center justify-between border-b border-border px-5 py-4">
          <div>
            <h2 className="font-semibold">Tiến độ theo danh mục</h2>
            <p className="mt-1 text-sm text-muted-foreground">Các danh mục có công việc</p>
          </div>
          <Button variant="ghost" size="sm" asChild>
            <Link to="/categories">
              Danh mục <ArrowRight className="ml-1.5 h-4 w-4" />
            </Link>
          </Button>
        </div>
        {summary.categories.length === 0 ? (
          <div className="px-5 py-10 text-center text-sm text-muted-foreground">
            Chưa có danh mục nào chứa công việc.
          </div>
        ) : (
          <div className="grid divide-y divide-border md:grid-cols-2 md:divide-x md:divide-y-0">
            {summary.categories.map((category) => {
              const completed = category.totalTaskCount - category.openTaskCount;
              const percentage = Math.round((completed / category.totalTaskCount) * 100);
              return (
                <div key={category.id} className="p-5">
                  <div className="mb-3 flex items-center justify-between gap-3">
                    <div className="flex min-w-0 items-center gap-2">
                      <span className="h-2.5 w-2.5 shrink-0 rounded-full" style={{ backgroundColor: category.color }} />
                      <span className="truncate text-sm font-medium">{category.name}</span>
                    </div>
                    <span className="shrink-0 text-xs text-muted-foreground">{category.openTaskCount} đang mở</span>
                  </div>
                  <div className="h-2 overflow-hidden rounded-full bg-muted">
                    <div
                      className="h-full rounded-full transition-[width]"
                      style={{ width: `${percentage}%`, backgroundColor: category.color }}
                    />
                  </div>
                  <p className="mt-2 text-xs text-muted-foreground">
                    {completed}/{category.totalTaskCount} hoàn thành · {percentage}%
                  </p>
                </div>
              );
            })}
          </div>
        )}
      </section>

      <TaskDialog open={creating} onOpenChange={handleCreateDialog} />
    </div>
  );
}

function DashboardTaskRow({
  task,
  completing,
  onComplete,
}: {
  task: DashboardTaskSummary;
  completing: boolean;
  onComplete: (taskId: string) => Promise<void>;
}) {
  const dueDate = task.dueDate ? new Date(task.dueDate) : null;
  const dueToday = dueDate ? isToday(dueDate) : false;
  const overdue = dueDate ? isBefore(startOfDay(dueDate), startOfDay(new Date())) : false;

  return (
    <div className="flex items-start gap-3 px-5 py-4">
      <Checkbox
        checked={false}
        disabled={completing}
        onCheckedChange={(checked) => {
          if (checked === true) void onComplete(task.id);
        }}
        aria-label={`Hoàn thành ${task.title}`}
        className="mt-0.5"
      />
      <div className="min-w-0 flex-1">
        <Link to="/tasks" className="block truncate text-sm font-medium hover:text-primary">
          {task.title}
        </Link>
        <div className="mt-2 flex flex-wrap items-center gap-1.5">
          <Badge variant="outline" className="text-xs font-normal">
            {statusLabels[task.status]}
          </Badge>
          <span className={cn("rounded-md border px-1.5 py-0.5 text-xs", priorityClasses[task.priority])}>
            {priorityLabels[task.priority]}
          </span>
          {task.categoryName && (
            <span className="inline-flex items-center gap-1 text-xs text-muted-foreground">
              <span className="h-1.5 w-1.5 rounded-full" style={{ backgroundColor: task.categoryColor ?? "#94a3b8" }} />
              {task.categoryName}
            </span>
          )}
        </div>
      </div>
      {dueDate && (
        <span
          className={cn(
            "shrink-0 text-xs",
            overdue ? "font-medium text-rose-600" : dueToday ? "font-medium text-blue-600" : "text-muted-foreground",
          )}
        >
          {overdue ? "Quá hạn" : dueToday ? "Hôm nay" : format(dueDate, "dd/MM", { locale: vi })}
        </span>
      )}
    </div>
  );
}

function EmptyTaskState({ text }: { text: string }) {
  return (
    <div className="flex min-h-44 flex-col items-center justify-center px-5 text-center">
      <CheckCircle2 className="mb-3 h-7 w-7 text-emerald-500" />
      <p className="text-sm text-muted-foreground">{text}</p>
    </div>
  );
}

function DashboardLoading() {
  return (
    <div className="mx-auto w-full max-w-7xl p-4 sm:p-6 lg:p-8">
      <div className="h-6 w-56 animate-pulse rounded bg-muted" />
      <div className="mt-3 h-4 w-80 max-w-full animate-pulse rounded bg-muted" />
      <div className="mt-7 grid gap-3 sm:grid-cols-2 xl:grid-cols-4">
        {Array.from({ length: 4 }, (_, index) => (
          <div key={index} className="h-36 animate-pulse rounded-lg border border-border bg-muted/40" />
        ))}
      </div>
      <div className="mt-6 grid gap-6 xl:grid-cols-[minmax(0,1.65fr)_minmax(300px,0.85fr)]">
        <div className="h-80 animate-pulse rounded-lg border border-border bg-muted/40" />
        <div className="h-80 animate-pulse rounded-lg border border-border bg-muted/40" />
      </div>
    </div>
  );
}

function DashboardError({ message, onRetry }: { message: string | null; onRetry: () => Promise<void> }) {
  return (
    <div className="mx-auto flex min-h-[60vh] max-w-lg flex-col items-center justify-center p-6 text-center">
      <CircleDashed className="mb-4 h-9 w-9 text-muted-foreground" />
      <h1 className="text-lg font-semibold">Không tải được tổng quan</h1>
      <p className="mt-2 text-sm text-muted-foreground">{message ?? "Vui lòng thử lại sau."}</p>
      <Button className="mt-5" onClick={() => void onRetry()}>
        Thử lại
      </Button>
    </div>
  );
}

function getGreeting() {
  const hour = new Date().getHours();
  if (hour < 11) return "Chào buổi sáng";
  if (hour < 18) return "Chào buổi chiều";
  return "Chào buổi tối";
}

function formatTrendDay(value: string) {
  const date = parseDateOnly(value);
  return format(date, "EEE", { locale: vi });
}

function formatTrendDate(value: string) {
  const date = parseDateOnly(value);
  return format(date, "EEEE, dd/MM", { locale: vi });
}

function parseDateOnly(value: string) {
  const [year, month, day] = value.split("-").map(Number);
  return new Date(year, month - 1, day);
}
