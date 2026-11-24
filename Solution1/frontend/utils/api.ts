export const API_TOKEN_STORAGE_KEY = "token";

export class ApiError extends Error {
  readonly status: number;
  readonly data: unknown;
  readonly originalError?: unknown;

  constructor(
    message: string,
    status: number,
    data?: unknown,
    originalError?: unknown,
  ) {
    super(message);
    this.name = "ApiError";
    this.status = status;
    this.data = data;
    this.originalError = originalError;
  }
}

export type ApiFetchOptions = Omit<RequestInit, "body" | "headers"> & {
  body?: unknown;
  headers?: HeadersInit;
};

function getErrorMessage(data: unknown, status: number): string {
  if (typeof data === "string" && data.trim()) {
    return data;
  }

  if (data && typeof data === "object") {
    const record = data as Record<string, unknown>;
    if (typeof record.message === "string" && record.message.trim()) {
      return record.message;
    }

    if (typeof record.error === "string" && record.error.trim()) {
      return record.error;
    }

    if (
      record.error &&
      typeof record.error === "object" &&
      typeof (record.error as Record<string, unknown>).message === "string"
    ) {
      return (record.error as Record<string, string>).message;
    }

    if (typeof record.detail === "string" && record.detail.trim()) {
      return record.detail;
    }

    if (record.errors && typeof record.errors === "object") {
      const messages = Object.values(record.errors).flatMap((value) =>
        Array.isArray(value)
          ? value.filter((message): message is string => typeof message === "string")
          : typeof value === "string"
            ? [value]
            : [],
      );
      if (messages.length) {
        return messages.join(" ");
      }
    }

    if (typeof record.title === "string" && record.title.trim()) {
      return record.title;
    }
  }

  return `API request failed with status ${status}.`;
}

export async function apiFetch<T = unknown>(
  path: string,
  options: ApiFetchOptions = {},
): Promise<T> {
  const baseUrl = process.env.NEXT_PUBLIC_API_BASE_URL?.replace(/\/+$/, "");
  if (!baseUrl) {
    throw new Error("NEXT_PUBLIC_API_BASE_URL is not configured.");
  }

  const { body, headers: requestHeaders, ...requestOptions } = options;
  const headers = new Headers(requestHeaders);
  if (!headers.has("Accept")) {
    headers.set("Accept", "application/json");
  }
  if (body !== undefined && !headers.has("Content-Type")) {
    headers.set("Content-Type", "application/json");
  }

  const token =
    typeof window === "undefined"
      ? null
      : window.sessionStorage.getItem(API_TOKEN_STORAGE_KEY);
  if (token && !headers.has("Authorization")) {
    headers.set("Authorization", `Bearer ${token}`);
  }

  let response: Response;
  try {
    response = await fetch(`${baseUrl}/${path.replace(/^\/+/, "")}`, {
      ...requestOptions,
      headers,
      body: body === undefined ? undefined : JSON.stringify(body),
    });
  } catch (error) {
    const detail = error instanceof Error ? `: ${error.message}` : "";
    throw new ApiError(`Network request failed${detail}`, 0, undefined, error);
  }

  const responseText = await response.text();
  let data: unknown;
  if (responseText) {
    try {
      data = JSON.parse(responseText);
    } catch {
      if (response.ok) {
        throw new ApiError(
          "The API returned an invalid JSON response.",
          response.status,
          responseText,
        );
      }
      data = responseText;
    }
  }

  if (!response.ok) {
    throw new ApiError(
      getErrorMessage(data, response.status),
      response.status,
      data,
    );
  }

  return data as T;
}
