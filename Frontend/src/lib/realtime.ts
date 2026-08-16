import * as signalR from "@microsoft/signalr";
import { API_BASE_URL, getAccessToken } from "./api-client";
import type {
  Notification,
  ShareStatus,
  Status,
  SubTask,
  Task,
  TaskActivity,
  TaskComment,
  TaskShare,
} from "./todo-types";

interface RealtimeHandlers {
  taskUpdated: (task: Task & { subTasks?: SubTask[] }) => void;
  taskDeleted: (taskId: string) => void;
  subTaskUpdated: (subTask: SubTask) => void;
  taskShared: (share: TaskShare) => void;
  shareResponded: (share: TaskShare) => void;
  notificationReceived: (notification: Notification) => void;
  commentAdded: (comment: TaskComment) => void;
  commentUpdated: (comment: TaskComment) => void;
  commentDeleted: (commentId: string) => void;
  assigneeChanged: (task: Task & { subTasks?: SubTask[] }) => void;
  activityAdded: (activity: TaskActivity) => void;
}

let connection: signalR.HubConnection | null = null;
const joinedTaskIds = new Set<string>();

export async function startRealtime(handlers: RealtimeHandlers) {
  if (connection) return;

  connection = new signalR.HubConnectionBuilder()
    .withUrl(`${API_BASE_URL}/hubs/tasks`, {
      accessTokenFactory: () => getAccessToken() ?? "",
    })
    .withAutomaticReconnect()
    .build();

  connection.on("TaskUpdated", handlers.taskUpdated);
  connection.on("TaskStatusChanged", (payload: { taskId: string; status: Status }) => {
    // TaskUpdated is emitted after this; keep the handler for protocol completeness.
    void payload;
  });
  connection.on("SubTaskUpdated", handlers.subTaskUpdated);
  connection.on("TaskDeleted", (payload: { taskId: string }) =>
    handlers.taskDeleted(payload.taskId),
  );
  connection.on("TaskShared", handlers.taskShared);
  connection.on("ShareResponded", (share: TaskShare & { status?: ShareStatus }) =>
    handlers.shareResponded(share),
  );
  connection.on("NotificationReceived", handlers.notificationReceived);
  connection.on("CommentAdded", handlers.commentAdded);
  connection.on("CommentUpdated", handlers.commentUpdated);
  connection.on("CommentDeleted", (payload: { taskId: string; commentId: string }) =>
    handlers.commentDeleted(payload.commentId),
  );
  connection.on("AssigneeChanged", handlers.assigneeChanged);
  connection.on("ActivityAdded", handlers.activityAdded);

  connection.onreconnected(async () => {
    const taskIds = [...joinedTaskIds];
    joinedTaskIds.clear();
    await syncRealtimeTaskGroups(taskIds);
  });

  await connection.start();
}

export async function stopRealtime() {
  const current = connection;
  connection = null;
  joinedTaskIds.clear();
  await current?.stop();
}

export async function syncRealtimeTaskGroups(taskIds: string[]) {
  if (!connection || connection.state !== signalR.HubConnectionState.Connected) return;

  const next = new Set(taskIds);
  const leaving = [...joinedTaskIds].filter((taskId) => !next.has(taskId));
  const joining = [...next].filter((taskId) => !joinedTaskIds.has(taskId));

  await Promise.allSettled(leaving.map((taskId) => connection?.invoke("LeaveTaskGroup", taskId)));
  leaving.forEach((taskId) => joinedTaskIds.delete(taskId));

  await Promise.allSettled(joining.map((taskId) => connection?.invoke("JoinTaskGroup", taskId)));
  joining.forEach((taskId) => joinedTaskIds.add(taskId));
}
