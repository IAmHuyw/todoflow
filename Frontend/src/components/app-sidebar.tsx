import { Link, useRouterState } from "@tanstack/react-router";
import {
  LayoutDashboard,
  FolderKanban,
  Tag as TagIcon,
  Users,
  Bell,
  CheckCircle2,
  LogOut,
} from "lucide-react";
import {
  Sidebar,
  SidebarContent,
  SidebarFooter,
  SidebarGroup,
  SidebarGroupContent,
  SidebarGroupLabel,
  SidebarHeader,
  SidebarMenu,
  SidebarMenuButton,
  SidebarMenuItem,
} from "@/components/ui/sidebar";
import { Badge } from "@/components/ui/badge";
import { useTodoStore, useCurrentUser } from "@/lib/todo-store";
import { Button } from "@/components/ui/button";
import { useNavigate } from "@tanstack/react-router";

const items = [
  { title: "Dashboard", url: "/dashboard", icon: LayoutDashboard },
  { title: "Categories", url: "/categories", icon: FolderKanban },
  { title: "Tags", url: "/tags", icon: TagIcon },
  { title: "Shared", url: "/shared", icon: Users },
  { title: "Notifications", url: "/notifications", icon: Bell },
];

export function AppSidebar() {
  const currentPath = useRouterState({
    select: (r) => r.location.pathname,
  });
  const user = useCurrentUser();
  const logout = useTodoStore((s) => s.logout);
  const navigate = useNavigate();
  const unread = useTodoStore((s) =>
    s.notifications.filter((n) => n.userId === s.currentUserId && !n.isRead).length,
  );
  const sharedPending = useTodoStore((s) =>
    s.shares.filter(
      (sh) => sh.sharedWithUserId === s.currentUserId && sh.status === "pending",
    ).length,
  );

  const badgeFor = (url: string) => {
    if (url === "/notifications" && unread > 0) return unread;
    if (url === "/shared" && sharedPending > 0) return sharedPending;
    return null;
  };

  const handleLogout = async () => {
    await logout();
    navigate({ to: "/auth" });
  };

  return (
    <Sidebar collapsible="icon">
      <SidebarHeader className="border-b border-border">
        <Link to="/dashboard" className="flex items-center gap-2 px-2 py-2 font-semibold">
          <div className="flex h-7 w-7 shrink-0 items-center justify-center rounded-md bg-primary text-primary-foreground">
            <CheckCircle2 className="h-4 w-4" />
          </div>
          <span className="group-data-[collapsible=icon]:hidden">TodoFlow</span>
        </Link>
      </SidebarHeader>
      <SidebarContent>
        <SidebarGroup>
          <SidebarGroupLabel>Workspace</SidebarGroupLabel>
          <SidebarGroupContent>
            <SidebarMenu>
              {items.map((item) => {
                const active = currentPath === item.url;
                const badge = badgeFor(item.url);
                return (
                  <SidebarMenuItem key={item.title}>
                    <SidebarMenuButton asChild isActive={active}>
                      <Link to={item.url} className="flex items-center gap-2">
                        <item.icon className="h-4 w-4" />
                        <span className="flex-1">{item.title}</span>
                        {badge != null && (
                          <Badge variant="secondary" className="h-5 px-1.5 text-[10px]">
                            {badge}
                          </Badge>
                        )}
                      </Link>
                    </SidebarMenuButton>
                  </SidebarMenuItem>
                );
              })}
            </SidebarMenu>
          </SidebarGroupContent>
        </SidebarGroup>
      </SidebarContent>
      <SidebarFooter className="border-t border-border">
        <div className="flex items-center gap-2 px-2 py-2 group-data-[collapsible=icon]:hidden">
          <div className="flex h-8 w-8 items-center justify-center rounded-full bg-primary/10 text-sm font-medium text-primary">
            {user?.username?.[0]?.toUpperCase() ?? "?"}
          </div>
          <div className="flex-1 overflow-hidden text-sm">
            <div className="truncate font-medium">{user?.username}</div>
            <div className="truncate text-xs text-muted-foreground">{user?.email}</div>
          </div>
          <Button variant="ghost" size="icon" onClick={handleLogout} title="Đăng xuất">
            <LogOut className="h-4 w-4" />
          </Button>
        </div>
      </SidebarFooter>
    </Sidebar>
  );
}
