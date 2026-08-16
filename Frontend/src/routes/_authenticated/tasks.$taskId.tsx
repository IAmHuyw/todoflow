import { createFileRoute, Link } from "@tanstack/react-router";
import { useEffect, useMemo, useState } from "react";
import { format, formatDistanceToNow } from "date-fns";
import { vi } from "date-fns/locale";
import {
  ArrowLeft,
  Bell,
  Check,
  Clock3,
  History,
  LoaderCircle,
  MessageSquare,
  Pencil,
  Plus,
  Send,
  Share2,
  Trash2,
  UserRound,
} from "lucide-react";
import { toast } from "sonner";
import { TaskDialog } from "@/components/task/TaskDialog";
import { ShareDialog } from "@/components/task/ShareDialog";
import { ReminderDialog } from "@/components/task/ReminderDialog";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Checkbox } from "@/components/ui/checkbox";
import { Input } from "@/components/ui/input";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { Textarea } from "@/components/ui/textarea";
import { useTodoStore } from "@/lib/todo-store";
import type { Status, TaskComment } from "@/lib/todo-types";
import { cn } from "@/lib/utils";

export const Route = createFileRoute("/_authenticated/tasks/$taskId")({
  component: TaskDetailPage,
  head: () => ({ meta: [{ title: "Chi tiết công việc — TodoFlow" }] }),
});

const statusLabel: Record<Status, string> = {
  todo: "Cần làm",
  in_progress: "Đang làm",
  done: "Xong",
};

const emptyCollaborators = [] as const;

function TaskDetailPage() {
  const { taskId } = Route.useParams();
  const currentUserId = useTodoStore((state) => state.currentUserId);
  const task = useTodoStore((state) => state.tasks.find((item) => item.id === taskId));
  const shares = useTodoStore((state) => state.shares);
  const categories = useTodoStore((state) => state.categories);
  const tags = useTodoStore((state) => state.tags);
  const allSubtasks = useTodoStore((state) => state.subtasks);
  const taskCollaborators = useTodoStore((state) => state.taskCollaborators);
  const allComments = useTodoStore((state) => state.taskComments);
  const allActivities = useTodoStore((state) => state.taskActivities);
  const allReminders = useTodoStore((state) => state.reminders);
  const loadTask = useTodoStore((state) => state.loadTask);
  const loadCollaborators = useTodoStore((state) => state.loadTaskCollaborators);
  const loadComments = useTodoStore((state) => state.loadTaskComments);
  const loadActivities = useTodoStore((state) => state.loadTaskActivities);
  const updateAssignee = useTodoStore((state) => state.updateTaskAssignee);
  const setTaskStatus = useTodoStore((state) => state.setTaskStatus);
  const addSubtask = useTodoStore((state) => state.addSubtask);
  const toggleSubtask = useTodoStore((state) => state.toggleSubtask);
  const addComment = useTodoStore((state) => state.addTaskComment);
  const updateComment = useTodoStore((state) => state.updateTaskComment);
  const deleteComment = useTodoStore((state) => state.deleteTaskComment);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [commentPage, setCommentPage] = useState(1);
  const [commentTotalPages, setCommentTotalPages] = useState(0);
  const [activityPage, setActivityPage] = useState(1);
  const [activityTotalPages, setActivityTotalPages] = useState(0);
  const [commentText, setCommentText] = useState("");
  const [subtaskTitle, setSubtaskTitle] = useState("");
  const [sending, setSending] = useState(false);
  const [editingCommentId, setEditingCommentId] = useState<string | null>(null);
  const [editingCommentText, setEditingCommentText] = useState("");
  const [editingTask, setEditingTask] = useState(false);
  const [sharing, setSharing] = useState(false);
  const [reminding, setReminding] = useState(false);

  const subtasks = useMemo(
    () => allSubtasks.filter((item) => item.taskId === taskId),
    [allSubtasks, taskId],
  );
  const collaborators = taskCollaborators[taskId] ?? emptyCollaborators;
  const comments = useMemo(
    () => allComments.filter((item) => item.taskId === taskId),
    [allComments, taskId],
  );
  const activities = useMemo(
    () => allActivities.filter((item) => item.taskId === taskId),
    [allActivities, taskId],
  );
  const reminders = useMemo(
    () => allReminders.filter((item) => item.taskId === taskId),
    [allReminders, taskId],
  );

  useEffect(() => {
    let active = true;
    setLoading(true);
    Promise.all([
      loadTask(taskId),
      loadCollaborators(taskId),
      loadComments(taskId, 1),
      loadActivities(taskId, 1),
    ])
      .then(([, , commentResult, activityResult]) => {
        if (!active) return;
        setCommentPage(1);
        setCommentTotalPages(commentResult.totalPages);
        setActivityPage(1);
        setActivityTotalPages(activityResult.totalPages);
      })
      .catch((caught) => {
        if (active) {
          setError(caught instanceof Error ? caught.message : "Không tải được công việc.");
        }
      })
      .finally(() => {
        if (active) setLoading(false);
      });
    return () => {
      active = false;
    };
  }, [loadActivities, loadCollaborators, loadComments, loadTask, taskId]);

  const owner = task?.userId === currentUserId;
  const currentShare = shares.find(
    (share) =>
      share.taskId === taskId &&
      share.sharedWithUserId === currentUserId &&
      share.status === "accepted",
  );
  const canEdit = owner || currentShare?.permission === "edit";
  const category = categories.find((item) => item.id === task?.categoryId);
  const taskTags = tags.filter((tag) => task?.tagIds.includes(tag.id));
  const orderedComments = useMemo(
    () => [...comments].sort((a, b) => Date.parse(a.createdAt) - Date.parse(b.createdAt)),
    [comments],
  );
  const orderedActivities = useMemo(
    () => [...activities].sort((a, b) => Date.parse(b.createdAt) - Date.parse(a.createdAt)),
    [activities],
  );
  const mentionQuery = commentText.match(/(?:^|\s)@([\w.-]*)$/)?.[1]?.toLowerCase();
  const mentionSuggestions =
    mentionQuery === undefined
      ? []
      : collaborators
          .filter((item) => item.username.toLowerCase().startsWith(mentionQuery))
          .slice(0, 5);

  const run = async (action: Promise<unknown>, success?: string) => {
    try {
      await action;
      if (success) toast.success(success);
    } catch (caught) {
      toast.error(caught instanceof Error ? caught.message : "Không thực hiện được thao tác.");
    }
  };

  const submitComment = async () => {
    const content = commentText.trim();
    if (!content) return;
    setSending(true);
    try {
      await addComment(taskId, content);
      setCommentText("");
    } catch (caught) {
      toast.error(caught instanceof Error ? caught.message : "Không gửi được bình luận.");
    } finally {
      setSending(false);
    }
  };

  if (loading) {
    return (
      <div className="flex min-h-[60vh] items-center justify-center text-muted-foreground">
        <LoaderCircle className="h-6 w-6 animate-spin" />
      </div>
    );
  }

  if (!task || error) {
    return (
      <div className="mx-auto max-w-xl p-6 text-center">
        <h1 className="text-xl font-semibold">Không tải được công việc</h1>
        <p className="mt-2 text-sm text-muted-foreground">{error ?? "Công việc không tồn tại."}</p>
        <Button asChild className="mt-5">
          <Link to="/tasks">Quay lại bảng công việc</Link>
        </Button>
      </div>
    );
  }

  return (
    <div className="mx-auto max-w-7xl p-4 sm:p-6">
      <div className="mb-5 flex flex-wrap items-center justify-between gap-3">
        <Button asChild variant="ghost" size="sm">
          <Link to="/tasks">
            <ArrowLeft className="mr-2 h-4 w-4" />
            Bảng công việc
          </Link>
        </Button>
        <div className="flex items-center gap-2">
          {canEdit && (
            <Button variant="outline" size="sm" onClick={() => setReminding(true)}>
              <Bell className="mr-2 h-4 w-4" />
              Nhắc nhở
            </Button>
          )}
          {owner && (
            <Button variant="outline" size="sm" onClick={() => setSharing(true)}>
              <Share2 className="mr-2 h-4 w-4" />
              Chia sẻ
            </Button>
          )}
          {canEdit && (
            <Button size="sm" onClick={() => setEditingTask(true)}>
              <Pencil className="mr-2 h-4 w-4" />
              Chỉnh sửa
            </Button>
          )}
        </div>
      </div>

      <div className="grid items-start gap-0 border border-border lg:grid-cols-[minmax(0,1.45fr)_minmax(340px,0.9fr)]">
        <section className="min-w-0 p-4 sm:p-6 lg:border-r lg:border-border">
          <div className="flex flex-wrap items-start justify-between gap-4">
            <div className="min-w-0">
              <h1
                className={cn(
                  "break-words text-2xl font-semibold",
                  task.status === "done" && "text-muted-foreground line-through",
                )}
              >
                {task.title}
              </h1>
              <div className="mt-3 flex flex-wrap gap-2">
                <Badge variant="outline">{statusLabel[task.status]}</Badge>
                <Badge variant="secondary">
                  Ưu tiên{" "}
                  {task.priority === "high"
                    ? "cao"
                    : task.priority === "medium"
                      ? "trung bình"
                      : "thấp"}
                </Badge>
                {category && (
                  <Badge
                    style={{ borderColor: category.color, color: category.color }}
                    variant="outline"
                  >
                    {category.name}
                  </Badge>
                )}
                {taskTags.map((tag) => (
                  <Badge key={tag.id} variant="outline">
                    #{tag.name}
                  </Badge>
                ))}
              </div>
            </div>
            {canEdit ? (
              <Select
                value={task.status}
                onValueChange={(value: Status) =>
                  void run(setTaskStatus(task.id, value), "Đã cập nhật trạng thái")
                }
              >
                <SelectTrigger className="w-40">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="todo">Cần làm</SelectItem>
                  <SelectItem value="in_progress">Đang làm</SelectItem>
                  <SelectItem value="done">Xong</SelectItem>
                </SelectContent>
              </Select>
            ) : null}
          </div>

          <div className="mt-7 border-t border-border pt-5">
            <h2 className="text-sm font-semibold">Mô tả</h2>
            <p className="mt-2 whitespace-pre-wrap text-sm leading-6 text-muted-foreground">
              {task.description || "Chưa có mô tả."}
            </p>
          </div>

          <div className="mt-6 grid gap-5 border-t border-border pt-5 sm:grid-cols-2">
            <div>
              <div className="mb-2 flex items-center gap-2 text-sm font-semibold">
                <UserRound className="h-4 w-4" />
                Người phụ trách
              </div>
              {owner ? (
                <Select
                  value={task.assigneeId ?? "unassigned"}
                  onValueChange={(value) =>
                    void run(
                      updateAssignee(task.id, value === "unassigned" ? null : value),
                      "Đã cập nhật người phụ trách",
                    )
                  }
                >
                  <SelectTrigger>
                    <SelectValue placeholder="Chưa giao" />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="unassigned">Chưa giao</SelectItem>
                    {collaborators
                      .filter((item) => item.isOwner || item.permission === "edit")
                      .map((item) => (
                        <SelectItem key={item.userId} value={item.userId}>
                          {item.fullName || item.username}
                          {item.isOwner ? " (chủ sở hữu)" : ""}
                        </SelectItem>
                      ))}
                  </SelectContent>
                </Select>
              ) : (
                <p className="text-sm text-muted-foreground">
                  {task.assigneeFullName || task.assigneeUsername || "Chưa giao"}
                </p>
              )}
            </div>
            <div>
              <div className="mb-2 flex items-center gap-2 text-sm font-semibold">
                <Clock3 className="h-4 w-4" />
                Thời hạn
              </div>
              <p className="text-sm text-muted-foreground">
                {task.dueDate
                  ? format(new Date(task.dueDate), "HH:mm, dd/MM/yyyy")
                  : "Không có thời hạn"}
              </p>
              {reminders.length > 0 && (
                <p className="mt-1 text-xs text-muted-foreground">
                  {reminders.length} lịch nhắc đã đặt
                </p>
              )}
            </div>
          </div>

          <div className="mt-7 border-t border-border pt-5">
            <div className="mb-3 flex items-center justify-between">
              <h2 className="font-semibold">Việc con</h2>
              <span className="text-xs text-muted-foreground">
                {subtasks.filter((item) => item.isCompleted).length}/{subtasks.length} hoàn thành
              </span>
            </div>
            <div className="space-y-2">
              {subtasks.map((subtask) => (
                <label
                  key={subtask.id}
                  className="flex items-start gap-3 border-b border-border py-2 last:border-0"
                >
                  <Checkbox
                    disabled={!canEdit}
                    checked={subtask.isCompleted}
                    onCheckedChange={() => void run(toggleSubtask(subtask.id))}
                  />
                  <span
                    className={cn(
                      "text-sm",
                      subtask.isCompleted && "text-muted-foreground line-through",
                    )}
                  >
                    {subtask.title}
                    {subtask.note && (
                      <span className="mt-0.5 block text-xs text-muted-foreground">
                        {subtask.note}
                      </span>
                    )}
                  </span>
                </label>
              ))}
              {subtasks.length === 0 && (
                <p className="text-sm text-muted-foreground">Chưa có việc con.</p>
              )}
            </div>
            {canEdit && (
              <form
                className="mt-3 flex gap-2"
                onSubmit={(event) => {
                  event.preventDefault();
                  const title = subtaskTitle.trim();
                  if (!title) return;
                  void run(
                    addSubtask(task.id, title).then(() => setSubtaskTitle("")),
                    "Đã thêm việc con",
                  );
                }}
              >
                <Input
                  value={subtaskTitle}
                  onChange={(event) => setSubtaskTitle(event.target.value)}
                  placeholder="Thêm việc con..."
                />
                <Button type="submit" size="icon" title="Thêm việc con">
                  <Plus className="h-4 w-4" />
                </Button>
              </form>
            )}
          </div>

          <div className="mt-7 border-t border-border pt-5">
            <h2 className="mb-3 font-semibold">Cộng tác viên</h2>
            <div className="flex flex-wrap gap-2">
              {collaborators.map((item) => (
                <span
                  key={item.userId}
                  className="inline-flex items-center gap-2 rounded-md border border-border px-2 py-1 text-sm"
                >
                  <span className="flex h-6 w-6 items-center justify-center rounded-full bg-muted text-xs font-medium">
                    {(item.fullName || item.username)[0]?.toUpperCase()}
                  </span>
                  {item.fullName || item.username}
                  <span className="text-xs text-muted-foreground">
                    {item.isOwner
                      ? "Chủ sở hữu"
                      : item.permission === "edit"
                        ? "Chỉnh sửa"
                        : "Chỉ xem"}
                  </span>
                </span>
              ))}
            </div>
          </div>
        </section>

        <section className="min-w-0 p-4 sm:p-6">
          <Tabs defaultValue="comments">
            <TabsList className="grid w-full grid-cols-2">
              <TabsTrigger value="comments">
                <MessageSquare className="mr-2 h-4 w-4" />
                Bình luận
              </TabsTrigger>
              <TabsTrigger value="activity">
                <History className="mr-2 h-4 w-4" />
                Lịch sử
              </TabsTrigger>
            </TabsList>
            <TabsContent value="comments" className="mt-4">
              {commentPage < commentTotalPages && (
                <Button
                  variant="ghost"
                  size="sm"
                  className="mb-3 w-full"
                  onClick={async () => {
                    const next = commentPage + 1;
                    const result = await loadComments(taskId, next);
                    setCommentPage(next);
                    setCommentTotalPages(result.totalPages);
                  }}
                >
                  Tải bình luận cũ hơn
                </Button>
              )}
              <div className="max-h-[520px] space-y-4 overflow-y-auto pr-1">
                {orderedComments.map((comment) => (
                  <CommentItem
                    key={comment.id}
                    comment={comment}
                    canDelete={owner || comment.authorId === currentUserId}
                    canEdit={comment.authorId === currentUserId}
                    editing={editingCommentId === comment.id}
                    editingText={editingCommentText}
                    onEditingText={setEditingCommentText}
                    onStartEdit={() => {
                      setEditingCommentId(comment.id);
                      setEditingCommentText(comment.content);
                    }}
                    onCancelEdit={() => setEditingCommentId(null)}
                    onSave={() =>
                      void run(
                        updateComment(comment.id, editingCommentText.trim()).then(() =>
                          setEditingCommentId(null),
                        ),
                        "Đã sửa bình luận",
                      )
                    }
                    onDelete={() => void run(deleteComment(comment.id), "Đã xóa bình luận")}
                  />
                ))}
                {orderedComments.length === 0 && (
                  <p className="py-10 text-center text-sm text-muted-foreground">
                    Chưa có bình luận. Hãy bắt đầu trao đổi.
                  </p>
                )}
              </div>
              <div className="relative mt-4 border-t border-border pt-4">
                {mentionSuggestions.length > 0 && (
                  <div className="absolute bottom-full mb-2 w-full border border-border bg-popover p-1 shadow-md">
                    {mentionSuggestions.map((item) => (
                      <button
                        key={item.userId}
                        type="button"
                        className="flex w-full items-center gap-2 rounded-sm px-2 py-1.5 text-left text-sm hover:bg-accent"
                        onClick={() =>
                          setCommentText((value) =>
                            value.replace(/@[\w.-]*$/, `@${item.username} `),
                          )
                        }
                      >
                        <UserRound className="h-4 w-4" />
                        {item.fullName || item.username}
                        <span className="text-xs text-muted-foreground">@{item.username}</span>
                      </button>
                    ))}
                  </div>
                )}
                <Textarea
                  value={commentText}
                  onChange={(event) => setCommentText(event.target.value)}
                  maxLength={2000}
                  rows={3}
                  placeholder="Viết bình luận, dùng @username để nhắc tên..."
                />
                <div className="mt-2 flex items-center justify-between">
                  <span className="text-xs text-muted-foreground">{commentText.length}/2000</span>
                  <Button
                    disabled={sending || !commentText.trim()}
                    onClick={() => void submitComment()}
                  >
                    <Send className="mr-2 h-4 w-4" />
                    {sending ? "Đang gửi..." : "Gửi"}
                  </Button>
                </div>
              </div>
            </TabsContent>
            <TabsContent value="activity" className="mt-4">
              <div className="space-y-1">
                {orderedActivities.map((activity) => (
                  <div key={activity.id} className="border-b border-border py-3 last:border-0">
                    <p className="text-sm">
                      <span className="font-medium">
                        {activity.actorFullName || activity.actorUsername || "Hệ thống"}
                      </span>{" "}
                      {activity.message}
                    </p>
                    <p className="mt-1 text-xs text-muted-foreground">
                      {formatDistanceToNow(new Date(activity.createdAt), {
                        addSuffix: true,
                        locale: vi,
                      })}
                    </p>
                  </div>
                ))}
                {orderedActivities.length === 0 && (
                  <p className="py-10 text-center text-sm text-muted-foreground">
                    Chưa có lịch sử hoạt động.
                  </p>
                )}
              </div>
              {activityPage < activityTotalPages && (
                <Button
                  variant="outline"
                  className="mt-3 w-full"
                  onClick={async () => {
                    const next = activityPage + 1;
                    const result = await loadActivities(taskId, next);
                    setActivityPage(next);
                    setActivityTotalPages(result.totalPages);
                  }}
                >
                  Tải thêm lịch sử
                </Button>
              )}
            </TabsContent>
          </Tabs>
        </section>
      </div>

      <TaskDialog
        open={editingTask}
        onOpenChange={setEditingTask}
        task={task}
        readOnly={!canEdit}
      />
      <ShareDialog open={sharing} onOpenChange={setSharing} taskId={task.id} />
      <ReminderDialog open={reminding} onOpenChange={setReminding} taskId={task.id} />
    </div>
  );
}

function CommentItem({
  comment,
  canDelete,
  canEdit,
  editing,
  editingText,
  onEditingText,
  onStartEdit,
  onCancelEdit,
  onSave,
  onDelete,
}: {
  comment: TaskComment;
  canDelete: boolean;
  canEdit: boolean;
  editing: boolean;
  editingText: string;
  onEditingText: (value: string) => void;
  onStartEdit: () => void;
  onCancelEdit: () => void;
  onSave: () => void;
  onDelete: () => void;
}) {
  return (
    <div className="group flex gap-3">
      <div className="flex h-8 w-8 shrink-0 items-center justify-center rounded-full bg-muted text-xs font-semibold">
        {(comment.authorFullName || comment.authorUsername)[0]?.toUpperCase()}
      </div>
      <div className="min-w-0 flex-1">
        <div className="flex flex-wrap items-center gap-2">
          <span className="text-sm font-medium">
            {comment.authorFullName || comment.authorUsername}
          </span>
          <span className="text-xs text-muted-foreground">
            {formatDistanceToNow(new Date(comment.createdAt), { addSuffix: true, locale: vi })}
            {comment.updatedAt ? " · đã sửa" : ""}
          </span>
        </div>
        {editing ? (
          <div className="mt-2">
            <Textarea
              value={editingText}
              onChange={(event) => onEditingText(event.target.value)}
              maxLength={2000}
            />
            <div className="mt-2 flex justify-end gap-2">
              <Button size="sm" variant="ghost" onClick={onCancelEdit}>
                Hủy
              </Button>
              <Button size="sm" disabled={!editingText.trim()} onClick={onSave}>
                <Check className="mr-1 h-4 w-4" />
                Lưu
              </Button>
            </div>
          </div>
        ) : (
          <p className="mt-1 whitespace-pre-wrap break-words text-sm leading-6">
            <MentionText text={comment.content} />
          </p>
        )}
        <div className="mt-1 flex gap-1 opacity-100 sm:opacity-0 sm:group-hover:opacity-100">
          {canEdit && !editing && (
            <Button variant="ghost" size="sm" className="h-7 px-2 text-xs" onClick={onStartEdit}>
              <Pencil className="mr-1 h-3 w-3" />
              Sửa
            </Button>
          )}
          {canDelete && (
            <Button
              variant="ghost"
              size="sm"
              className="h-7 px-2 text-xs text-red-600"
              onClick={onDelete}
            >
              <Trash2 className="mr-1 h-3 w-3" />
              Xóa
            </Button>
          )}
        </div>
      </div>
    </div>
  );
}

function MentionText({ text }: { text: string }) {
  return (
    <>
      {text.split(/(@[\w.-]+)/g).map((part, index) =>
        part.startsWith("@") ? (
          <span key={`${part}-${index}`} className="font-medium text-blue-700">
            {part}
          </span>
        ) : (
          part
        ),
      )}
    </>
  );
}
