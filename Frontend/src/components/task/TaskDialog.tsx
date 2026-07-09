import { useEffect, useMemo, useState } from "react";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { Badge } from "@/components/ui/badge";
import { X } from "lucide-react";
import { useTodoStore } from "@/lib/todo-store";
import type { Priority, RecurrenceType, Status, Task } from "@/lib/todo-types";
import { toast } from "sonner";

interface Props {
  open: boolean;
  onOpenChange: (v: boolean) => void;
  task?: Task;
  readOnly?: boolean;
}

export function TaskDialog({ open, onOpenChange, task, readOnly }: Props) {
  const userId = useTodoStore((s) => s.currentUserId);
  const allCategories = useTodoStore((s) => s.categories);
  const allTags = useTodoStore((s) => s.tags);
  const addTask = useTodoStore((s) => s.addTask);
  const updateTask = useTodoStore((s) => s.updateTask);

  const [title, setTitle] = useState("");
  const [description, setDescription] = useState("");
  const [priority, setPriority] = useState<Priority>("medium");
  const [status, setStatus] = useState<Status>("todo");
  const [categoryId, setCategoryId] = useState<string>("none");
  const [dueDate, setDueDate] = useState("");
  const [recurrenceType, setRecurrenceType] = useState<RecurrenceType>("none");
  const [recurrenceInterval, setRecurrenceInterval] = useState(1);
  const [recurrenceEndDate, setRecurrenceEndDate] = useState("");
  const [tagIds, setTagIds] = useState<string[]>([]);
  const categories = useMemo(
    () => allCategories.filter((c) => c.userId === userId),
    [allCategories, userId],
  );
  const tags = useMemo(
    () => allTags.filter((t) => t.userId === userId),
    [allTags, userId],
  );

  useEffect(() => {
    if (open) {
      setTitle(task?.title ?? "");
      setDescription(task?.description ?? "");
      setPriority(task?.priority ?? "medium");
      setStatus(task?.status ?? "todo");
      setCategoryId(task?.categoryId ?? "none");
      setDueDate(task?.dueDate ? task.dueDate.slice(0, 10) : "");
      setRecurrenceType(task?.recurrenceType ?? "none");
      setRecurrenceInterval(task?.recurrenceInterval ?? 1);
      setRecurrenceEndDate(
        task?.recurrenceEndDate ? task.recurrenceEndDate.slice(0, 10) : "",
      );
      setTagIds(task?.tagIds ?? []);
    }
  }, [open, task]);

  const submit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!title.trim()) return;
    const payload = {
      title: title.trim(),
      description: description.trim(),
      priority,
      status,
      categoryId: categoryId === "none" ? null : categoryId,
      dueDate: dueDate ? new Date(dueDate).toISOString() : null,
      recurrenceType,
      recurrenceInterval: recurrenceType === "none" ? 1 : recurrenceInterval,
      recurrenceEndDate:
        recurrenceType === "none" || !recurrenceEndDate
          ? null
          : new Date(recurrenceEndDate).toISOString(),
      tagIds,
    };
    try {
      if (task) await updateTask(task.id, payload);
      else await addTask(payload);
      onOpenChange(false);
    } catch (error) {
      toast.error(error instanceof Error ? error.message : "Không lưu được công việc");
    }
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-lg">
        <DialogHeader>
          <DialogTitle>{task ? "Chỉnh sửa công việc" : "Tạo công việc mới"}</DialogTitle>
          <DialogDescription>
            {readOnly
              ? "Công việc được chia sẻ, bạn chỉ chỉnh sửa được khi có quyền sửa."
              : "Nhập thông tin công việc của bạn."}
          </DialogDescription>
        </DialogHeader>
        <form onSubmit={submit} className="space-y-4">
          <div className="space-y-2">
            <Label htmlFor="title">Tiêu đề</Label>
            <Input
              id="title"
              value={title}
              onChange={(e) => setTitle(e.target.value)}
              placeholder="VD: Viết README cho dự án"
              required
            />
          </div>
          <div className="space-y-2">
            <Label htmlFor="desc">Mô tả</Label>
            <Textarea
              id="desc"
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              rows={3}
            />
          </div>
          <div className="grid grid-cols-2 gap-3">
            <div className="space-y-2">
              <Label>Độ ưu tiên</Label>
              <Select value={priority} onValueChange={(v) => setPriority(v as Priority)}>
                <SelectTrigger>
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="low">Thấp</SelectItem>
                  <SelectItem value="medium">Trung bình</SelectItem>
                  <SelectItem value="high">Cao</SelectItem>
                </SelectContent>
              </Select>
            </div>
            <div className="space-y-2">
              <Label>Trạng thái</Label>
              <Select value={status} onValueChange={(v) => setStatus(v as Status)}>
                <SelectTrigger>
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="todo">Cần làm</SelectItem>
                  <SelectItem value="in_progress">Đang làm</SelectItem>
                  <SelectItem value="done">Xong</SelectItem>
                </SelectContent>
              </Select>
            </div>
            <div className="space-y-2">
              <Label>Danh mục</Label>
              <Select value={categoryId} onValueChange={setCategoryId}>
                <SelectTrigger>
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="none">Không có</SelectItem>
                  {categories.map((c) => (
                    <SelectItem key={c.id} value={c.id}>
                      {c.name}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
            <div className="space-y-2">
              <Label htmlFor="due">Hạn làm</Label>
              <Input
                id="due"
                type="date"
                value={dueDate}
                onChange={(e) => setDueDate(e.target.value)}
              />
            </div>
          </div>
          <div className="grid grid-cols-3 gap-3">
            <div className="space-y-2">
              <Label>Lặp lại</Label>
              <Select
                value={recurrenceType}
                onValueChange={(v) => setRecurrenceType(v as RecurrenceType)}
              >
                <SelectTrigger>
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="none">Không lặp</SelectItem>
                  <SelectItem value="daily">Hàng ngày</SelectItem>
                  <SelectItem value="weekly">Hàng tuần</SelectItem>
                  <SelectItem value="monthly">Hàng tháng</SelectItem>
                </SelectContent>
              </Select>
            </div>
            <div className="space-y-2">
              <Label htmlFor="repeat-interval">Mỗi</Label>
              <Input
                id="repeat-interval"
                type="number"
                min={1}
                max={365}
                value={recurrenceInterval}
                disabled={recurrenceType === "none"}
                onChange={(e) =>
                  setRecurrenceInterval(Math.max(1, Number(e.target.value) || 1))
                }
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="repeat-end">Kết thúc</Label>
              <Input
                id="repeat-end"
                type="date"
                value={recurrenceEndDate}
                disabled={recurrenceType === "none"}
                onChange={(e) => setRecurrenceEndDate(e.target.value)}
              />
            </div>
          </div>
          {tags.length > 0 && (
            <div className="space-y-2">
              <Label>Nhãn</Label>
              <div className="flex flex-wrap gap-1.5">
                {tags.map((t) => {
                  const active = tagIds.includes(t.id);
                  return (
                    <button
                      key={t.id}
                      type="button"
                      onClick={() =>
                        setTagIds((prev) =>
                          active ? prev.filter((x) => x !== t.id) : [...prev, t.id],
                        )
                      }
                    >
                      <Badge
                        variant={active ? "default" : "outline"}
                        className="cursor-pointer"
                      >
                        {active && <X className="mr-1 h-3 w-3" />}#{t.name}
                      </Badge>
                    </button>
                  );
                })}
              </div>
            </div>
          )}
          <DialogFooter>
            <Button type="button" variant="ghost" onClick={() => onOpenChange(false)}>
              Huỷ
            </Button>
            <Button type="submit">{task ? "Lưu" : "Tạo công việc"}</Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
