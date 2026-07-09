import { createFileRoute } from "@tanstack/react-router";
import { useMemo, useState } from "react";
import { Plus, Trash2, Pencil } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import {
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { useTodoStore } from "@/lib/todo-store";
import type { Category } from "@/lib/todo-types";
import { toast } from "sonner";

export const Route = createFileRoute("/_authenticated/categories")({
  component: CategoriesPage,
  head: () => ({ meta: [{ title: "Categories — TodoFlow" }] }),
});

const COLORS = ["#3b82f6", "#10b981", "#f59e0b", "#ef4444", "#8b5cf6", "#ec4899"];

function CategoriesPage() {
  const userId = useTodoStore((s) => s.currentUserId);
  const allCategories = useTodoStore((s) => s.categories);
  const tasks = useTodoStore((s) => s.tasks);
  const addCategory = useTodoStore((s) => s.addCategory);
  const updateCategory = useTodoStore((s) => s.updateCategory);
  const deleteCategory = useTodoStore((s) => s.deleteCategory);

  const [editing, setEditing] = useState<Category | null>(null);
  const [open, setOpen] = useState(false);
  const [name, setName] = useState("");
  const [color, setColor] = useState(COLORS[0]);
  const categories = useMemo(
    () => allCategories.filter((c) => c.userId === userId),
    [allCategories, userId],
  );

  const openNew = () => {
    setEditing(null);
    setName("");
    setColor(COLORS[0]);
    setOpen(true);
  };
  const openEdit = (c: Category) => {
    setEditing(c);
    setName(c.name);
    setColor(c.color);
    setOpen(true);
  };
  const save = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!name.trim()) return;
    try {
      if (editing) await updateCategory(editing.id, { name: name.trim(), color });
      else await addCategory(name.trim(), color);
      setOpen(false);
      toast.success(editing ? "Đã cập nhật category" : "Đã tạo category");
    } catch (error) {
      toast.error(error instanceof Error ? error.message : "Không lưu được category");
    }
  };

  const removeCategory = async (id: string) => {
    try {
      await deleteCategory(id);
      toast.success("Đã xoá category");
    } catch (error) {
      toast.error(error instanceof Error ? error.message : "Không xoá được category");
    }
  };

  return (
    <div className="mx-auto max-w-3xl p-6">
      <div className="mb-6 flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight">Categories</h1>
          <p className="text-sm text-muted-foreground">Phân loại task theo nhóm.</p>
        </div>
        <Button onClick={openNew}>
          <Plus className="mr-2 h-4 w-4" /> Category mới
        </Button>
      </div>

      <div className="space-y-2">
        {categories.length === 0 && (
          <div className="rounded-lg border border-dashed border-border p-8 text-center text-sm text-muted-foreground">
            Chưa có category nào.
          </div>
        )}
        {categories.map((c) => {
          const count = tasks.filter(
            (t) => t.categoryId === c.id && !t.isDeleted,
          ).length;
          return (
            <div
              key={c.id}
              className="flex items-center justify-between rounded-xl border border-border bg-card p-3"
            >
              <div className="flex items-center gap-3">
                <span
                  className="h-4 w-4 rounded-full"
                  style={{ backgroundColor: c.color }}
                />
                <div>
                  <div className="font-medium">{c.name}</div>
                  <div className="text-xs text-muted-foreground">{count} task</div>
                </div>
              </div>
              <div className="flex gap-1">
                <Button variant="ghost" size="icon" onClick={() => openEdit(c)}>
                  <Pencil className="h-4 w-4" />
                </Button>
                <Button
                  variant="ghost"
                  size="icon"
                  onClick={() => void removeCategory(c.id)}
                >
                  <Trash2 className="h-4 w-4" />
                </Button>
              </div>
            </div>
          );
        })}
      </div>

      <Dialog open={open} onOpenChange={setOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>
              {editing ? "Chỉnh sửa category" : "Category mới"}
            </DialogTitle>
          </DialogHeader>
          <form onSubmit={save} className="space-y-4">
            <div className="space-y-2">
              <Label htmlFor="cname">Tên</Label>
              <Input
                id="cname"
                value={name}
                onChange={(e) => setName(e.target.value)}
                required
              />
            </div>
            <div className="space-y-2">
              <Label>Màu</Label>
              <div className="flex gap-2">
                {COLORS.map((col) => (
                  <button
                    key={col}
                    type="button"
                    onClick={() => setColor(col)}
                    className="h-8 w-8 rounded-full border-2 transition-transform"
                    style={{
                      backgroundColor: col,
                      borderColor: color === col ? col : "transparent",
                      transform: color === col ? "scale(1.1)" : "scale(1)",
                    }}
                  />
                ))}
              </div>
            </div>
            <DialogFooter>
              <Button type="button" variant="ghost" onClick={() => setOpen(false)}>
                Huỷ
              </Button>
              <Button type="submit">Lưu</Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>
    </div>
  );
}
