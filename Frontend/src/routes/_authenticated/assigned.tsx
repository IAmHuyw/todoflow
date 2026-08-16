import { createFileRoute } from "@tanstack/react-router";
import { useCallback, useEffect, useState } from "react";
import { ClipboardCheck, LoaderCircle } from "lucide-react";
import { TaskCard } from "@/components/task/TaskCard";
import { Button } from "@/components/ui/button";
import { useTodoStore } from "@/lib/todo-store";
import type { Task } from "@/lib/todo-types";

export const Route = createFileRoute("/_authenticated/assigned")({
  component: AssignedTasksPage,
  head: () => ({ meta: [{ title: "Được giao cho tôi — TodoFlow" }] }),
});

function AssignedTasksPage() {
  const userId = useTodoStore((state) => state.currentUserId);
  const loadTaskPage = useTodoStore((state) => state.loadTaskPage);
  const [tasks, setTasks] = useState<Task[]>([]);
  const [page, setPage] = useState(1);
  const [totalPages, setTotalPages] = useState(0);
  const [totalCount, setTotalCount] = useState(0);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const load = useCallback(
    async (nextPage: number, append = false) => {
      setLoading(true);
      setError(null);
      try {
        const result = await loadTaskPage({
          scope: "accessible",
          assignee: "me",
          page: nextPage,
          pageSize: 30,
          sortBy: "dueDate",
          sortDir: "asc",
        });
        setTasks((current) =>
          append
            ? [...new Map([...current, ...result.items].map((task) => [task.id, task])).values()]
            : result.items,
        );
        setPage(result.page);
        setTotalPages(result.totalPages);
        setTotalCount(result.totalCount);
      } catch (caught) {
        setError(caught instanceof Error ? caught.message : "Không tải được công việc được giao.");
      } finally {
        setLoading(false);
      }
    },
    [loadTaskPage],
  );

  useEffect(() => {
    void load(1);
  }, [load]);

  return (
    <div className="mx-auto max-w-5xl p-4 sm:p-6">
      <div className="mb-6">
        <h1 className="text-2xl font-semibold">Được giao cho tôi</h1>
        <p className="text-sm text-muted-foreground">{totalCount} công việc bạn đang phụ trách</p>
      </div>

      {error && (
        <div className="mb-4 border border-red-200 bg-red-50 p-3 text-sm text-red-700">{error}</div>
      )}

      {!loading && tasks.length === 0 ? (
        <div className="flex min-h-64 flex-col items-center justify-center border border-dashed border-border text-center">
          <ClipboardCheck className="mb-3 h-9 w-9 text-muted-foreground" />
          <p className="font-medium">Chưa có công việc được giao</p>
          <p className="mt-1 text-sm text-muted-foreground">
            Công việc sẽ xuất hiện tại đây khi chủ sở hữu chọn bạn làm người phụ trách.
          </p>
        </div>
      ) : (
        <div className="grid gap-3 md:grid-cols-2">
          {tasks.map((task) => (
            <TaskCard key={task.id} task={task} isShared={task.userId !== userId} />
          ))}
        </div>
      )}

      {loading && (
        <div className="flex justify-center py-8 text-muted-foreground">
          <LoaderCircle className="h-5 w-5 animate-spin" />
        </div>
      )}
      {!loading && page < totalPages && (
        <div className="mt-5 flex justify-center">
          <Button variant="outline" onClick={() => void load(page + 1, true)}>
            Tải thêm
          </Button>
        </div>
      )}
    </div>
  );
}
