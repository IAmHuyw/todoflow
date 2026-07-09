import { useMemo, useState } from "react";
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
import { Badge } from "@/components/ui/badge";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { Trash2 } from "lucide-react";
import { toast } from "sonner";
import { useTodoStore } from "@/lib/todo-store";
import type { SharePermission } from "@/lib/todo-types";

interface Props {
  open: boolean;
  onOpenChange: (v: boolean) => void;
  taskId: string;
}

export function ShareDialog({ open, onOpenChange, taskId }: Props) {
  const [email, setEmail] = useState("");
  const [permission, setPermission] = useState<SharePermission>("view");

  const allShares = useTodoStore((s) => s.shares);
  const users = useTodoStore((s) => s.users);
  const shareTask = useTodoStore((s) => s.shareTask);
  const revoke = useTodoStore((s) => s.revokeShare);
  const changePerm = useTodoStore((s) => s.changeSharePermission);
  const shares = useMemo(
    () => allShares.filter((sh) => sh.taskId === taskId),
    [allShares, taskId],
  );

  const submit = async (e: React.FormEvent) => {
    e.preventDefault();
    const res = await shareTask(taskId, email, permission);
    if (!res.ok) return toast.error(res.error);
    toast.success("Đã gửi lời mời chia sẻ");
    setEmail("");
  };

  const updatePermission = async (shareId: string, value: SharePermission) => {
    try {
      await changePerm(shareId, value);
      toast.success("Đã cập nhật quyền");
    } catch (error) {
      toast.error(error instanceof Error ? error.message : "Không đổi được quyền");
    }
  };

  const removeShare = async (shareId: string) => {
    try {
      await revoke(shareId);
      toast.success("Đã thu hồi chia sẻ");
    } catch (error) {
      toast.error(error instanceof Error ? error.message : "Không thu hồi được chia sẻ");
    }
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Chia sẻ task</DialogTitle>
          <DialogDescription>
            Mời thành viên khác cộng tác. Thử với <span className="font-mono">alex</span>.
          </DialogDescription>
        </DialogHeader>
        <form onSubmit={submit} className="space-y-3">
          <div className="space-y-2">
            <Label>Email hoặc username</Label>
            <div className="flex gap-2">
              <Input
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                placeholder="alex"
                required
              />
              <Select
                value={permission}
                onValueChange={(v) => setPermission(v as SharePermission)}
              >
                <SelectTrigger className="w-28">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="view">View</SelectItem>
                  <SelectItem value="edit">Edit</SelectItem>
                </SelectContent>
              </Select>
              <Button type="submit">Mời</Button>
            </div>
          </div>
        </form>
        <div className="space-y-2">
          <Label>Đang chia sẻ với</Label>
          {shares.length === 0 && (
            <p className="text-sm text-muted-foreground">Chưa chia sẻ cho ai.</p>
          )}
          {shares.map((sh) => {
            const u = users.find((x) => x.id === sh.sharedWithUserId);
            const username = sh.sharedWithUsername ?? u?.username ?? "User";
            const email = sh.sharedWithEmail ?? u?.email ?? "";
            return (
              <div
                key={sh.id}
                className="flex items-center justify-between rounded-md border border-border bg-card p-2"
              >
                <div className="flex items-center gap-2">
                  <div className="flex h-7 w-7 items-center justify-center rounded-full bg-primary/10 text-xs font-medium text-primary">
                    {username[0]?.toUpperCase()}
                  </div>
                  <div>
                    <div className="text-sm font-medium">{username}</div>
                    <div className="text-xs text-muted-foreground">{email}</div>
                  </div>
                </div>
                <div className="flex items-center gap-2">
                  <Badge
                    variant={
                      sh.status === "accepted"
                        ? "default"
                        : sh.status === "rejected"
                          ? "destructive"
                          : "secondary"
                    }
                    className="text-xs"
                  >
                    {sh.status}
                  </Badge>
                  <Select
                    value={sh.permission}
                    onValueChange={(v) =>
                      void updatePermission(sh.id, v as SharePermission)
                    }
                  >
                    <SelectTrigger className="h-8 w-24 text-xs">
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="view">View</SelectItem>
                      <SelectItem value="edit">Edit</SelectItem>
                    </SelectContent>
                  </Select>
                  <Button
                    variant="ghost"
                    size="icon"
                    className="h-8 w-8"
                    onClick={() => void removeShare(sh.id)}
                  >
                    <Trash2 className="h-3.5 w-3.5" />
                  </Button>
                </div>
              </div>
            );
          })}
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
