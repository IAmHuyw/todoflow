import { createFileRoute, Outlet, useLocation } from "@tanstack/react-router";
import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import {
  closestCorners,
  DndContext,
  KeyboardSensor,
  PointerSensor,
  TouchSensor,
  useDroppable,
  useSensor,
  useSensors,
  type DragEndEvent,
} from "@dnd-kit/core";
import {
  SortableContext,
  sortableKeyboardCoordinates,
  useSortable,
  verticalListSortingStrategy,
} from "@dnd-kit/sortable";
import { CSS } from "@dnd-kit/utilities";
import { GripVertical, Loader2, Plus, Search } from "lucide-react";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { ToggleGroup, ToggleGroupItem } from "@/components/ui/toggle-group";
import { useTodoStore, type PagedResult, type TaskListQuery } from "@/lib/todo-store";
import { TaskCard, type TaskChange } from "@/components/task/TaskCard";
import { TaskDialog } from "@/components/task/TaskDialog";
import type { Priority, Status, Task } from "@/lib/todo-types";

export const Route = createFileRoute("/_authenticated/tasks")({
  component: TasksRoute,
  head: () => ({ meta: [{ title: "Công việc - TodoFlow" }] }),
});

function TasksRoute() {
  const pathname = useLocation({ select: (location) => location.pathname });
  const isTaskDetail = pathname.startsWith("/tasks/");

  return isTaskDetail ? <Outlet /> : <TasksBoard />;
}

const STATUSES: Status[] = ["todo", "in_progress", "done"];
const PAGE_SIZE = 30;

type StatusFilter = Status | "all";
type TaskSort = "sortOrder" | "createdAt" | "dueDate" | "priority";

interface ColumnState {
  items: Task[];
  page: number;
  totalCount: number;
  totalPages: number;
  loading: boolean;
  loadingMore: boolean;
  error: string | null;
}

type ColumnsState = Record<Status, ColumnState>;

function createColumnsState(): ColumnsState {
  const empty = (): ColumnState => ({
    items: [],
    page: 0,
    totalCount: 0,
    totalPages: 0,
    loading: false,
    loadingMore: false,
    error: null,
  });
  return { todo: empty(), in_progress: empty(), done: empty() };
}

function TasksBoard() {
  const [creating, setCreating] = useState(false);
  const [search, setSearch] = useState("");
  const [categoryId, setCategoryId] = useState("all");
  const [priority, setPriority] = useState<Priority | "all">("all");
  const [statusFilter, setStatusFilter] = useState<StatusFilter>("all");
  const [sortBy, setSortBy] = useState<TaskSort>("sortOrder");
  const [columns, setColumns] = useState<ColumnsState>(createColumnsState);
  const requestVersion = useRef(0);

  const sensors = useSensors(
    useSensor(PointerSensor, { activationConstraint: { distance: 8 } }),
    useSensor(TouchSensor, { activationConstraint: { delay: 180, tolerance: 6 } }),
    useSensor(KeyboardSensor, { coordinateGetter: sortableKeyboardCoordinates }),
  );

  const userId = useTodoStore((state) => state.currentUserId);
  const allCategories = useTodoStore((state) => state.categories);
  const loadTaskPage = useTodoStore((state) => state.loadTaskPage);
  const moveTask = useTodoStore((state) => state.moveTask);

  const categories = useMemo(
    () => allCategories.filter((category) => category.userId === userId),
    [allCategories, userId],
  );
  const visibleStatuses = useMemo(
    () => (statusFilter === "all" ? STATUSES : [statusFilter]),
    [statusFilter],
  );
  const manualSorting = sortBy === "sortOrder";
  const hasActiveFilters = Boolean(search.trim()) || categoryId !== "all" || priority !== "all";

  const buildQuery = useCallback(
    (status: Status, page: number): TaskListQuery => ({
      scope: "owned",
      status,
      search,
      categoryId,
      priority,
      sortBy,
      sortDir: sortBy === "createdAt" ? "desc" : "asc",
      page,
      pageSize: PAGE_SIZE,
    }),
    [categoryId, priority, search, sortBy],
  );

  useEffect(() => {
    const version = ++requestVersion.current;
    const timeout = window.setTimeout(() => {
      setColumns((current) => {
        const next = { ...current };
        for (const status of visibleStatuses) {
          next[status] = { ...current[status], loading: true, error: null };
        }
        return next;
      });

      for (const status of visibleStatuses) {
        void loadTaskPage(buildQuery(status, 1))
          .then((page) => {
            if (requestVersion.current !== version) return;
            setColumns((current) => ({
              ...current,
              [status]: {
                items: page.items,
                page: page.page,
                totalCount: page.totalCount,
                totalPages: page.totalPages,
                loading: false,
                loadingMore: false,
                error: null,
              },
            }));
          })
          .catch((error) => {
            if (requestVersion.current !== version) return;
            setColumns((current) => ({
              ...current,
              [status]: {
                ...current[status],
                loading: false,
                loadingMore: false,
                error: getErrorMessage(error),
              },
            }));
          });
      }
    }, 250);

    return () => window.clearTimeout(timeout);
  }, [buildQuery, loadTaskPage, visibleStatuses]);

  const loadMore = async (status: Status) => {
    const column = columns[status];
    if (column.loadingMore || column.page >= column.totalPages) return;

    setColumns((current) => ({
      ...current,
      [status]: { ...current[status], loadingMore: true, error: null },
    }));
    try {
      const page = await loadTaskPage(buildQuery(status, column.page + 1));
      setColumns((current) => ({
        ...current,
        [status]: {
          ...current[status],
          items: mergeUniqueTasks(current[status].items, page.items),
          page: page.page,
          totalCount: page.totalCount,
          totalPages: page.totalPages,
          loadingMore: false,
        },
      }));
    } catch (error) {
      const message = getErrorMessage(error);
      setColumns((current) => ({
        ...current,
        [status]: { ...current[status], loadingMore: false, error: message },
      }));
      toast.error(message);
    }
  };

  const reloadColumns = async (statuses: Status[]) => {
    const uniqueStatuses = [...new Set(statuses)];
    await Promise.all(
      uniqueStatuses.map(async (status) => {
        const pagesToLoad = Math.max(columns[status].page, 1);
        const pages: PagedResult<Task>[] = [];
        for (let pageNumber = 1; pageNumber <= pagesToLoad; pageNumber++) {
          pages.push(await loadTaskPage(buildQuery(status, pageNumber)));
        }
        const lastPage = pages.at(-1)!;
        setColumns((current) => ({
          ...current,
          [status]: {
            ...current[status],
            items: mergeUniqueTasks(
              [],
              pages.flatMap((page) => page.items),
            ),
            page: Math.min(pagesToLoad, Math.max(lastPage.totalPages, 1)),
            totalCount: lastPage.totalCount,
            totalPages: lastPage.totalPages,
            loading: false,
            loadingMore: false,
            error: null,
          },
        }));
      }),
    );
  };

  const handleTaskChange = (change: TaskChange) => {
    const affected =
      change.nextStatus && change.nextStatus !== change.previousStatus
        ? [change.previousStatus, change.nextStatus]
        : [change.previousStatus];
    void reloadColumns(affected).catch((error) => toast.error(getErrorMessage(error)));
  };

  const handleDragEnd = async ({ active, over }: DragEndEvent) => {
    if (!manualSorting || !over || active.id === over.id) return;

    const activeTask = STATUSES.flatMap((status) => columns[status].items).find(
      (task) => task.id === String(active.id),
    );
    if (!activeTask || activeTask.userId !== userId) return;

    const overId = String(over.id);
    const overTask = STATUSES.flatMap((status) => columns[status].items).find(
      (task) => task.id === overId,
    );
    const targetStatus = overTask?.status ?? parseColumnId(overId);
    if (!targetStatus) return;

    const snapshot = cloneColumns(columns);
    const next = cloneColumns(columns);
    next[activeTask.status].items = next[activeTask.status].items.filter(
      (task) => task.id !== activeTask.id,
    );

    const targetItems = next[targetStatus].items;
    const columnAnchorId = overTask ? null : (targetItems.at(-1)?.id ?? null);
    const targetIndex = overTask
      ? Math.max(
          0,
          targetItems.findIndex((task) => task.id === overTask.id),
        )
      : targetItems.length;
    const movedTask = { ...activeTask, status: targetStatus };
    targetItems.splice(targetIndex < 0 ? targetItems.length : targetIndex, 0, movedTask);

    if (activeTask.status !== targetStatus) {
      next[activeTask.status].totalCount = Math.max(0, next[activeTask.status].totalCount - 1);
      next[targetStatus].totalCount += 1;
    }
    for (const status of [activeTask.status, targetStatus]) {
      next[status].items = next[status].items.map((task, index) => ({
        ...task,
        sortOrder: index,
      }));
    }
    setColumns(next);

    const anchorTaskId = overTask?.id ?? columnAnchorId;
    const placement = overTask ? "before" : "after";

    try {
      await moveTask(activeTask.id, { status: targetStatus, anchorTaskId, placement });
      await reloadColumns([activeTask.status, targetStatus]);
    } catch (error) {
      setColumns(snapshot);
      toast.error(`Không thể di chuyển công việc. ${getErrorMessage(error)}`);
    }
  };

  const totalVisible = visibleStatuses.reduce(
    (total, status) => total + columns[status].totalCount,
    0,
  );
  const firstError = visibleStatuses.map((status) => columns[status].error).find(Boolean);

  return (
    <div className="mx-auto max-w-6xl p-4 sm:p-6">
      <div className="mb-6 flex flex-wrap items-center justify-between gap-3">
        <div>
          <h1 className="text-2xl font-semibold">Công việc của tôi</h1>
          <p className="text-sm text-muted-foreground">
            {totalVisible} công việc
            {statusFilter === "all" ? ` · ${columns.done.totalCount} hoàn thành` : ""}
          </p>
        </div>
        <Button onClick={() => setCreating(true)}>
          <Plus className="mr-2 h-4 w-4" /> Công việc mới
        </Button>
      </div>

      <div className="mb-4 flex flex-wrap items-center gap-2 rounded-lg border border-border bg-card p-3">
        <div className="relative min-w-[200px] flex-1">
          <Search className="pointer-events-none absolute left-2.5 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
          <Input
            value={search}
            onChange={(event) => setSearch(event.target.value)}
            placeholder="Tìm công việc..."
            className="pl-8"
          />
        </div>
        <Select value={categoryId} onValueChange={setCategoryId}>
          <SelectTrigger className="w-full sm:w-40">
            <SelectValue placeholder="Danh mục" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">Tất cả danh mục</SelectItem>
            {categories.map((category) => (
              <SelectItem key={category.id} value={category.id}>
                {category.name}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
        <Select value={priority} onValueChange={(value) => setPriority(value as Priority | "all")}>
          <SelectTrigger className="w-full sm:w-36">
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">Độ ưu tiên</SelectItem>
            <SelectItem value="low">Thấp</SelectItem>
            <SelectItem value="medium">Trung bình</SelectItem>
            <SelectItem value="high">Cao</SelectItem>
          </SelectContent>
        </Select>
        <Select value={sortBy} onValueChange={(value) => setSortBy(value as TaskSort)}>
          <SelectTrigger className="w-full sm:w-48">
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

      <ToggleGroup
        type="single"
        value={statusFilter}
        onValueChange={(value) => value && setStatusFilter(value as StatusFilter)}
        variant="outline"
        className="mb-6 w-full justify-start overflow-x-auto rounded-md"
        aria-label="Lọc theo trạng thái"
      >
        <ToggleGroupItem value="all" className="shrink-0">
          Tất cả
        </ToggleGroupItem>
        <ToggleGroupItem value="todo" className="shrink-0">
          Cần làm
        </ToggleGroupItem>
        <ToggleGroupItem value="in_progress" className="shrink-0">
          Đang làm
        </ToggleGroupItem>
        <ToggleGroupItem value="done" className="shrink-0">
          Xong
        </ToggleGroupItem>
      </ToggleGroup>

      {firstError && (
        <div className="mb-4 rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
          {firstError}
        </div>
      )}

      <DndContext sensors={sensors} collisionDetection={closestCorners} onDragEnd={handleDragEnd}>
        <div
          className={`grid gap-4 ${visibleStatuses.length === 3 ? "md:grid-cols-3" : "grid-cols-1"}`}
        >
          {visibleStatuses.map((status) => (
            <TaskColumn
              key={status}
              status={status}
              column={columns[status]}
              userId={userId}
              manualSorting={manualSorting}
              filtered={hasActiveFilters}
              onLoadMore={() => void loadMore(status)}
              onTaskChange={handleTaskChange}
            />
          ))}
        </div>
      </DndContext>

      <TaskDialog
        open={creating}
        onOpenChange={(open) => {
          setCreating(open);
          if (!open) {
            void reloadColumns(visibleStatuses).catch((error) =>
              toast.error(getErrorMessage(error)),
            );
          }
        }}
      />
    </div>
  );
}

function TaskColumn({
  status,
  column,
  userId,
  manualSorting,
  filtered,
  onLoadMore,
  onTaskChange,
}: {
  status: Status;
  column: ColumnState;
  userId: string | null;
  manualSorting: boolean;
  filtered: boolean;
  onLoadMore: () => void;
  onTaskChange: (change: TaskChange) => void;
}) {
  const { setNodeRef, isOver } = useDroppable({ id: `column:${status}` });
  const hasMore = column.page < column.totalPages;

  return (
    <section>
      <div className="mb-3 flex items-center justify-between px-1">
        <h2 className="text-sm font-semibold uppercase text-muted-foreground">
          {status === "todo" ? "Cần làm" : status === "in_progress" ? "Đang làm" : "Xong"}
        </h2>
        <span className="text-xs text-muted-foreground">{column.totalCount}</span>
      </div>
      <SortableContext
        items={column.items.map((task) => task.id)}
        strategy={verticalListSortingStrategy}
      >
        <div
          ref={setNodeRef}
          className={`min-h-24 space-y-3 rounded-lg transition-colors ${isOver && manualSorting ? "bg-muted/60" : ""}`}
        >
          {column.loading && column.items.length === 0 && (
            <ColumnMessage>
              <Loader2 className="mr-2 h-4 w-4 animate-spin" />
              Đang tải...
            </ColumnMessage>
          )}
          {!column.loading && column.items.length === 0 && (
            <ColumnMessage>
              {filtered ? "Không có kết quả phù hợp." : "Chưa có công việc trong cột này."}
            </ColumnMessage>
          )}
          {column.items.map((task) => (
            <SortableTaskCard
              key={task.id}
              task={task}
              isShared={task.userId !== userId}
              draggable={manualSorting}
              onTaskChange={onTaskChange}
            />
          ))}
          {hasMore && (
            <Button
              type="button"
              variant="outline"
              className="w-full"
              disabled={column.loadingMore}
              onClick={onLoadMore}
            >
              {column.loadingMore && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
              Tải thêm ({column.items.length}/{column.totalCount})
            </Button>
          )}
        </div>
      </SortableContext>
    </section>
  );
}

function SortableTaskCard({
  task,
  isShared,
  draggable,
  onTaskChange,
}: {
  task: Task;
  isShared: boolean;
  draggable: boolean;
  onTaskChange: (change: TaskChange) => void;
}) {
  const disabled = isShared || !draggable;
  const {
    attributes,
    listeners,
    setActivatorNodeRef,
    setNodeRef,
    transform,
    transition,
    isDragging,
  } = useSortable({ id: task.id, disabled });

  return (
    <div
      ref={setNodeRef}
      style={{ transform: CSS.Transform.toString(transform), transition }}
      className={`group relative ${isDragging ? "opacity-60" : ""}`}
    >
      {!disabled && (
        <button
          ref={setActivatorNodeRef}
          type="button"
          className="absolute -left-2 top-3 z-10 flex h-7 w-7 cursor-grab items-center justify-center rounded-md border border-border bg-background text-muted-foreground shadow-sm hover:bg-muted hover:text-foreground active:cursor-grabbing sm:-left-3 sm:opacity-0 sm:transition-opacity sm:group-hover:opacity-100 sm:focus:opacity-100"
          aria-label="Kéo để sắp xếp công việc"
          title="Kéo để sắp xếp"
          {...attributes}
          {...listeners}
        >
          <GripVertical className="h-4 w-4" />
        </button>
      )}
      <TaskCard task={task} isShared={isShared} onTaskChange={onTaskChange} />
    </div>
  );
}

function ColumnMessage({ children }: { children: React.ReactNode }) {
  return (
    <div className="flex min-h-24 items-center justify-center rounded-lg border border-dashed border-border p-6 text-center text-xs text-muted-foreground">
      {children}
    </div>
  );
}

function cloneColumns(columns: ColumnsState): ColumnsState {
  return {
    todo: { ...columns.todo, items: [...columns.todo.items] },
    in_progress: { ...columns.in_progress, items: [...columns.in_progress.items] },
    done: { ...columns.done, items: [...columns.done.items] },
  };
}

function mergeUniqueTasks(current: Task[], incoming: Task[]) {
  const result = [...current];
  const indexes = new Map(result.map((task, index) => [task.id, index]));
  for (const task of incoming) {
    const index = indexes.get(task.id);
    if (index === undefined) {
      indexes.set(task.id, result.length);
      result.push(task);
    } else {
      result[index] = task;
    }
  }
  return result;
}

function parseColumnId(id: string): Status | null {
  if (!id.startsWith("column:")) return null;
  const status = id.replace("column:", "");
  return status === "todo" || status === "in_progress" || status === "done" ? status : null;
}

function getErrorMessage(error: unknown) {
  return error instanceof Error ? error.message : "Có lỗi xảy ra khi kết nối máy chủ.";
}
