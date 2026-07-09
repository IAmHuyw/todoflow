import { createFileRoute, useNavigate, Link } from "@tanstack/react-router";
import { z } from "zod";
import { zodValidator, fallback } from "@tanstack/zod-adapter";
import { useEffect, useState } from "react";
import { CheckCircle2 } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { useTodoStore } from "@/lib/todo-store";
import { apiRequest } from "@/lib/api-client";
import { toast } from "sonner";

const searchSchema = z.object({
  mode: fallback(z.string(), "login").default("login"),
});

export const Route = createFileRoute("/auth")({
  validateSearch: zodValidator(searchSchema),
  component: AuthPage,
  head: () => ({
    meta: [{ title: "Đăng nhập — TodoFlow" }],
  }),
});

function AuthPage() {
  const { mode } = Route.useSearch();
  const navigate = useNavigate();
  const login = useTodoStore((s) => s.login);
  const register = useTodoStore((s) => s.register);
  const initializeAuth = useTodoStore((s) => s.initializeAuth);
  const currentUserId = useTodoStore((s) => s.currentUserId);
  const hydrated = useTodoStore((s) => s.hydrated);

  useEffect(() => {
    initializeAuth();
  }, [initializeAuth]);

  useEffect(() => {
    if (hydrated && currentUserId) {
      navigate({ to: "/dashboard" });
    }
  }, [hydrated, currentUserId, navigate]);

  const [loginForm, setLoginForm] = useState({
    email: "",
    password: "",
  });
  const [registerForm, setRegisterForm] = useState({
    username: "",
    email: "",
    password: "",
  });
  const [forgotMode, setForgotMode] = useState(false);
  const [resetStep, setResetStep] = useState<"request" | "reset">("request");
  const [resetForm, setResetForm] = useState({
    email: "",
    otp: "",
    newPassword: "",
  });

  const submitLogin = async (e: React.FormEvent) => {
    e.preventDefault();
    const res = await login(loginForm.email, loginForm.password);
    if (!res.ok) return toast.error(res.error);
    toast.success("Đăng nhập thành công");
    navigate({ to: "/dashboard" });
  };

  const submitRegister = async (e: React.FormEvent) => {
    e.preventDefault();
    if (registerForm.password.length < 6)
      return toast.error("Mật khẩu tối thiểu 6 ký tự");
    const res = await register(registerForm);
    if (!res.ok) return toast.error(res.error);
    toast.success("Tạo tài khoản thành công");
    navigate({ to: "/dashboard" });
  };

  const submitForgotPassword = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      await apiRequest("/api/auth/forgot-password", {
        method: "POST",
        body: JSON.stringify({ email: resetForm.email }),
      });
      toast.success("Mã OTP đã được gửi tới email của bạn");
      setResetStep("reset");
    } catch (error) {
      toast.error(error instanceof Error ? error.message : "Không gửi được mã OTP");
    }
  };

  const submitResetPassword = async (e: React.FormEvent) => {
    e.preventDefault();
    if (resetForm.newPassword.length < 6) {
      return toast.error("Mật khẩu mới tối thiểu 6 ký tự");
    }

    try {
      await apiRequest("/api/auth/reset-password", {
        method: "POST",
        body: JSON.stringify(resetForm),
      });
      toast.success("Đặt lại mật khẩu thành công");
      setLoginForm({ email: resetForm.email, password: "" });
      setForgotMode(false);
      setResetStep("request");
      setResetForm({ email: "", otp: "", newPassword: "" });
    } catch (error) {
      toast.error(error instanceof Error ? error.message : "Không đặt lại được mật khẩu");
    }
  };

  return (
    <div className="flex min-h-screen items-center justify-center bg-muted/30 px-4">
      <div className="w-full max-w-md">
        <Link
          to="/"
          className="mb-8 flex items-center justify-center gap-2 font-semibold"
        >
          <div className="flex h-7 w-7 items-center justify-center rounded-md bg-primary text-primary-foreground">
            <CheckCircle2 className="h-4 w-4" />
          </div>
          TodoFlow
        </Link>
        <div className="rounded-2xl border border-border bg-card p-6 shadow-sm">
          {forgotMode ? (
            <div>
              <div className="mb-5">
                <h1 className="text-xl font-semibold tracking-tight">Quên mật khẩu</h1>
                <p className="mt-1 text-sm text-muted-foreground">
                  Nhập email tài khoản để nhận mã OTP đặt lại mật khẩu.
                </p>
              </div>
              {resetStep === "request" ? (
                <form onSubmit={submitForgotPassword} className="space-y-4">
                  <div className="space-y-2">
                    <Label htmlFor="forgot-email">Email</Label>
                    <Input
                      id="forgot-email"
                      type="email"
                      value={resetForm.email}
                      onChange={(e) =>
                        setResetForm({ ...resetForm, email: e.target.value })
                      }
                      required
                    />
                  </div>
                  <Button type="submit" className="w-full">
                    Gửi mã OTP
                  </Button>
                </form>
              ) : (
                <form onSubmit={submitResetPassword} className="space-y-4">
                  <div className="space-y-2">
                    <Label htmlFor="reset-email">Email</Label>
                    <Input
                      id="reset-email"
                      type="email"
                      value={resetForm.email}
                      onChange={(e) =>
                        setResetForm({ ...resetForm, email: e.target.value })
                      }
                      required
                    />
                  </div>
                  <div className="space-y-2">
                    <Label htmlFor="reset-otp">Mã OTP</Label>
                    <Input
                      id="reset-otp"
                      inputMode="numeric"
                      maxLength={6}
                      value={resetForm.otp}
                      onChange={(e) =>
                        setResetForm({
                          ...resetForm,
                          otp: e.target.value.replace(/\D/g, "").slice(0, 6),
                        })
                      }
                      required
                    />
                  </div>
                  <div className="space-y-2">
                    <Label htmlFor="reset-password">Mật khẩu mới</Label>
                    <Input
                      id="reset-password"
                      type="password"
                      value={resetForm.newPassword}
                      onChange={(e) =>
                        setResetForm({ ...resetForm, newPassword: e.target.value })
                      }
                      required
                      minLength={6}
                    />
                  </div>
                  <Button type="submit" className="w-full">
                    Đặt lại mật khẩu
                  </Button>
                </form>
              )}
              <Button
                type="button"
                variant="ghost"
                className="mt-4 w-full"
                onClick={() => {
                  setForgotMode(false);
                  setResetStep("request");
                }}
              >
                Quay lại đăng nhập
              </Button>
            </div>
          ) : (
          <Tabs defaultValue={mode === "register" ? "register" : "login"}>
            <TabsList className="grid w-full grid-cols-2">
              <TabsTrigger value="login">Đăng nhập</TabsTrigger>
              <TabsTrigger value="register">Đăng ký</TabsTrigger>
            </TabsList>

            <TabsContent value="login" className="mt-6">
              <form onSubmit={submitLogin} className="space-y-4">
                <div className="space-y-2">
                  <Label htmlFor="l-email">Email hoặc tên đăng nhập</Label>
                  <Input
                    id="l-email"
                    value={loginForm.email}
                    onChange={(e) =>
                      setLoginForm({ ...loginForm, email: e.target.value })
                    }
                    required
                  />
                </div>
                <div className="space-y-2">
                  <Label htmlFor="l-pw">Mật khẩu</Label>
                  <Input
                    id="l-pw"
                    type="password"
                    value={loginForm.password}
                    onChange={(e) =>
                      setLoginForm({ ...loginForm, password: e.target.value })
                    }
                    required
                  />
                </div>
                <Button type="submit" className="w-full">
                  Đăng nhập
                </Button>
                <Button
                  type="button"
                  variant="link"
                  className="w-full px-0"
                  onClick={() => {
                    setResetForm({
                      email: loginForm.email,
                      otp: "",
                      newPassword: "",
                    });
                    setForgotMode(true);
                  }}
                >
                  Quên mật khẩu?
                </Button>
              </form>
            </TabsContent>

            <TabsContent value="register" className="mt-6">
              <form onSubmit={submitRegister} className="space-y-4">
                <div className="space-y-2">
                  <Label htmlFor="r-username">Tên đăng nhập</Label>
                  <Input
                    id="r-username"
                    value={registerForm.username}
                    onChange={(e) =>
                      setRegisterForm({
                        ...registerForm,
                        username: e.target.value,
                      })
                    }
                    required
                  />
                </div>
                <div className="space-y-2">
                  <Label htmlFor="r-email">Email</Label>
                  <Input
                    id="r-email"
                    type="email"
                    value={registerForm.email}
                    onChange={(e) =>
                      setRegisterForm({
                        ...registerForm,
                        email: e.target.value,
                      })
                    }
                    required
                  />
                </div>
                <div className="space-y-2">
                  <Label htmlFor="r-pw">Mật khẩu</Label>
                  <Input
                    id="r-pw"
                    type="password"
                    value={registerForm.password}
                    onChange={(e) =>
                      setRegisterForm({
                        ...registerForm,
                        password: e.target.value,
                      })
                    }
                    required
                    minLength={6}
                  />
                </div>
                <Button type="submit" className="w-full">
                  Tạo tài khoản
                </Button>
              </form>
            </TabsContent>
          </Tabs>
          )}
        </div>
      </div>
    </div>
  );
}
