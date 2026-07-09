import { createFileRoute } from "@tanstack/react-router";
import { useMemo, useState } from "react";
import { Plus, X } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Badge } from "@/components/ui/badge";
import { useTodoStore } from "@/lib/todo-store";
import { toast } from "sonner";

export const Route = createFileRoute("/_authenticated/tags")({
  component: TagsPage,
  head: () => ({ meta: [{ title: "Nhãn — TodoFlow" }] }),
});

function TagsPage() {
  const userId = useTodoStore((s) => s.currentUserId);
  const allTags = useTodoStore((s) => s.tags);
  const tasks = useTodoStore((s) => s.tasks);
  const addTag = useTodoStore((s) => s.addTag);
  const deleteTag = useTodoStore((s) => s.deleteTag);

  const [name, setName] = useState("");
  const tags = useMemo(
    () => allTags.filter((t) => t.userId === userId),
    [allTags, userId],
  );

  const submit = async (e: React.FormEvent) => {
    e.preventDefault();
    const clean = name.trim().replace(/\s+/g, "-").toLowerCase();
    if (!clean) return;
    try {
      await addTag(clean);
      setName("");
      toast.success("Đã thêm nhãn");
    } catch (error) {
      toast.error(error instanceof Error ? error.message : "Không thêm được nhãn");
    }
  };

  const removeTag = async (id: string) => {
    try {
      await deleteTag(id);
      toast.success("Đã xoá nhãn");
    } catch (error) {
      toast.error(error instanceof Error ? error.message : "Không xoá được nhãn");
    }
  };

  return (
    <div className="mx-auto max-w-3xl p-6">
      <div className="mb-6">
        <h1 className="text-2xl font-semibold tracking-tight">Nhãn</h1>
        <p className="text-sm text-muted-foreground">Gắn nhãn nhanh cho công việc.</p>
      </div>
      <form onSubmit={submit} className="mb-6 flex gap-2">
        <Input
          value={name}
          onChange={(e) => setName(e.target.value)}
          placeholder="Tên nhãn (VD: khan-cap, y-tuong)"
        />
        <Button type="submit">
          <Plus className="mr-2 h-4 w-4" />
          Thêm
        </Button>
      </form>
      <div className="flex flex-wrap gap-2">
        {tags.length === 0 && (
          <p className="text-sm text-muted-foreground">Chưa có nhãn.</p>
        )}
        {tags.map((t) => {
          const count = tasks.filter(
            (x) => x.tagIds.includes(t.id) && !x.isDeleted,
          ).length;
          return (
            <Badge
              key={t.id}
              variant="secondary"
              className="gap-2 py-1.5 pl-3 pr-1.5 text-sm font-normal"
            >
              #{t.name}
              <span className="text-xs text-muted-foreground">({count})</span>
              <button
                type="button"
                onClick={() => void removeTag(t.id)}
                className="rounded-full p-0.5 hover:bg-background"
              >
                <X className="h-3 w-3" />
              </button>
            </Badge>
          );
        })}
      </div>
    </div>
  );
}
