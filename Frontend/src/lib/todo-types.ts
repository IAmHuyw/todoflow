export type Priority = "low" | "medium" | "high";
export type Status = "todo" | "in_progress" | "done";
export type RecurrenceType = "none" | "daily" | "weekly" | "monthly";
export type SharePermission = "view" | "edit";
export type ShareStatus = "pending" | "accepted" | "rejected";
export type NotificationType =
  | "due_date_reminder"
  | "task_shared"
  | "task_updated"
  | "task_completed";
export type ReminderChannel = "email" | "in_app" | "both";

export interface User {
  id: string;
  username: string;
  email: string;
  password?: string;
  createdAt: string;
}

export interface Category {
  id: string;
  userId: string;
  name: string;
  color: string;
}

export interface Tag {
  id: string;
  userId: string;
  name: string;
}

export interface SubTask {
  id: string;
  taskId: string;
  title: string;
  note: string;
  isCompleted: boolean;
}

export interface Task {
  id: string;
  userId: string;
  categoryId: string | null;
  title: string;
  description: string;
  priority: Priority;
  status: Status;
  dueDate: string | null;
  recurrenceType: RecurrenceType;
  recurrenceInterval: number;
  recurrenceEndDate: string | null;
  recurrenceParentId: string | null;
  sortOrder: number;
  isDeleted: boolean;
  tagIds: string[];
  createdAt: string;
  updatedAt: string;
}

export interface TaskShare {
  id: string;
  taskId: string;
  ownerId: string;
  sharedWithUserId: string;
  permission: SharePermission;
  status: ShareStatus;
  createdAt: string;
  ownerUsername?: string | null;
  ownerEmail?: string | null;
  sharedWithUsername?: string | null;
  sharedWithEmail?: string | null;
  task?: Task | null;
}

export interface Notification {
  id: string;
  userId: string;
  taskId: string | null;
  type: NotificationType;
  message: string;
  isRead: boolean;
  createdAt: string;
}

export interface TaskReminder {
  id: string;
  taskId: string;
  remindAt: string;
  channel: ReminderChannel;
  isSent: boolean;
  createdAt: string;
}
