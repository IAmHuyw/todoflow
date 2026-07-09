import { createFileRoute } from "@tanstack/react-router";
import { useEffect, useMemo, useState } from "react";
import { Plus, Search } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { useTodoStore } from "@/lib/todo-store";
import { TaskCard } from "@/components/task/TaskCard";
import { TaskDialog } from "@/components/task/TaskDialog";
import type { Priority, Status } from "@/lib/todo-types";

export const Route = createFileRoute("/_authenticated/dashboard")({
  component: Dashboard,
  head: () => ({ meta: [{ title: "Dashboard — TodoFlow" }] }),
});

function Dashboard() {
  const [creating, setCreating] = useState(false);
  const [search, setSearch] = useState("");
  const [categoryId, setCategoryId] = useState("all");
  const [priority, setPriority] = useState("all");
  const [status, setStatus] = useState("all");
  const [sortBy, setSortBy] = useState("createdAt");

  const userId = useTodoStore((s) => s.currentUserId);
  const tasks = useTodoStore((s) => s.tasks);
  const allCategories = useTodoStore((s) => s.categories);
  const loading = useTodoStore((s) => s.loading);
  const error = useTodoStore((s) => s.error);
  const loadTasks = useTodoStore((s) => s.loadTasks);

  const categories = useMemo(
    () => allCategories.filter((c) => c.userId === userId),
    [allCategories, userId],
  );

  useEffect(() => {
    const timeout = window.setTimeout(() => {
      void loadTasks({
        search,
        categoryId,
        priority: priority as Priority | "all",
        status: status as Status | "all",
        sortBy: sortBy as "createdAt" | "dueDate" | "priority",
      }).catch(() => undefined);
    }, 250);

    return () => window.clearTimeout(timeout);
  }, [categoryId, loadTasks, priority, search, sortBy, status]);

  const filtered = useMemo(() => {
    return tasks.filter((t) => !t.isDeleted && t.userId === userId);
  }, [tasks, userId]);

  const groups = useMemo(
    () => ({
      todo: filtered.filter((t) => t.status === "todo"),
      in_progress: filtered.filter((t) => t.status === "in_progress"),
      done: filtered.filter((t) => t.status === "done"),
    }),
    [filtered],
  );

  return (
    <div className="mx-auto max-w-6xl p-6">
      <div className="mb-6 flex flex-wrap items-center justify-between gap-3">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight">Tasks của tôi</h1>
          <p className="text-sm text-muted-foreground">
            {filtered.length} task · {groups.done.length} hoàn thành
          </p>
        </div>
        <Button onClick={() => setCreating(true)}>
          <Plus className="mr-2 h-4 w-4" /> Task mới
        </Button>
      </div>

      <div className="mb-6 flex flex-wrap items-center gap-2 rounded-xl border border-border bg-card p-3">
        <div className="relative min-w-[200px] flex-1">
          <Search className="pointer-events-none absolute left-2.5 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
          <Input
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            placeholder="Tìm task..."
            className="pl-8"
          />
        </div>
        <Select value={categoryId} onValueChange={setCategoryId}>
          <SelectTrigger className="w-40">
            <SelectValue placeholder="Category" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">Tất cả category</SelectItem>
            {categories.map((c) => (
              <SelectItem key={c.id} value={c.id}>
                {c.name}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
        <Select value={priority} onValueChange={setPriority}>
          <SelectTrigger className="w-32">
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">Priority</SelectItem>
            <SelectItem value="low">Low</SelectItem>
            <SelectItem value="medium">Medium</SelectItem>
            <SelectItem value="high">High</SelectItem>
          </SelectContent>
        </Select>
        <Select value={status} onValueChange={setStatus}>
          <SelectTrigger className="w-32">
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">Status</SelectItem>
            <SelectItem value="todo">Todo</SelectItem>
            <SelectItem value="in_progress">Đang làm</SelectItem>
            <SelectItem value="done">Xong</SelectItem>
          </SelectContent>
        </Select>
        <Select value={sortBy} onValueChange={setSortBy}>
          <SelectTrigger className="w-40">
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="createdAt">Sắp xếp: mới nhất</SelectItem>
            <SelectItem value="dueDate">Sắp xếp: due date</SelectItem>
            <SelectItem value="priority">Sắp xếp: priority</SelectItem>
          </SelectContent>
        </Select>
      </div>

      {error && (
        <div className="mb-4 rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
          {error}
        </div>
      )}
      {loading && (
        <div className="mb-4 rounded-lg border border-border bg-muted px-4 py-3 text-sm text-muted-foreground">
          Đang tải dữ liệu...
        </div>
      )}

      <div className="grid gap-4 md:grid-cols-3">
        {(["todo", "in_progress", "done"] as const).map((col) => (
          <div key={col}>
            <div className="mb-3 flex items-center justify-between px-1">
              <h2 className="text-sm font-semibold uppercase tracking-wide text-muted-foreground">
                {col === "todo" ? "Todo" : col === "in_progress" ? "Đang làm" : "Xong"}
              </h2>
              <span className="text-xs text-muted-foreground">
                {groups[col].length}
              </span>
            </div>
            <div className="space-y-3">
              {groups[col].length === 0 && (
                <div className="rounded-lg border border-dashed border-border p-6 text-center text-xs text-muted-foreground">
                  Trống
                </div>
              )}
              {groups[col].map((t) => (
                <TaskCard
                  key={t.id}
                  task={t}
                  isShared={t.userId !== userId}
                />
              ))}
            </div>
          </div>
        ))}
      </div>

      <TaskDialog open={creating} onOpenChange={setCreating} />
    </div>
  );
}
