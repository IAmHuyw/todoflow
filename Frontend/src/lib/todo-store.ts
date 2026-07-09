import { create } from "zustand";
import {
  apiRequest,
  clearTokens,
  getAccessToken,
  getRefreshToken,
  setTokens,
} from "./api-client";
import type {
  Category,
  Notification,
  Priority,
  ReminderChannel,
  SharePermission,
  ShareStatus,
  Status,
  SubTask,
  Tag,
  Task,
  TaskReminder,
  TaskShare,
  User,
} from "./todo-types";
import {
  startRealtime,
  stopRealtime,
  syncRealtimeTaskGroups,
} from "./realtime";

interface AuthResponse {
  user: User;
  accessToken: string;
  refreshToken: string;
}

interface TaskDto extends Task {
  subTasks: SubTask[];
}

interface TaskShareDto extends Omit<TaskShare, "task"> {
  task?: TaskDto | null;
}

interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface TaskListQuery {
  categoryId?: string;
  priority?: Priority | "all";
  status?: Status | "all";
  search?: string;
  sortBy?: "createdAt" | "dueDate" | "priority" | "title";
  page?: number;
  pageSize?: number;
}

interface TodoState {
  users: User[];
  categories: Category[];
  tags: Tag[];
  tasks: Task[];
  subtasks: SubTask[];
  shares: TaskShare[];
  notifications: Notification[];
  reminders: TaskReminder[];
  currentUserId: string | null;
  hydrated: boolean;
  loading: boolean;
  error: string | null;
  setHydrated: (v: boolean) => void;
  initializeAuth: () => Promise<void>;
  loadWorkspace: () => Promise<void>;
  loadTasks: (query?: TaskListQuery) => Promise<void>;
  login: (email: string, password: string) => Promise<{ ok: boolean; error?: string }>;
  register: (u: {
    username: string;
    email: string;
    password: string;
  }) => Promise<{ ok: boolean; error?: string }>;
  logout: () => Promise<void>;
  addCategory: (name: string, color: string) => Promise<void>;
  updateCategory: (id: string, patch: Partial<Category>) => Promise<void>;
  deleteCategory: (id: string) => Promise<void>;
  addTag: (name: string) => Promise<void>;
  deleteTag: (id: string) => Promise<void>;
  addTask: (
    input: Omit<Task, "id" | "userId" | "isDeleted" | "createdAt" | "updatedAt">,
  ) => Promise<string | null>;
  updateTask: (id: string, patch: Partial<Task>) => Promise<void>;
  deleteTask: (id: string) => Promise<void>;
  setTaskStatus: (id: string, status: Status) => Promise<void>;
  addSubtask: (taskId: string, title: string) => Promise<void>;
  toggleSubtask: (id: string) => Promise<void>;
  deleteSubtask: (id: string) => Promise<void>;
  shareTask: (
    taskId: string,
    emailOrUsername: string,
    permission: SharePermission,
  ) => Promise<{ ok: boolean; error?: string }>;
  respondShare: (id: string, status: ShareStatus) => Promise<void>;
  changeSharePermission: (id: string, permission: SharePermission) => Promise<void>;
  revokeShare: (id: string) => Promise<void>;
  addReminder: (taskId: string, remindAt: string, channel: ReminderChannel) => Promise<void>;
  deleteReminder: (id: string) => Promise<void>;
  markNotificationRead: (id: string) => Promise<void>;
  markAllNotificationsRead: () => Promise<void>;
}

export const useTodoStore = create<TodoState>((set, get) => ({
  users: [],
  categories: [],
  tags: [],
  tasks: [],
  subtasks: [],
  shares: [],
  notifications: [],
  reminders: [],
  currentUserId: null,
  hydrated: false,
  loading: false,
  error: null,

  setHydrated: (v) => set({ hydrated: v }),

  initializeAuth: async () => {
    if (get().hydrated) return;
    if (!getAccessToken() || !getRefreshToken()) {
      clearTokens();
      set({ hydrated: true, currentUserId: null, users: [] });
      return;
    }

    set({ loading: true, error: null });
    try {
      const user = await apiRequest<User>("/api/auth/me");
      set({ users: [user], currentUserId: user.id, hydrated: true });
      await get().loadWorkspace();
      await startStoreRealtime(set, get);
    } catch {
      clearTokens();
      set({
        users: [],
        categories: [],
        tags: [],
        tasks: [],
        subtasks: [],
        shares: [],
        notifications: [],
        reminders: [],
        currentUserId: null,
        hydrated: true,
      });
    } finally {
      set({ loading: false });
    }
  },

  loadWorkspace: async () => {
    set({ loading: true, error: null });
    try {
      const [categories, tags, page, receivedShares, notificationPage] = await Promise.all([
        apiRequest<Category[]>("/api/categories"),
        apiRequest<Tag[]>("/api/tags"),
        apiRequest<PagedResult<TaskDto>>(
          "/api/tasks?page=1&pageSize=100&sortBy=createdAt&sortDir=desc",
        ),
        apiRequest<TaskShareDto[]>("/api/tasks/shared-with-me"),
        apiRequest<PagedResult<Notification>>("/api/notifications?page=1&pageSize=100"),
      ]);
      const ownedShareLists = await Promise.all(
        page.items
          .filter((task) => task.userId === get().currentUserId)
          .map((task) =>
            apiRequest<TaskShareDto[]>(`/api/tasks/${task.id}/shares`).catch(() => []),
          ),
      );
      const shareData = [...receivedShares, ...ownedShareLists.flat()];
      const normalized = normalizeTasks([
        ...page.items,
        ...shareData.flatMap((share) => (share.task ? [share.task] : [])),
      ]);
      const reminderLists = await Promise.all(
        normalized.tasks.map((task) =>
          apiRequest<TaskReminder[]>(`/api/tasks/${task.id}/reminders`).catch(() => []),
        ),
      );
      set({
        categories,
        tags,
        ...normalized,
        shares: normalizeShares(shareData),
        notifications: notificationPage.items,
        reminders: reminderLists.flat(),
      });
      await syncRealtimeTaskGroups(get().tasks.map((task) => task.id));
    } catch (error) {
      set({ error: getErrorMessage(error) });
      throw error;
    } finally {
      set({ loading: false });
    }
  },

  loadTasks: async (query) => {
    set({ loading: true, error: null });
    try {
      const page = await apiRequest<PagedResult<TaskDto>>(
        `/api/tasks?${buildTaskQueryString(query)}`,
      );
      set(normalizeTasks(page.items));
      await syncRealtimeTaskGroups(get().tasks.map((task) => task.id));
    } catch (error) {
      set({ error: getErrorMessage(error) });
      throw error;
    } finally {
      set({ loading: false });
    }
  },

  login: async (email, password) => {
    try {
      const response = await apiRequest<AuthResponse>(
        "/api/auth/login",
        {
          method: "POST",
          body: JSON.stringify({ emailOrUsername: email, password }),
        },
        false,
      );
      setTokens(response.accessToken, response.refreshToken);
      set({ users: [response.user], currentUserId: response.user.id, hydrated: true });
      await get().loadWorkspace();
      await startStoreRealtime(set, get);
      return { ok: true };
    } catch (error) {
      return { ok: false, error: getErrorMessage(error) };
    }
  },

  register: async ({ username, email, password }) => {
    try {
      const response = await apiRequest<AuthResponse>(
        "/api/auth/register",
        {
          method: "POST",
          body: JSON.stringify({ username, email, password }),
        },
        false,
      );
      setTokens(response.accessToken, response.refreshToken);
      set({ users: [response.user], currentUserId: response.user.id, hydrated: true });
      await get().loadWorkspace();
      await startStoreRealtime(set, get);
      return { ok: true };
    } catch (error) {
      return { ok: false, error: getErrorMessage(error) };
    }
  },

  logout: async () => {
    const refreshToken = getRefreshToken();
    if (refreshToken) {
      try {
        await apiRequest("/api/auth/logout", {
          method: "POST",
          body: JSON.stringify({ refreshToken }),
        });
      } catch {
        // Local logout still succeeds if the network request fails.
      }
    }
    clearTokens();
    await stopRealtime();
    set({
      users: [],
      categories: [],
      tags: [],
      tasks: [],
      subtasks: [],
      shares: [],
      notifications: [],
      reminders: [],
      currentUserId: null,
      hydrated: true,
    });
  },

  addCategory: async (name, color) => {
    const category = await apiRequest<Category>("/api/categories", {
      method: "POST",
      body: JSON.stringify({ name, color }),
    });
    set({ categories: [...get().categories, category] });
  },

  updateCategory: async (id, patch) => {
    const existing = get().categories.find((category) => category.id === id);
    if (!existing) return;
    const category = await apiRequest<Category>(`/api/categories/${id}`, {
      method: "PUT",
      body: JSON.stringify({
        name: patch.name ?? existing.name,
        color: patch.color ?? existing.color,
      }),
    });
    set({
      categories: get().categories.map((item) => (item.id === id ? category : item)),
    });
  },

  deleteCategory: async (id) => {
    await apiRequest(`/api/categories/${id}`, { method: "DELETE" });
    set({
      categories: get().categories.filter((category) => category.id !== id),
      tasks: get().tasks.map((task) =>
        task.categoryId === id ? { ...task, categoryId: null } : task,
      ),
    });
  },

  addTag: async (name) => {
    const tag = await apiRequest<Tag>("/api/tags", {
      method: "POST",
      body: JSON.stringify({ name }),
    });
    set({ tags: [...get().tags, tag] });
  },

  deleteTag: async (id) => {
    await apiRequest(`/api/tags/${id}`, { method: "DELETE" });
    set({
      tags: get().tags.filter((tag) => tag.id !== id),
      tasks: get().tasks.map((task) => ({
        ...task,
        tagIds: task.tagIds.filter((tagId) => tagId !== id),
      })),
    });
  },

  addTask: async (input) => {
    const task = await apiRequest<TaskDto>("/api/tasks", {
      method: "POST",
      body: JSON.stringify(input),
    });
    upsertTask(set, get, task);
    return task.id;
  },

  updateTask: async (id, patch) => {
    const existing = get().tasks.find((task) => task.id === id);
    if (!existing) return;

    const task = await apiRequest<TaskDto>(`/api/tasks/${id}`, {
      method: "PUT",
      body: JSON.stringify({
        categoryId: patch.categoryId ?? existing.categoryId,
        title: patch.title ?? existing.title,
        description: patch.description ?? existing.description,
        priority: patch.priority ?? existing.priority,
        status: patch.status ?? existing.status,
        dueDate: patch.dueDate === undefined ? existing.dueDate : patch.dueDate,
        tagIds: patch.tagIds ?? existing.tagIds,
      }),
    });
    upsertTask(set, get, task);
  },

  deleteTask: async (id) => {
    await apiRequest(`/api/tasks/${id}`, { method: "DELETE" });
    set({
      tasks: get().tasks.map((task) =>
        task.id === id
          ? { ...task, isDeleted: true, updatedAt: new Date().toISOString() }
          : task,
      ),
      subtasks: get().subtasks.filter((subtask) => subtask.taskId !== id),
    });
  },

  setTaskStatus: async (id, status) => {
    const task = await apiRequest<TaskDto>(`/api/tasks/${id}/status`, {
      method: "PATCH",
      body: JSON.stringify({ status }),
    });
    upsertTask(set, get, task);
  },

  addSubtask: async (taskId, title) => {
    const subTask = await apiRequest<SubTask>(`/api/tasks/${taskId}/subtasks`, {
      method: "POST",
      body: JSON.stringify({ title }),
    });
    set({ subtasks: [...get().subtasks, subTask] });
  },

  toggleSubtask: async (id) => {
    const existing = get().subtasks.find((subtask) => subtask.id === id);
    if (!existing) return;
    const subTask = await apiRequest<SubTask>(`/api/subtasks/${id}`, {
      method: "PUT",
      body: JSON.stringify({
        title: existing.title,
        isCompleted: !existing.isCompleted,
      }),
    });
    set({
      subtasks: get().subtasks.map((item) => (item.id === id ? subTask : item)),
    });
  },

  deleteSubtask: async (id) => {
    await apiRequest(`/api/subtasks/${id}`, { method: "DELETE" });
    set({ subtasks: get().subtasks.filter((subtask) => subtask.id !== id) });
  },

  shareTask: async (taskId, emailOrUsername, permission) => {
    try {
      const share = await apiRequest<TaskShareDto>(`/api/tasks/${taskId}/share`, {
        method: "POST",
        body: JSON.stringify({ emailOrUsername, permission }),
      });
      upsertShare(set, get, share);
      return { ok: true };
    } catch (error) {
      return { ok: false, error: getErrorMessage(error) };
    }
  },
  respondShare: async (id, status) => {
    const share = await apiRequest<TaskShareDto>(`/api/task-shares/${id}/respond`, {
      method: "PUT",
      body: JSON.stringify({ status }),
    });
    upsertShare(set, get, share);
    await syncRealtimeTaskGroups(get().tasks.map((task) => task.id));
  },
  changeSharePermission: async (id, permission) => {
    const share = await apiRequest<TaskShareDto>(`/api/task-shares/${id}/permission`, {
      method: "PUT",
      body: JSON.stringify({ permission }),
    });
    upsertShare(set, get, share);
  },
  revokeShare: async (id) => {
    await apiRequest(`/api/task-shares/${id}`, { method: "DELETE" });
    set({ shares: get().shares.filter((share) => share.id !== id) });
  },
  addReminder: async (taskId, remindAt, channel) => {
    const reminder = await apiRequest<TaskReminder>(`/api/tasks/${taskId}/reminders`, {
      method: "POST",
      body: JSON.stringify({ remindAt, channel }),
    });
    set({ reminders: [...get().reminders, reminder] });
  },
  deleteReminder: async (id) => {
    await apiRequest(`/api/reminders/${id}`, { method: "DELETE" });
    set({ reminders: get().reminders.filter((reminder) => reminder.id !== id) });
  },
  markNotificationRead: async (id) => {
    await apiRequest(`/api/notifications/${id}/read`, { method: "PUT" });
    set({
      notifications: get().notifications.map((notification) =>
        notification.id === id ? { ...notification, isRead: true } : notification,
      ),
    });
  },
  markAllNotificationsRead: async () => {
    const userId = get().currentUserId;
    await apiRequest("/api/notifications/read-all", { method: "PUT" });
    set({
      notifications: get().notifications.map((notification) =>
        notification.userId === userId ? { ...notification, isRead: true } : notification,
      ),
    });
  },
}));

export function useCurrentUser() {
  return useTodoStore((state) =>
    state.currentUserId
      ? state.users.find((user) => user.id === state.currentUserId) ?? null
      : null,
  );
}

function normalizeTasks(taskDtos: TaskDto[]) {
  const taskMap = new Map<string, Task>();
  const subTaskMap = new Map<string, SubTask>();

  for (const taskDto of taskDtos) {
    const { subTasks, ...task } = taskDto;
    taskMap.set(task.id, task);
    for (const subTask of subTasks ?? []) {
      subTaskMap.set(subTask.id, subTask);
    }
  }

  const tasks = [...taskMap.values()];
  const subtasks = [...subTaskMap.values()];
  return { tasks, subtasks };
}

function normalizeShares(shares: TaskShareDto[]) {
  const shareMap = new Map<string, TaskShare>();
  for (const { task: _task, ...share } of shares) {
    shareMap.set(share.id, share);
  }
  return [...shareMap.values()];
}

function upsertTask(
  set: (partial: Partial<TodoState>) => void,
  get: () => TodoState,
  taskDto: TaskDto,
) {
  const { tasks: normalizedTasks, subtasks } = normalizeTasks([taskDto]);
  const task = normalizedTasks[0];
  const exists = get().tasks.some((item) => item.id === task.id);

  set({
    tasks: exists
      ? get().tasks.map((item) => (item.id === task.id ? task : item))
      : [task, ...get().tasks],
    subtasks: [
      ...get().subtasks.filter((subtask) => subtask.taskId !== task.id),
      ...subtasks,
    ],
  });
}

function upsertShare(
  set: (partial: Partial<TodoState>) => void,
  get: () => TodoState,
  shareDto: TaskShareDto,
) {
  const { task, ...share } = shareDto;
  const exists = get().shares.some((item) => item.id === share.id);

  set({
    shares: exists
      ? get().shares.map((item) => (item.id === share.id ? share : item))
      : [share, ...get().shares],
  });

  if (task) {
    upsertTask(set, get, task);
  }
}

function upsertNotification(
  set: (partial: Partial<TodoState>) => void,
  get: () => TodoState,
  notification: Notification,
) {
  const exists = get().notifications.some((item) => item.id === notification.id);
  set({
    notifications: exists
      ? get().notifications.map((item) =>
          item.id === notification.id ? notification : item,
        )
      : [notification, ...get().notifications],
  });
}

async function startStoreRealtime(
  set: (partial: Partial<TodoState>) => void,
  get: () => TodoState,
) {
  try {
    await startRealtime({
      taskUpdated: (task) => upsertTask(set, get, toTaskDto(task)),
      taskDeleted: (taskId) =>
        set({
          tasks: get().tasks.map((task) =>
            task.id === taskId
              ? { ...task, isDeleted: true, updatedAt: new Date().toISOString() }
              : task,
          ),
          subtasks: get().subtasks.filter((subtask) => subtask.taskId !== taskId),
        }),
      subTaskUpdated: (subTask) =>
        set({
          subtasks: get().subtasks.some((item) => item.id === subTask.id)
            ? get().subtasks.map((item) => (item.id === subTask.id ? subTask : item))
            : [...get().subtasks, subTask],
        }),
      taskShared: (share) => upsertShare(set, get, share as TaskShareDto),
      shareResponded: (share) => upsertShare(set, get, share as TaskShareDto),
      notificationReceived: (notification) => upsertNotification(set, get, notification),
    });
    await syncRealtimeTaskGroups(get().tasks.map((task) => task.id));
  } catch {
    // Realtime is progressive enhancement; API CRUD still works without the socket.
  }
}

function toTaskDto(task: Task & { subTasks?: SubTask[] }): TaskDto {
  return { ...task, subTasks: task.subTasks ?? [] };
}

function buildTaskQueryString(query?: TaskListQuery) {
  const params = new URLSearchParams({
    page: String(query?.page ?? 1),
    pageSize: String(query?.pageSize ?? 100),
    sortBy: query?.sortBy ?? "createdAt",
  });
  const sortBy = query?.sortBy ?? "createdAt";
  params.set("sortDir", sortBy === "createdAt" ? "desc" : "asc");

  if (query?.categoryId && query.categoryId !== "all") {
    params.set("categoryId", query.categoryId);
  }
  if (query?.priority && query.priority !== "all") {
    params.set("priority", query.priority);
  }
  if (query?.status && query.status !== "all") {
    params.set("status", query.status);
  }
  if (query?.search?.trim()) {
    params.set("search", query.search.trim());
  }

  return params.toString();
}

function getErrorMessage(error: unknown) {
  if (error instanceof Error) {
    return error.message;
  }
  return "Có lỗi xảy ra khi gọi backend.";
}
