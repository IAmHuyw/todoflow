import { createFileRoute, useNavigate } from "@tanstack/react-router";
import { format } from "date-fns";
import { vi } from "date-fns/locale";
import { ChevronLeft, ChevronRight, LockKeyhole, Search, ShieldCheck, UnlockKeyhole } from "lucide-react";
import { useCallback, useEffect, useState } from "react";
import { toast } from "sonner";
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from "@/components/ui/alert-dialog";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import {
  type PagedResult,
  useCurrentUser,
  useTodoStore,
} from "@/lib/todo-store";
import type { AdminUser } from "@/lib/todo-types";

export const Route = createFileRoute("/_authenticated/admin/users")({
  component: AdminUsersPage,
  head: () => ({ meta: [{ title: "Quản trị người dùng — TodoFlow" }] }),
});

type StatusFilter = "all" | "active" | "disabled";

function AdminUsersPage() {
  const currentUser = useCurrentUser();
  const loadAdminUsers = useTodoStore((state) => state.loadAdminUsers);
  const updateAdminUserStatus = useTodoStore((state) => state.updateAdminUserStatus);
  const navigate = useNavigate();
  const [searchInput, setSearchInput] = useState("");
  const [search, setSearch] = useState("");
  const [status, setStatus] = useState<StatusFilter>("all");
  const [page, setPage] = useState(1);
  const [result, setResult] = useState<PagedResult<AdminUser> | null>(null);
  const [loading, setLoading] = useState(true);
  const [pendingUser, setPendingUser] = useState<AdminUser | null>(null);
  const [updatingId, setUpdatingId] = useState<string | null>(null);
  const isAdmin = currentUser?.role === "admin";

  useEffect(() => {
    if (currentUser && !isAdmin) {
      void navigate({ to: "/dashboard" });
    }
  }, [currentUser, isAdmin, navigate]);

  const loadUsers = useCallback(async () => {
    if (!isAdmin) return;

    setLoading(true);
    try {
      const data = await loadAdminUsers({
        search,
        isActive: status === "active" ? true : status === "disabled" ? false : undefined,
        page,
        pageSize: 20,
      });
      setResult(data);
    } catch (error) {
      toast.error(error instanceof Error ? error.message : "Không tải được danh sách người dùng");
    } finally {
      setLoading(false);
    }
  }, [isAdmin, loadAdminUsers, page, search, status]);

  useEffect(() => {
    void loadUsers();
  }, [loadUsers]);

  if (!currentUser || !isAdmin) return null;

  const submitSearch = (event: React.FormEvent) => {
    event.preventDefault();
    setPage(1);
    setSearch(searchInput.trim());
  };

  const changeStatusFilter = (value: StatusFilter) => {
    setPage(1);
    setStatus(value);
  };

  const updateStatus = async () => {
    if (!pendingUser) return;

    const nextIsActive = !pendingUser.isActive;
    setUpdatingId(pendingUser.id);
    try {
      const updated = await updateAdminUserStatus(pendingUser.id, nextIsActive);
      setResult((current) =>
        current
          ? {
              ...current,
              items: current.items.map((item) => (item.id === updated.id ? updated : item)),
            }
          : current,
      );
      setPendingUser(null);
      toast.success(nextIsActive ? "Đã mở khóa tài khoản" : "Đã khóa tài khoản");
    } catch (error) {
      toast.error(error instanceof Error ? error.message : "Không cập nhật được trạng thái tài khoản");
    } finally {
      setUpdatingId(null);
    }
  };

  const users = result?.items ?? [];
  const totalPages = result?.totalPages ?? 0;

  return (
    <div className="mx-auto w-full max-w-6xl p-4 sm:p-6">
      <div className="mb-6 flex flex-col gap-4 sm:flex-row sm:items-end sm:justify-between">
        <div>
          <div className="mb-2 flex items-center gap-2 text-primary">
            <ShieldCheck className="h-5 w-5" />
            <span className="text-sm font-medium">Quản trị hệ thống</span>
          </div>
          <h1 className="text-2xl font-semibold">Tài khoản người dùng</h1>
          <p className="mt-1 text-sm text-muted-foreground">
            {result ? `${result.totalCount} tài khoản trong hệ thống` : "Đang tải dữ liệu tài khoản"}
          </p>
        </div>
        <form onSubmit={submitSearch} className="flex w-full gap-2 sm:w-auto">
          <div className="relative min-w-0 flex-1 sm:w-72">
            <Search className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
            <Input
              value={searchInput}
              onChange={(event) => setSearchInput(event.target.value)}
              placeholder="Tìm tên, email..."
              className="pl-9"
            />
          </div>
          <Button type="submit">Tìm</Button>
        </form>
      </div>

      <div className="mb-4 flex justify-end">
        <Select value={status} onValueChange={(value) => changeStatusFilter(value as StatusFilter)}>
          <SelectTrigger className="w-44">
            <SelectValue placeholder="Trạng thái" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">Tất cả trạng thái</SelectItem>
            <SelectItem value="active">Đang hoạt động</SelectItem>
            <SelectItem value="disabled">Đã khóa</SelectItem>
          </SelectContent>
        </Select>
      </div>

      <div className="border border-border">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Người dùng</TableHead>
              <TableHead className="hidden md:table-cell">Vai trò</TableHead>
              <TableHead>Trạng thái</TableHead>
              <TableHead className="hidden lg:table-cell">Tham gia</TableHead>
              <TableHead className="w-32 text-right">Thao tác</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {loading && (
              <TableRow>
                <TableCell colSpan={5} className="h-28 text-center text-muted-foreground">
                  Đang tải tài khoản...
                </TableCell>
              </TableRow>
            )}
            {!loading && users.length === 0 && (
              <TableRow>
                <TableCell colSpan={5} className="h-28 text-center text-muted-foreground">
                  Không tìm thấy tài khoản phù hợp.
                </TableCell>
              </TableRow>
            )}
            {!loading &&
              users.map((user) => {
                const isProtected = user.role === "admin" || user.id === currentUser.id;
                return (
                  <TableRow key={user.id}>
                    <TableCell>
                      <div className="min-w-48">
                        <div className="font-medium">{user.fullName || user.username}</div>
                        <div className="text-xs text-muted-foreground">{user.email}</div>
                      </div>
                    </TableCell>
                    <TableCell className="hidden md:table-cell">
                      <Badge variant={user.role === "admin" ? "default" : "outline"}>
                        {user.role === "admin" ? "Quản trị viên" : "Người dùng"}
                      </Badge>
                    </TableCell>
                    <TableCell>
                      <Badge variant={user.isActive ? "secondary" : "destructive"}>
                        {user.isActive ? "Hoạt động" : "Đã khóa"}
                      </Badge>
                    </TableCell>
                    <TableCell className="hidden lg:table-cell text-muted-foreground">
                      {format(new Date(user.createdAt), "dd/MM/yyyy", { locale: vi })}
                    </TableCell>
                    <TableCell className="text-right">
                      {isProtected ? (
                        <span className="text-xs text-muted-foreground">Bảo vệ</span>
                      ) : (
                        <Button
                          variant={user.isActive ? "outline" : "default"}
                          size="sm"
                          onClick={() => setPendingUser(user)}
                          disabled={updatingId === user.id}
                        >
                          {user.isActive ? (
                            <LockKeyhole className="mr-1.5 h-3.5 w-3.5" />
                          ) : (
                            <UnlockKeyhole className="mr-1.5 h-3.5 w-3.5" />
                          )}
                          {user.isActive ? "Khóa" : "Mở"}
                        </Button>
                      )}
                    </TableCell>
                  </TableRow>
                );
              })}
          </TableBody>
        </Table>
      </div>

      <div className="mt-4 flex items-center justify-between text-sm">
        <span className="text-muted-foreground">
          {totalPages > 0 ? `Trang ${result?.page} / ${totalPages}` : "Không có trang dữ liệu"}
        </span>
        <div className="flex gap-1">
          <Button
            variant="outline"
            size="icon"
            title="Trang trước"
            disabled={loading || page <= 1}
            onClick={() => setPage((current) => current - 1)}
          >
            <ChevronLeft className="h-4 w-4" />
          </Button>
          <Button
            variant="outline"
            size="icon"
            title="Trang sau"
            disabled={loading || totalPages === 0 || page >= totalPages}
            onClick={() => setPage((current) => current + 1)}
          >
            <ChevronRight className="h-4 w-4" />
          </Button>
        </div>
      </div>

      <AlertDialog
        open={pendingUser !== null}
        onOpenChange={(open) => {
          if (!open && !updatingId) setPendingUser(null);
        }}
      >
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>
              {pendingUser?.isActive ? "Khóa tài khoản này?" : "Mở khóa tài khoản này?"}
            </AlertDialogTitle>
            <AlertDialogDescription>
              {pendingUser?.isActive
                ? "Người dùng sẽ bị đăng xuất và không thể truy cập TodoFlow cho đến khi được mở khóa."
                : "Người dùng sẽ có thể đăng nhập và sử dụng TodoFlow trở lại."}
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel disabled={updatingId !== null}>Hủy</AlertDialogCancel>
            <AlertDialogAction
              className={pendingUser?.isActive ? "bg-destructive text-destructive-foreground hover:bg-destructive/90" : undefined}
              disabled={updatingId !== null}
              onClick={() => void updateStatus()}
            >
              {updatingId ? "Đang cập nhật..." : pendingUser?.isActive ? "Khóa tài khoản" : "Mở khóa"}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  );
}
