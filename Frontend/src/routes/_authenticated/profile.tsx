import { createFileRoute } from "@tanstack/react-router";
import { useEffect, useState } from "react";
import { Save } from "lucide-react";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { useCurrentUser, useTodoStore } from "@/lib/todo-store";

export const Route = createFileRoute("/_authenticated/profile")({
  component: ProfilePage,
  head: () => ({ meta: [{ title: "Hồ sơ — TodoFlow" }] }),
});

function ProfilePage() {
  const user = useCurrentUser();
  const updateProfile = useTodoStore((state) => state.updateProfile);
  const [fullName, setFullName] = useState("");
  const [phoneNumber, setPhoneNumber] = useState("");
  const [dateOfBirth, setDateOfBirth] = useState("");
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    if (!user) return;
    setFullName(user.fullName ?? "");
    setPhoneNumber(user.phoneNumber ?? "");
    setDateOfBirth(user.dateOfBirth ?? "");
  }, [user]);

  if (!user) return null;

  const submit = async (event: React.FormEvent) => {
    event.preventDefault();
    setSaving(true);
    try {
      await updateProfile({
        fullName,
        phoneNumber,
        dateOfBirth: dateOfBirth || null,
      });
      toast.success("Đã cập nhật hồ sơ");
    } catch (error) {
      toast.error(error instanceof Error ? error.message : "Không cập nhật được hồ sơ");
    } finally {
      setSaving(false);
    }
  };

  return (
    <div className="mx-auto max-w-3xl p-6">
      <div className="mb-8">
        <h1 className="text-2xl font-semibold tracking-tight">Hồ sơ cá nhân</h1>
        <p className="text-sm text-muted-foreground">
          Quản lý thông tin cá nhân được dùng trong TodoFlow.
        </p>
      </div>

      <form onSubmit={submit} className="space-y-8">
        <section className="grid gap-5 border-t border-border pt-6 sm:grid-cols-2">
          <div className="space-y-2 sm:col-span-2">
            <h2 className="text-base font-semibold">Thông tin tài khoản</h2>
            <p className="text-sm text-muted-foreground">
              Username và email hiện không thể thay đổi.
            </p>
          </div>
          <div className="space-y-2">
            <Label htmlFor="profile-username">Tên đăng nhập</Label>
            <Input id="profile-username" value={user.username} readOnly className="bg-muted" />
          </div>
          <div className="space-y-2">
            <Label htmlFor="profile-email">Email</Label>
            <Input id="profile-email" value={user.email} readOnly className="bg-muted" />
          </div>
        </section>

        <section className="grid gap-5 border-t border-border pt-6 sm:grid-cols-2">
          <div className="space-y-2 sm:col-span-2">
            <h2 className="text-base font-semibold">Thông tin cá nhân</h2>
          </div>
          <div className="space-y-2 sm:col-span-2">
            <Label htmlFor="profile-full-name">Họ và tên</Label>
            <Input
              id="profile-full-name"
              value={fullName}
              onChange={(event) => setFullName(event.target.value)}
              maxLength={100}
              autoComplete="name"
              placeholder="Nguyễn Văn A"
            />
          </div>
          <div className="space-y-2">
            <Label htmlFor="profile-phone">Số điện thoại</Label>
            <Input
              id="profile-phone"
              type="tel"
              value={phoneNumber}
              onChange={(event) => setPhoneNumber(event.target.value)}
              maxLength={24}
              autoComplete="tel"
              placeholder="+84 901 234 567"
            />
          </div>
          <div className="space-y-2">
            <Label htmlFor="profile-date-of-birth">Ngày sinh</Label>
            <Input
              id="profile-date-of-birth"
              type="date"
              value={dateOfBirth}
              onChange={(event) => setDateOfBirth(event.target.value)}
              min={birthDateMinimum()}
              max={birthDateMaximum()}
            />
          </div>
        </section>

        <div className="flex justify-end border-t border-border pt-5">
          <Button type="submit" disabled={saving}>
            <Save className="mr-2 h-4 w-4" />
            {saving ? "Đang lưu..." : "Lưu thay đổi"}
          </Button>
        </div>
      </form>
    </div>
  );
}

function birthDateMinimum() {
  const date = new Date();
  date.setFullYear(date.getFullYear() - 120);
  return toDateInputValue(date);
}

function birthDateMaximum() {
  const date = new Date();
  date.setDate(date.getDate() - 1);
  return toDateInputValue(date);
}

function toDateInputValue(date: Date) {
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, "0");
  const day = String(date.getDate()).padStart(2, "0");
  return `${year}-${month}-${day}`;
}
