import { useState, useMemo } from "react";
import { format, isPast, isToday } from "date-fns";
import {
  Calendar as CalendarIcon,
  Check,
  MoreHorizontal,
  Pencil,
  Plus,
  Repeat,
  Share2,
  Trash2,
  Bell,
  ChevronDown,
  ChevronRight,
} from "lucide-react";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Checkbox } from "@/components/ui/checkbox";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { Input } from "@/components/ui/input";
import { cn } from "@/lib/utils";
import { useTodoStore } from "@/lib/todo-store";
import type { Task, Priority, Status } from "@/lib/todo-types";
import { TaskDialog } from "./TaskDialog";
import { ShareDialog } from "./ShareDialog";
import { ReminderDialog } from "./ReminderDialog";
import { toast } from "sonner";

const priorityColor: Record<Priority, string> = {
  low: "bg-slate-100 text-slate-700 border-slate-200",
  medium: "bg-amber-100 text-amber-800 border-amber-200",
  high: "bg-red-100 text-red-800 border-red-200",
};

const statusLabel: Record<Status, string> = {
  todo: "Cần làm",
  in_progress: "Đang làm",
  done: "Xong",
};

const priorityLabel: Record<Priority, string> = {
  low: "Thấp",
  medium: "Trung bình",
  high: "Cao",
};

const recurrenceLabel = {
  none: "",
  daily: "Lặp ngày",
  weekly: "Lặp tuần",
  monthly: "Lặp tháng",
};

export function TaskCard({ task, isShared }: { task: Task; isShared?: boolean }) {
  const [expanded, setExpanded] = useState(false);
  const [editing, setEditing] = useState(false);
  const [sharing, setSharing] = useState(false);
  const [reminding, setReminding] = useState(false);
  const [newSub, setNewSub] = useState("");
  const [newSubNote, setNewSubNote] = useState("");
  const [editingSubId, setEditingSubId] = useState<string | null>(null);
  const [editingSubNote, setEditingSubNote] = useState("");

  const category = useTodoStore((s) =>
    s.categories.find((c) => c.id === task.categoryId),
  );
  const allTags = useTodoStore((s) => s.tags);
  const allSubtasks = useTodoStore((s) => s.subtasks);
  const setStatus = useTodoStore((s) => s.setTaskStatus);
  const deleteTask = useTodoStore((s) => s.deleteTask);
  const addSubtask = useTodoStore((s) => s.addSubtask);
  const updateSubtask = useTodoStore((s) => s.updateSubtask);
  const toggleSubtask = useTodoStore((s) => s.toggleSubtask);
  const deleteSubtask = useTodoStore((s) => s.deleteSubtask);

  const done = task.status === "done";
  const tags = useMemo(
    () => allTags.filter((t) => task.tagIds.includes(t.id)),
    [allTags, task.tagIds],
  );
  const subs = useMemo(
    () => allSubtasks.filter((x) => x.taskId === task.id),
    [allSubtasks, task.id],
  );
  const subDone = subs.filter((s) => s.isCompleted).length;

  const runAction = async (action: Promise<void>, fallback: string) => {
    try {
      await action;
    } catch (error) {
      toast.error(error instanceof Error ? error.message : fallback);
    }
  };

  const dueBadge = useMemo(() => {
    if (!task.dueDate) return null;
    const d = new Date(task.dueDate);
    const overdue = isPast(d) && !isToday(d) && !done;
    const today = isToday(d);
    return (
      <span
        className={cn(
          "inline-flex items-center gap-1 rounded-md border px-1.5 py-0.5 text-xs",
          overdue
            ? "border-red-200 bg-red-50 text-red-700"
            : today
              ? "border-blue-200 bg-blue-50 text-blue-700"
              : "border-border bg-muted text-muted-foreground",
        )}
      >
        <CalendarIcon className="h-3 w-3" />
        {format(d, "dd MMM")}
      </span>
    );
  }, [task.dueDate, done]);

  return (
    <>
      <div
        className={cn(
          "group rounded-xl border border-border bg-card p-4 transition-shadow hover:shadow-sm",
          done && "opacity-70",
        )}
      >
        <div className="flex items-start gap-3">
          <Checkbox
            checked={done}
            onCheckedChange={(v) =>
              void runAction(
                setStatus(task.id, v === true ? "done" : "todo"),
                "Không đổi được trạng thái công việc",
              )
            }
            className="mt-1"
          />
          <div className="min-w-0 flex-1">
            <div className="flex items-start justify-between gap-2">
              <button
                onClick={() => setEditing(true)}
                className={cn(
                  "text-left text-sm font-medium leading-tight text-foreground hover:text-primary",
                  done && "line-through text-muted-foreground",
                )}
              >
                {task.title}
              </button>
              <div className="flex items-center gap-1 opacity-0 transition-opacity group-hover:opacity-100">
                <Button
                  variant="ghost"
                  size="icon"
                  className="h-7 w-7"
                  onClick={() => setReminding(true)}
                  title="Nhắc nhở"
                >
                  <Bell className="h-3.5 w-3.5" />
                </Button>
                {!isShared && (
                  <Button
                    variant="ghost"
                    size="icon"
                    className="h-7 w-7"
                    onClick={() => setSharing(true)}
                    title="Chia sẻ"
                  >
                    <Share2 className="h-3.5 w-3.5" />
                  </Button>
                )}
                <DropdownMenu>
                  <DropdownMenuTrigger asChild>
                    <Button variant="ghost" size="icon" className="h-7 w-7">
                      <MoreHorizontal className="h-3.5 w-3.5" />
                    </Button>
                  </DropdownMenuTrigger>
                  <DropdownMenuContent align="end">
                    <DropdownMenuItem onClick={() => setEditing(true)}>
                      Chỉnh sửa
                    </DropdownMenuItem>
                    <DropdownMenuItem
                      onClick={() =>
                        void runAction(
                          setStatus(task.id, "todo"),
                          "Không đổi được trạng thái công việc",
                        )
                      }
                    >
                      Đánh dấu: Cần làm
                    </DropdownMenuItem>
                    <DropdownMenuItem
                      onClick={() =>
                        void runAction(
                          setStatus(task.id, "in_progress"),
                          "Không đổi được trạng thái công việc",
                        )
                      }
                    >
                      Đánh dấu: Đang làm
                    </DropdownMenuItem>
                    <DropdownMenuItem
                      onClick={() =>
                        void runAction(
                          setStatus(task.id, "done"),
                          "Không đổi được trạng thái công việc",
                        )
                      }
                    >
                      Đánh dấu: Xong
                    </DropdownMenuItem>
                    {!isShared && (
                      <>
                        <DropdownMenuSeparator />
                        <DropdownMenuItem
                          className="text-red-600 focus:text-red-600"
                          onClick={() =>
                            void runAction(deleteTask(task.id), "Không xoá được công việc")
                          }
                        >
                          <Trash2 className="mr-2 h-3.5 w-3.5" />
                          Xoá
                        </DropdownMenuItem>
                      </>
                    )}
                  </DropdownMenuContent>
                </DropdownMenu>
              </div>
            </div>

            {task.description && (
              <p className="mt-1 text-xs text-muted-foreground line-clamp-2">
                {task.description}
              </p>
            )}

            <div className="mt-3 flex flex-wrap items-center gap-1.5">
              {category && (
                <span
                  className="inline-flex items-center gap-1 rounded-md border px-1.5 py-0.5 text-xs"
                  style={{
                    borderColor: category.color + "40",
                    backgroundColor: category.color + "15",
                    color: category.color,
                  }}
                >
                  <span
                    className="h-1.5 w-1.5 rounded-full"
                    style={{ backgroundColor: category.color }}
                  />
                  {category.name}
                </span>
              )}
              <span
                className={cn(
                  "inline-flex rounded-md border px-1.5 py-0.5 text-xs capitalize",
                  priorityColor[task.priority],
                )}
              >
                {priorityLabel[task.priority]}
              </span>
              <Badge variant="outline" className="text-xs font-normal">
                {statusLabel[task.status]}
              </Badge>
              {dueBadge}
              {task.recurrenceType !== "none" && (
                <span className="inline-flex items-center gap-1 rounded-md border border-emerald-200 bg-emerald-50 px-1.5 py-0.5 text-xs text-emerald-700">
                  <Repeat className="h-3 w-3" />
                  {recurrenceLabel[task.recurrenceType]}
                </span>
              )}
              {tags.map((t) => (
                <Badge key={t.id} variant="secondary" className="text-xs font-normal">
                  #{t.name}
                </Badge>
              ))}
              {subs.length > 0 && (
                <button
                  onClick={() => setExpanded((v) => !v)}
                  className="inline-flex items-center gap-1 rounded-md border border-border bg-muted px-1.5 py-0.5 text-xs text-muted-foreground hover:bg-accent"
                >
                  {expanded ? (
                    <ChevronDown className="h-3 w-3" />
                  ) : (
                    <ChevronRight className="h-3 w-3" />
                  )}
                  {subDone}/{subs.length} việc con
                </button>
              )}
            </div>

            {(expanded || (subs.length === 0 && !isShared)) && (
              <div className="mt-3 space-y-1.5 border-t border-border pt-3">
                {subs.map((s) => (
                  <div key={s.id} className="group/sub flex items-start gap-2">
                    <Checkbox
                      checked={s.isCompleted}
                      onCheckedChange={() =>
                        void runAction(
                          toggleSubtask(s.id),
                          "Không cập nhật được việc con",
                        )
                      }
                      className="mt-0.5"
                    />
                    <div className="min-w-0 flex-1">
                      <span
                        className={cn(
                          "block text-sm",
                          s.isCompleted && "line-through text-muted-foreground",
                        )}
                      >
                        {s.title}
                      </span>
                      {editingSubId === s.id ? (
                        <form
                          className="mt-1 flex items-center gap-2"
                          onSubmit={(e) => {
                            e.preventDefault();
                            void runAction(
                              updateSubtask(s.id, { note: editingSubNote }).then(() => {
                                setEditingSubId(null);
                                setEditingSubNote("");
                              }),
                              "Không cập nhật được ghi chú việc con",
                            );
                          }}
                        >
                          <Input
                            value={editingSubNote}
                            onChange={(e) => setEditingSubNote(e.target.value)}
                            placeholder="Ghi chú việc con..."
                            className="h-7 text-xs"
                          />
                          <Button type="submit" size="icon" className="h-7 w-7">
                            <Check className="h-3.5 w-3.5" />
                          </Button>
                        </form>
                      ) : (
                        s.note && (
                          <button
                            type="button"
                            className="mt-0.5 block max-w-full truncate text-left text-xs text-muted-foreground hover:text-foreground"
                            onClick={() => {
                              setEditingSubId(s.id);
                              setEditingSubNote(s.note);
                            }}
                          >
                            {s.note}
                          </button>
                        )
                      )}
                    </div>
                    {editingSubId !== s.id && (
                      <Button
                        variant="ghost"
                        size="icon"
                        className="h-6 w-6 opacity-0 group-hover/sub:opacity-100"
                        onClick={() => {
                          setEditingSubId(s.id);
                          setEditingSubNote(s.note);
                        }}
                        title="Ghi chú việc con"
                      >
                        <Pencil className="h-3 w-3" />
                      </Button>
                    )}
                    <Button
                      variant="ghost"
                      size="icon"
                      className="h-6 w-6 opacity-0 group-hover/sub:opacity-100"
                      onClick={() =>
                        void runAction(deleteSubtask(s.id), "Không xoá được việc con")
                      }
                    >
                      <Trash2 className="h-3 w-3" />
                    </Button>
                  </div>
                ))}
                {!isShared && (
                  <form
                    onSubmit={(e) => {
                      e.preventDefault();
                      const title = newSub.trim();
                      if (!title) return;
                      void runAction(
                        addSubtask(task.id, title, newSubNote.trim()).then(() => {
                          setNewSub("");
                          setNewSubNote("");
                          setExpanded(true);
                        }),
                        "Không thêm được việc con",
                      );
                    }}
                    className="space-y-2"
                  >
                    <div className="flex items-center gap-2">
                      <Plus className="h-3.5 w-3.5 text-muted-foreground" />
                      <Input
                        value={newSub}
                        onChange={(e) => setNewSub(e.target.value)}
                        placeholder="Thêm việc con..."
                        className="h-7 border-none px-0 text-sm shadow-none focus-visible:ring-0"
                      />
                    </div>
                    {newSub.trim() && (
                      <Input
                        value={newSubNote}
                        onChange={(e) => setNewSubNote(e.target.value)}
                        placeholder="Ghi chú cho việc con..."
                        className="ml-5 h-7 text-xs"
                      />
                    )}
                  </form>
                )}
              </div>
            )}
          </div>
        </div>
      </div>

      <TaskDialog
        open={editing}
        onOpenChange={setEditing}
        task={task}
        readOnly={isShared}
      />
      <ShareDialog open={sharing} onOpenChange={setSharing} taskId={task.id} />
      <ReminderDialog open={reminding} onOpenChange={setReminding} taskId={task.id} />
    </>
  );
}
