import { useEffect, useMemo, useState } from "react";
import { format } from "date-fns";
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
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { Trash2 } from "lucide-react";
import { useTodoStore } from "@/lib/todo-store";
import type { ReminderChannel } from "@/lib/todo-types";
import { toast } from "sonner";

interface Props {
  open: boolean;
  onOpenChange: (v: boolean) => void;
  taskId: string;
}

export function ReminderDialog({ open, onOpenChange, taskId }: Props) {
  const [remindDate, setRemindDate] = useState("");
  const [remindTime, setRemindTime] = useState("");
  const [channel, setChannel] = useState<ReminderChannel>("in_app");

  const allReminders = useTodoStore((s) => s.reminders);
  const add = useTodoStore((s) => s.addReminder);
  const del = useTodoStore((s) => s.deleteReminder);
  const reminders = useMemo(
    () => allReminders.filter((r) => r.taskId === taskId),
    [allReminders, taskId],
  );

  useEffect(() => {
    if (!open || (remindDate && remindTime)) return;

    const next = new Date(Date.now() + 5 * 60 * 1000);
    setRemindDate(formatDateInput(next));
    setRemindTime(formatTimeInput(next));
  }, [open, remindDate, remindTime]);

  const submit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!remindDate || !remindTime) {
      toast.error("Vui lòng chọn đầy đủ ngày và giờ nhắc nhở.");
      return;
    }

    const remindAt = parseReminderDateTime(remindDate, remindTime);
    if (Number.isNaN(remindAt.getTime())) {
      toast.error("Thời điểm nhắc nhở không hợp lệ.");
      return;
    }

    try {
      await add(taskId, remindAt.toISOString(), channel);
      setRemindDate("");
      setRemindTime("");
      toast.success("Đã đặt nhắc nhở");
    } catch (error) {
      toast.error(error instanceof Error ? error.message : "Không đặt được nhắc nhở");
    }
  };

  const removeReminder = async (id: string) => {
    try {
      await del(id);
      toast.success("Đã xoá nhắc nhở");
    } catch (error) {
      toast.error(error instanceof Error ? error.message : "Không xoá được nhắc nhở");
    }
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Đặt nhắc nhở</DialogTitle>
          <DialogDescription>
            Chọn thời điểm và kênh nhận thông báo.
          </DialogDescription>
        </DialogHeader>
        <form onSubmit={submit} className="space-y-3">
          <div className="space-y-2">
            <Label>Thời điểm</Label>
            <div className="grid gap-2 sm:grid-cols-[1fr_120px_128px_auto]">
              <Input
                type="date"
                value={remindDate}
                onChange={(e) => setRemindDate(e.target.value)}
                aria-label="Ngày nhắc nhở"
              />
              <Input
                type="time"
                value={remindTime}
                onChange={(e) => setRemindTime(e.target.value)}
                aria-label="Giờ nhắc nhở"
              />
              <Select
                value={channel}
                onValueChange={(v) => setChannel(v as ReminderChannel)}
              >
                <SelectTrigger>
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="in_app">Trong ứng dụng</SelectItem>
                  <SelectItem value="email">Email</SelectItem>
                  <SelectItem value="both">Cả hai</SelectItem>
                </SelectContent>
              </Select>
              <Button type="submit">Thêm</Button>
            </div>
          </div>
        </form>
        <div className="space-y-2">
          <Label>Nhắc nhở đã đặt</Label>
          {reminders.length === 0 && (
            <p className="text-sm text-muted-foreground">Chưa có nhắc nhở.</p>
          )}
          {reminders.map((r) => (
            <div
              key={r.id}
              className="flex items-center justify-between rounded-md border border-border bg-card p-2 text-sm"
            >
              <div>
                <div className="font-medium">
                  {format(new Date(r.remindAt), "dd MMM yyyy, HH:mm")}
                </div>
                <div className="text-xs text-muted-foreground">
                  Kênh: {channelLabel[r.channel]} {r.isSent && "· đã gửi"}
                </div>
              </div>
              <Button
                variant="ghost"
                size="icon"
                className="h-8 w-8"
                onClick={() => void removeReminder(r.id)}
              >
                <Trash2 className="h-3.5 w-3.5" />
              </Button>
            </div>
          ))}
        </div>
        <DialogFooter>
          <Button variant="ghost" onClick={() => onOpenChange(false)}>
            Đóng
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

const channelLabel = {
  in_app: "Trong ứng dụng",
  email: "Email",
  both: "Cả hai",
};

function formatDateInput(date: Date) {
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, "0");
  const day = String(date.getDate()).padStart(2, "0");
  return `${year}-${month}-${day}`;
}

function formatTimeInput(date: Date) {
  const hours = String(date.getHours()).padStart(2, "0");
  const minutes = String(date.getMinutes()).padStart(2, "0");
  return `${hours}:${minutes}`;
}

function parseReminderDateTime(dateValue: string, timeValue: string) {
  const normalizedDate = dateValue.includes("/")
    ? normalizeSlashDate(dateValue)
    : dateValue;

  return new Date(`${normalizedDate}T${timeValue}:00`);
}

function normalizeSlashDate(value: string) {
  const [day, month, year] = value.split("/");
  return `${year}-${month.padStart(2, "0")}-${day.padStart(2, "0")}`;
}
