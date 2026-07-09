import { createFileRoute } from "@tanstack/react-router";
import { useEffect, useMemo, useState } from "react";
import {
  closestCorners,
  DndContext,
  PointerSensor,
  useDroppable,
  useSensor,
  useSensors,
  type DragEndEvent,
} from "@dnd-kit/core";
import {
  SortableContext,
  useSortable,
  verticalListSortingStrategy,
} from "@dnd-kit/sortable";
import { CSS } from "@dnd-kit/utilities";
import { GripVertical, Plus, Search } from "lucide-react";
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
import type { Priority, Status, Task } from "@/lib/todo-types";

export const Route = createFileRoute("/_authenticated/dashboard")({
  component: Dashboard,
  head: () => ({ meta: [{ title: "Bảng công việc — TodoFlow" }] }),
});

function Dashboard() {
  const [creating, setCreating] = useState(false);
  const [search, setSearch] = useState("");
  const [categoryId, setCategoryId] = useState("all");
  const [priority, setPriority] = useState("all");
  const [status, setStatus] = useState("all");
  const [sortBy, setSortBy] = useState("sortOrder");
  const sensors = useSensors(useSensor(PointerSensor, { activationConstraint: { distance: 8 } }));

  const userId = useTodoStore((s) => s.currentUserId);
  const tasks = useTodoStore((s) => s.tasks);
  const allCategories = useTodoStore((s) => s.categories);
  const loading = useTodoStore((s) => s.loading);
  const error = useTodoStore((s) => s.error);
  const loadTasks = useTodoStore((s) => s.loadTasks);
  const reorderTasks = useTodoStore((s) => s.reorderTasks);

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
        sortBy: sortBy as "sortOrder" | "createdAt" | "dueDate" | "priority",
      }).catch(() => undefined);
    }, 250);

    return () => window.clearTimeout(timeout);
  }, [categoryId, loadTasks, priority, search, sortBy, status]);

  const filtered = useMemo(() => {
    return tasks.filter((t) => !t.isDeleted && t.userId === userId);
  }, [tasks, userId]);

  const groups = useMemo(
    () => ({
      todo: sortTasks(filtered.filter((t) => t.status === "todo")),
      in_progress: sortTasks(filtered.filter((t) => t.status === "in_progress")),
      done: sortTasks(filtered.filter((t) => t.status === "done")),
    }),
    [filtered],
  );

  const handleDragEnd = async ({ active, over }: DragEndEvent) => {
    if (!over || active.id === over.id) return;

    const activeTask = filtered.find((task) => task.id === active.id);
    if (!activeTask || activeTask.userId !== userId) return;

    const overId = String(over.id);
    const overTask = filtered.find((task) => task.id === overId);
    const targetStatus = overTask?.status ?? parseColumnId(overId);
    if (!targetStatus) return;

    const nextGroups = {
      todo: [...groups.todo],
      in_progress: [...groups.in_progress],
      done: [...groups.done],
    };

    nextGroups[activeTask.status] = nextGroups[activeTask.status].filter(
      (task) => task.id !== activeTask.id,
    );

    const movedTask = { ...activeTask, status: targetStatus };
    const targetTasks = nextGroups[targetStatus];
    const targetIndex = overTask
      ? Math.max(
          0,
          targetTasks.findIndex((task) => task.id === overTask.id),
        )
      : targetTasks.length;

    targetTasks.splice(targetIndex === -1 ? targetTasks.length : targetIndex, 0, movedTask);

    const items = (["todo", "in_progress", "done"] as const).flatMap((statusKey) =>
      nextGroups[statusKey].map((task, index) => ({
        id: task.id,
        status: statusKey,
        sortOrder: index,
      })),
    );

    await reorderTasks(items).catch(() => undefined);
  };

  return (
    <div className="mx-auto max-w-6xl p-6">
      <div className="mb-6 flex flex-wrap items-center justify-between gap-3">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight">Công việc của tôi</h1>
          <p className="text-sm text-muted-foreground">
            {filtered.length} công việc · {groups.done.length} hoàn thành
          </p>
        </div>
        <Button onClick={() => setCreating(true)}>
          <Plus className="mr-2 h-4 w-4" /> Công việc mới
        </Button>
      </div>

      <div className="mb-6 flex flex-wrap items-center gap-2 rounded-xl border border-border bg-card p-3">
        <div className="relative min-w-[200px] flex-1">
          <Search className="pointer-events-none absolute left-2.5 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
          <Input
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            placeholder="Tìm công việc..."
            className="pl-8"
          />
        </div>
        <Select value={categoryId} onValueChange={setCategoryId}>
          <SelectTrigger className="w-40">
            <SelectValue placeholder="Danh mục" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">Tất cả danh mục</SelectItem>
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
            <SelectItem value="all">Độ ưu tiên</SelectItem>
            <SelectItem value="low">Thấp</SelectItem>
            <SelectItem value="medium">Trung bình</SelectItem>
            <SelectItem value="high">Cao</SelectItem>
          </SelectContent>
        </Select>
        <Select value={status} onValueChange={setStatus}>
          <SelectTrigger className="w-32">
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">Trạng thái</SelectItem>
            <SelectItem value="todo">Cần làm</SelectItem>
            <SelectItem value="in_progress">Đang làm</SelectItem>
            <SelectItem value="done">Xong</SelectItem>
          </SelectContent>
        </Select>
        <Select value={sortBy} onValueChange={setSortBy}>
          <SelectTrigger className="w-40">
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="sortOrder">Sắp xếp: thủ công</SelectItem>
            <SelectItem value="createdAt">Sắp xếp: mới nhất</SelectItem>
            <SelectItem value="dueDate">Sắp xếp: hạn làm</SelectItem>
            <SelectItem value="priority">Sắp xếp: độ ưu tiên</SelectItem>
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

      <DndContext sensors={sensors} collisionDetection={closestCorners} onDragEnd={handleDragEnd}>
        <div className="grid gap-4 md:grid-cols-3">
          {(["todo", "in_progress", "done"] as const).map((col) => (
            <TaskColumn
              key={col}
              status={col}
              tasks={groups[col]}
              userId={userId}
            />
          ))}
        </div>
      </DndContext>

      <TaskDialog open={creating} onOpenChange={setCreating} />
    </div>
  );
}

function TaskColumn({
  status,
  tasks,
  userId,
}: {
  status: Status;
  tasks: Task[];
  userId: string | null;
}) {
  const { setNodeRef, isOver } = useDroppable({ id: `column:${status}` });

  return (
    <div>
      <div className="mb-3 flex items-center justify-between px-1">
        <h2 className="text-sm font-semibold uppercase tracking-wide text-muted-foreground">
          {status === "todo" ? "Cần làm" : status === "in_progress" ? "Đang làm" : "Xong"}
        </h2>
        <span className="text-xs text-muted-foreground">{tasks.length}</span>
      </div>
      <SortableContext items={tasks.map((task) => task.id)} strategy={verticalListSortingStrategy}>
        <div
          ref={setNodeRef}
          className={`min-h-24 space-y-3 rounded-lg transition-colors ${
            isOver ? "bg-muted/60" : ""
          }`}
        >
          {tasks.length === 0 && (
            <div className="rounded-lg border border-dashed border-border p-6 text-center text-xs text-muted-foreground">
              Trống
            </div>
          )}
          {tasks.map((task) => (
            <SortableTaskCard key={task.id} task={task} isShared={task.userId !== userId} />
          ))}
        </div>
      </SortableContext>
    </div>
  );
}

function SortableTaskCard({ task, isShared }: { task: Task; isShared: boolean }) {
  const {
    attributes,
    listeners,
    setActivatorNodeRef,
    setNodeRef,
    transform,
    transition,
    isDragging,
  } =
    useSortable({ id: task.id, disabled: isShared });

  return (
    <div
      ref={setNodeRef}
      style={{
        transform: CSS.Transform.toString(transform),
        transition,
      }}
      className={`group relative ${isDragging ? "opacity-60" : ""}`}
    >
      {!isShared && (
        <button
          ref={setActivatorNodeRef}
          type="button"
          className="absolute -left-3 top-3 z-10 flex h-7 w-7 cursor-grab items-center justify-center rounded-md border border-border bg-background text-muted-foreground opacity-0 shadow-sm transition-opacity hover:bg-muted hover:text-foreground active:cursor-grabbing group-hover:opacity-100"
          aria-label="Kéo để sắp xếp công việc"
          title="Kéo để sắp xếp"
          {...attributes}
          {...listeners}
        >
          <GripVertical className="h-4 w-4" />
        </button>
      )}
      <TaskCard task={task} isShared={isShared} />
    </div>
  );
}

function sortTasks(tasks: Task[]) {
  return [...tasks].sort((a, b) => {
    if (a.sortOrder !== b.sortOrder) return a.sortOrder - b.sortOrder;
    return new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime();
  });
}

function parseColumnId(id: string): Status | null {
  if (!id.startsWith("column:")) return null;
  const status = id.replace("column:", "");
  return status === "todo" || status === "in_progress" || status === "done"
    ? status
    : null;
}
