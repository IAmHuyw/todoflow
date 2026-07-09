import { createFileRoute, Link } from "@tanstack/react-router";
import { Button } from "@/components/ui/button";
import { CheckCircle2, ListTodo, Users, Bell, ArrowRight } from "lucide-react";

export const Route = createFileRoute("/")({
  component: Landing,
});

function Landing() {
  return (
    <div className="min-h-screen bg-background">
      <header className="border-b border-border">
        <div className="mx-auto flex max-w-6xl items-center justify-between px-6 py-4">
          <Link to="/" className="flex items-center gap-2 font-semibold">
            <div className="flex h-7 w-7 items-center justify-center rounded-md bg-primary text-primary-foreground">
              <CheckCircle2 className="h-4 w-4" />
            </div>
            <span>TodoFlow</span>
          </Link>
          <div className="flex items-center gap-2">
            <Button asChild variant="ghost" size="sm">
              <Link to="/auth">Đăng nhập</Link>
            </Button>
            <Button asChild size="sm">
              <Link to="/auth" search={{ mode: "register" }}>
                Bắt đầu miễn phí
              </Link>
            </Button>
          </div>
        </div>
      </header>

      <section className="mx-auto max-w-4xl px-6 py-24 text-center">
        <div className="mb-6 inline-flex items-center gap-2 rounded-full border border-border bg-muted/50 px-3 py-1 text-xs text-muted-foreground">
          <span className="h-1.5 w-1.5 rounded-full bg-primary" />
          Ứng dụng quản lý công việc
        </div>
        <h1 className="text-5xl font-semibold tracking-tight text-foreground md:text-6xl">
          Quản lý công việc gọn.
          <br />
          <span className="text-muted-foreground">Ý tưởng không lạc trôi.</span>
        </h1>
        <p className="mx-auto mt-6 max-w-xl text-lg text-muted-foreground">
          TodoFlow gom công việc, danh mục, nhãn, việc con, chia sẻ cộng tác và nhắc nhở
          vào một không gian tối giản, tập trung.
        </p>
        <div className="mt-8 flex justify-center gap-3">
          <Button asChild size="lg">
            <Link to="/auth" search={{ mode: "register" }}>
              Tạo tài khoản
              <ArrowRight className="ml-2 h-4 w-4" />
            </Link>
          </Button>
        </div>
      </section>

      <section className="mx-auto max-w-6xl px-6 pb-24">
        <div className="grid gap-4 md:grid-cols-3">
          {[
            {
              icon: ListTodo,
              title: "Công việc có cấu trúc",
              desc: "Danh mục, độ ưu tiên, nhãn và danh sách việc con được tổ chức rõ ràng.",
            },
            {
              icon: Users,
              title: "Cộng tác dễ dàng",
              desc: "Chia sẻ công việc với đồng đội, phân quyền xem/sửa và phản hồi lời mời.",
            },
            {
              icon: Bell,
              title: "Nhắc nhở thông minh",
              desc: "Đặt nhắc nhở theo lịch và nhận thông báo trong ứng dụng khi có cập nhật.",
            },
          ].map((f) => (
            <div
              key={f.title}
              className="rounded-2xl border border-border bg-card p-6"
            >
              <div className="mb-4 flex h-10 w-10 items-center justify-center rounded-lg bg-primary/10 text-primary">
                <f.icon className="h-5 w-5" />
              </div>
              <h3 className="font-semibold text-foreground">{f.title}</h3>
              <p className="mt-2 text-sm text-muted-foreground">{f.desc}</p>
            </div>
          ))}
        </div>
      </section>

      <footer className="border-t border-border">
        <div className="mx-auto max-w-6xl px-6 py-6 text-center text-sm text-muted-foreground">
          © {new Date().getFullYear()} TodoFlow
        </div>
      </footer>
    </div>
  );
}
