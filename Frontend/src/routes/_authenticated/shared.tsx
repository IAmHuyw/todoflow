import { createFileRoute } from "@tanstack/react-router";
import { useMemo } from "react";
import { Check, X, Inbox } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { useTodoStore } from "@/lib/todo-store";
import { TaskCard } from "@/components/task/TaskCard";
import { toast } from "sonner";

export const Route = createFileRoute("/_authenticated/shared")({
  component: SharedPage,
  head: () => ({ meta: [{ title: "Shared — TodoFlow" }] }),
});

function SharedPage() {
  const userId = useTodoStore((s) => s.currentUserId);
  const shares = useTodoStore((s) => s.shares);
  const tasks = useTodoStore((s) => s.tasks);
  const users = useTodoStore((s) => s.users);
  const respond = useTodoStore((s) => s.respondShare);

  const invitations = useMemo(
    () =>
      shares.filter(
        (sh) => sh.sharedWithUserId === userId && sh.status === "pending",
      ),
    [shares, userId],
  );

  const respondToInvitation = async (id: string, status: "accepted" | "rejected") => {
    try {
      await respond(id, status);
      toast.success(status === "accepted" ? "Đã chấp nhận lời mời" : "Đã từ chối lời mời");
    } catch (error) {
      toast.error(error instanceof Error ? error.message : "Không phản hồi được lời mời");
    }
  };
  const acceptedTasks = useMemo(
    () =>
      shares
        .filter(
          (sh) => sh.sharedWithUserId === userId && sh.status === "accepted",
        )
        .map((sh) => tasks.find((t) => t.id === sh.taskId))
        .filter((t): t is NonNullable<typeof t> => !!t && !t.isDeleted),
    [shares, userId, tasks],
  );

  return (
    <div className="mx-auto max-w-4xl p-6">
      <div className="mb-6">
        <h1 className="text-2xl font-semibold tracking-tight">Shared with me</h1>
        <p className="text-sm text-muted-foreground">
          Task được cộng tác cùng người khác.
        </p>
      </div>

      {invitations.length > 0 && (
        <section className="mb-8">
          <h2 className="mb-3 text-sm font-semibold uppercase tracking-wide text-muted-foreground">
            Lời mời ({invitations.length})
          </h2>
          <div className="space-y-2">
            {invitations.map((sh) => {
              const task = tasks.find((t) => t.id === sh.taskId);
              const owner = users.find((u) => u.id === sh.ownerId);
              const ownerName = sh.ownerUsername ?? owner?.username ?? "User";
              return (
                <div
                  key={sh.id}
                  className="flex items-center justify-between rounded-xl border border-border bg-card p-3"
                >
                  <div>
                    <div className="font-medium">{task?.title}</div>
                    <div className="text-xs text-muted-foreground">
                      Từ <span className="font-medium">{ownerName}</span> ·
                      quyền <Badge variant="outline">{sh.permission}</Badge>
                    </div>
                  </div>
                  <div className="flex gap-2">
                    <Button
                      size="sm"
                      variant="outline"
                      onClick={() => void respondToInvitation(sh.id, "rejected")}
                    >
                      <X className="mr-1 h-3.5 w-3.5" /> Từ chối
                    </Button>
                    <Button
                      size="sm"
                      onClick={() => void respondToInvitation(sh.id, "accepted")}
                    >
                      <Check className="mr-1 h-3.5 w-3.5" /> Chấp nhận
                    </Button>
                  </div>
                </div>
              );
            })}
          </div>
        </section>
      )}

      <section>
        <h2 className="mb-3 text-sm font-semibold uppercase tracking-wide text-muted-foreground">
          Task đang cộng tác ({acceptedTasks.length})
        </h2>
        {acceptedTasks.length === 0 ? (
          <div className="flex flex-col items-center rounded-xl border border-dashed border-border p-10 text-center">
            <Inbox className="mb-2 h-8 w-8 text-muted-foreground" />
            <p className="text-sm text-muted-foreground">
              Chưa có task nào được chia sẻ.
            </p>
          </div>
        ) : (
          <div className="grid gap-3 md:grid-cols-2">
            {acceptedTasks.map((t) => (
              <TaskCard key={t.id} task={t} isShared />
            ))}
          </div>
        )}
      </section>
    </div>
  );
}
