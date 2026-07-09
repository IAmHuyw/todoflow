import { createFileRoute } from "@tanstack/react-router";
import { formatDistanceToNow } from "date-fns";
import { Bell, Check, CheckCheck, Share2, Clock, CheckCircle2 } from "lucide-react";
import { useMemo } from "react";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/utils";
import { useTodoStore } from "@/lib/todo-store";
import type { NotificationType } from "@/lib/todo-types";
import { toast } from "sonner";

export const Route = createFileRoute("/_authenticated/notifications")({
  component: NotificationsPage,
  head: () => ({ meta: [{ title: "Notifications — TodoFlow" }] }),
});

const iconFor: Record<NotificationType, typeof Bell> = {
  due_date_reminder: Clock,
  task_shared: Share2,
  task_updated: Bell,
  task_completed: CheckCircle2,
};

function NotificationsPage() {
  const userId = useTodoStore((s) => s.currentUserId);
  const allNotifications = useTodoStore((s) => s.notifications);
  const notifications = useMemo(
    () =>
      allNotifications
      .filter((n) => n.userId === userId)
      .sort(
        (a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime(),
      ),
    [allNotifications, userId],
  );
  const markRead = useTodoStore((s) => s.markNotificationRead);
  const markAll = useTodoStore((s) => s.markAllNotificationsRead);
  const unread = notifications.filter((n) => !n.isRead).length;

  const markOneRead = async (id: string) => {
    try {
      await markRead(id);
    } catch (error) {
      toast.error(error instanceof Error ? error.message : "Không đánh dấu được thông báo");
    }
  };

  const markAllRead = async () => {
    try {
      await markAll();
    } catch (error) {
      toast.error(error instanceof Error ? error.message : "Không đánh dấu được thông báo");
    }
  };

  return (
    <div className="mx-auto max-w-3xl p-6">
      <div className="mb-6 flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight">Notifications</h1>
          <p className="text-sm text-muted-foreground">
            {unread} chưa đọc · tổng {notifications.length}
          </p>
        </div>
        {unread > 0 && (
          <Button variant="outline" onClick={() => void markAllRead()}>
            <CheckCheck className="mr-2 h-4 w-4" /> Đọc tất cả
          </Button>
        )}
      </div>

      <div className="space-y-2">
        {notifications.length === 0 && (
          <div className="rounded-xl border border-dashed border-border p-10 text-center text-sm text-muted-foreground">
            Không có thông báo nào.
          </div>
        )}
        {notifications.map((n) => {
          const Icon = iconFor[n.type] ?? Bell;
          return (
            <div
              key={n.id}
              className={cn(
                "flex items-start gap-3 rounded-xl border border-border p-3 transition-colors",
                n.isRead ? "bg-card" : "bg-primary/5 border-primary/20",
              )}
            >
              <div
                className={cn(
                  "flex h-8 w-8 shrink-0 items-center justify-center rounded-full",
                  n.isRead ? "bg-muted text-muted-foreground" : "bg-primary/10 text-primary",
                )}
              >
                <Icon className="h-4 w-4" />
              </div>
              <div className="min-w-0 flex-1">
                <p className="text-sm">{n.message}</p>
                <p className="mt-0.5 text-xs text-muted-foreground">
                  {formatDistanceToNow(new Date(n.createdAt), { addSuffix: true })}
                </p>
              </div>
              {!n.isRead && (
                <Button
                  variant="ghost"
                  size="icon"
                  className="h-8 w-8"
                  onClick={() => void markOneRead(n.id)}
                >
                  <Check className="h-4 w-4" />
                </Button>
              )}
            </div>
          );
        })}
      </div>
    </div>
  );
}
