export const API_BASE_URL =
  import.meta.env.VITE_API_BASE_URL?.replace(/\/$/, "") ??
  "http://localhost:5050";

const ACCESS_TOKEN_KEY = "todoflow_access_token";
const REFRESH_TOKEN_KEY = "todoflow_refresh_token";

interface ApiResponse<T> {
  success: boolean;
  data: T;
  message?: string | null;
  errors?: Record<string, string[]> | null;
}

interface RefreshResponse {
  accessToken: string;
  refreshToken: string;
}

export class ApiError extends Error {
  status: number;
  errors?: Record<string, string[]>;

  constructor(message: string, status: number, errors?: Record<string, string[]>) {
    super(message);
    this.name = "ApiError";
    this.status = status;
    this.errors = errors;
  }
}

export function getAccessToken() {
  return localStorage.getItem(ACCESS_TOKEN_KEY);
}

export function getRefreshToken() {
  return localStorage.getItem(REFRESH_TOKEN_KEY);
}

export function setTokens(accessToken: string, refreshToken: string) {
  localStorage.setItem(ACCESS_TOKEN_KEY, accessToken);
  localStorage.setItem(REFRESH_TOKEN_KEY, refreshToken);
}

export function clearTokens() {
  localStorage.removeItem(ACCESS_TOKEN_KEY);
  localStorage.removeItem(REFRESH_TOKEN_KEY);
}

export async function apiRequest<T>(
  path: string,
  init: RequestInit = {},
  retry = true,
): Promise<T> {
  const token = getAccessToken();
  const headers = new Headers(init.headers);

  if (!headers.has("Content-Type") && init.body) {
    headers.set("Content-Type", "application/json");
  }
  if (token) {
    headers.set("Authorization", `Bearer ${token}`);
  }

  const response = await fetch(`${API_BASE_URL}${path}`, {
    ...init,
    headers,
  });

  if (response.status === 401 && retry && getRefreshToken()) {
    const refreshed = await refreshAccessToken();
    if (refreshed) {
      return apiRequest<T>(path, init, false);
    }
  }

  const payload = await readJson<ApiResponse<T>>(response);

  if (!response.ok || !payload.success) {
    throw new ApiError(
      payload.message ?? "Không thể kết nối máy chủ.",
      response.status,
      payload.errors ?? undefined,
    );
  }

  return payload.data;
}

async function refreshAccessToken() {
  const refreshToken = getRefreshToken();
  if (!refreshToken) return false;

  try {
    const response = await fetch(`${API_BASE_URL}/api/auth/refresh-token`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ refreshToken }),
    });
    const payload = await readJson<ApiResponse<RefreshResponse>>(response);
    if (!response.ok || !payload.success) {
      clearTokens();
      return false;
    }
    setTokens(payload.data.accessToken, payload.data.refreshToken);
    return true;
  } catch {
    clearTokens();
    return false;
  }
}

async function readJson<T>(response: Response): Promise<T> {
  const text = await response.text();
  if (!text) {
    return {
      success: response.ok,
      data: undefined,
      message: response.ok ? null : response.statusText,
      errors: null,
    } as T;
  }
  return JSON.parse(text) as T;
}
