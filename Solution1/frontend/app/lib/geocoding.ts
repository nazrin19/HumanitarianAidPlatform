export type GeocodingStatus =
  | "Found"
  | "NotFound"
  | "Ambiguous"
  | "ProviderUnavailable"
  | "InvalidInput"
  | "Cancelled";

export type GeocodingCandidate = {
  latitude: number;
  longitude: number;
  displayAddress: string;
};

export type GeocodingResult = {
  status: GeocodingStatus;
  latitude: number | null;
  longitude: number | null;
  displayAddress: string | null;
  candidates: GeocodingCandidate[];
  isAmbiguous: boolean;
  errorCode: string | null;
  errorMessage: string | null;
};

export type GeocodingLookupOutcome =
  | {
      kind:
        | "found"
        | "ambiguous"
        | "notFound"
        | "invalidInput"
        | "providerUnavailable"
        | "cancelled";
      httpStatus: number;
      result?: GeocodingResult;
      message?: string;
    }
  | {
      kind: "validationError";
      httpStatus: 400;
      message: string;
      errors: Record<string, string[]>;
    }
  | {
      kind: "unauthorized";
      httpStatus: 401;
      message: string;
    }
  | {
      kind: "forbidden";
      httpStatus: 403;
      message: string;
    }
  | {
      kind: "networkError" | "invalidResponse" | "httpError";
      httpStatus?: number;
      message: string;
    };

type GeocodingResultKind =
    | "found"
    | "ambiguous"
    | "notFound"
    | "invalidInput"
    | "providerUnavailable"
    | "cancelled";

export type GeocodingRequestOptions = {
  accessToken?: string;
  signal?: AbortSignal;
  baseUrl?: string;
};

const API_BASE_URL = (
  process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5001"
).replace(/\/+$/, "");

const statusNames: Record<number, GeocodingStatus> = {
  0: "Found",
  1: "NotFound",
  2: "Ambiguous",
  3: "ProviderUnavailable",
  4: "InvalidInput",
  5: "Cancelled",
};

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null;
}

function readStatus(value: unknown): GeocodingStatus | null {
  if (
    value === "Found" ||
    value === "NotFound" ||
    value === "Ambiguous" ||
    value === "ProviderUnavailable" ||
    value === "InvalidInput" ||
    value === "Cancelled"
  ) {
    return value;
  }
  if (typeof value === "number") return statusNames[value] ?? null;
  return null;
}

function parseResult(value: unknown): GeocodingResult | null {
  if (!isRecord(value)) return null;
  const status = readStatus(value.status);
  if (
    !status ||
    !Array.isArray(value.candidates) ||
    typeof value.isAmbiguous !== "boolean" ||
    !(value.latitude === null || typeof value.latitude === "number") ||
    !(value.longitude === null || typeof value.longitude === "number") ||
    !(value.displayAddress === null || typeof value.displayAddress === "string")
  ) {
    return null;
  }

  const candidates: GeocodingCandidate[] = [];
  for (const candidate of value.candidates) {
    if (
      !isRecord(candidate) ||
      typeof candidate.latitude !== "number" ||
      typeof candidate.longitude !== "number" ||
      typeof candidate.displayAddress !== "string"
    ) {
      return null;
    }
    candidates.push({
      latitude: candidate.latitude,
      longitude: candidate.longitude,
      displayAddress: candidate.displayAddress,
    });
  }

  return {
    status,
    latitude: value.latitude,
    longitude: value.longitude,
    displayAddress: value.displayAddress,
    candidates,
    isAmbiguous: value.isAmbiguous,
    errorCode: typeof value.errorCode === "string" ? value.errorCode : null,
    errorMessage:
      typeof value.errorMessage === "string" ? value.errorMessage : null,
  };
}

function kindForStatus(status: GeocodingStatus): GeocodingResultKind {
  switch (status) {
    case "Found":
      return "found";
    case "Ambiguous":
      return "ambiguous";
    case "NotFound":
      return "notFound";
    case "InvalidInput":
      return "invalidInput";
    case "ProviderUnavailable":
      return "providerUnavailable";
    case "Cancelled":
      return "cancelled";
  }
}

function validationErrors(value: unknown): Record<string, string[]> | null {
  if (!isRecord(value) || !isRecord(value.errors)) return null;
  const errors: Record<string, string[]> = {};
  for (const [field, messages] of Object.entries(value.errors)) {
    if (
      Array.isArray(messages) &&
      messages.every((item) => typeof item === "string")
    ) {
      errors[field] = messages;
    }
  }
  return errors;
}

function errorMessage(value: unknown, fallback: string): string {
  if (!isRecord(value)) return fallback;
  if (typeof value.errorMessage === "string") return value.errorMessage;
  if (typeof value.detail === "string") return value.detail;
  if (typeof value.title === "string") return value.title;
  return fallback;
}

async function postGeocoding(
  endpoint: "forward" | "reverse",
  payload: Record<string, string | number>,
  options: GeocodingRequestOptions = {},
): Promise<GeocodingLookupOutcome> {
  try {
    const headers: Record<string, string> = {
      "Content-Type": "application/json",
    };
    if (options.accessToken) {
      headers.Authorization = `Bearer ${options.accessToken}`;
    }

    const response = await fetch(
      `${(options.baseUrl ?? API_BASE_URL).replace(/\/+$/, "")}/api/geocoding/${endpoint}`,
      {
        method: "POST",
        headers,
        body: JSON.stringify(payload),
        signal: options.signal,
      },
    );
    const body: unknown = await response.json().catch(() => undefined);
    if (response.status === 401) {
      return {
        kind: "unauthorized",
        httpStatus: 401,
        message: "Your session has expired. Please sign in again.",
      };
    }
    if (response.status === 403) {
      return {
        kind: "forbidden",
        httpStatus: 403,
        message: "You do not have permission to search for locations.",
      };
    }
    const result = parseResult(body);

    if (result) {
      return {
        kind: kindForStatus(result.status),
        httpStatus: response.status,
        result,
      };
    }
    if (response.status === 400) {
      const errors = validationErrors(body);
      if (errors) {
        return {
          kind: "validationError",
          httpStatus: 400,
          message: errorMessage(body, "Please check the geocoding request."),
          errors,
        };
      }
      return {
        kind: "invalidResponse",
        httpStatus: 400,
        message: "The server returned an invalid geocoding response.",
      };
    }
    if (response.status === 404) {
      return {
        kind: "notFound",
        httpStatus: 404,
        message: "No matching location was found.",
      };
    }
    if (response.status === 499) {
      return {
        kind: "cancelled",
        httpStatus: 499,
        message: "The lookup was cancelled.",
      };
    }
    if (response.status === 503) {
      return {
        kind: "providerUnavailable",
        httpStatus: 503,
        message: "The geocoding provider is temporarily unavailable.",
      };
    }
    return {
      kind: response.ok ? "invalidResponse" : "httpError",
      httpStatus: response.status,
      message: response.ok
        ? "The server returned an invalid geocoding response."
        : "The geocoding request could not be completed.",
    };
  } catch (error) {
    if (error instanceof DOMException && error.name === "AbortError") {
      return {
        kind: "cancelled",
        httpStatus: 499,
        message: "The lookup was cancelled.",
      };
    }
    return {
      kind: "networkError",
      message: "Unable to reach the geocoding service. Check your connection and try again.",
    };
  }
}

export function forwardGeocode(
  query: string,
  options?: GeocodingRequestOptions,
): Promise<GeocodingLookupOutcome> {
  const normalizedQuery = query.trim();
  if (normalizedQuery.length < 4 || normalizedQuery.length > 300) {
    return Promise.resolve({
      kind: "invalidInput",
      httpStatus: 0,
      message: "Enter a location between 4 and 300 characters.",
    });
  }
  return postGeocoding("forward", { query: normalizedQuery }, options);
}

export function reverseGeocode(
  latitude: number,
  longitude: number,
  options?: GeocodingRequestOptions,
): Promise<GeocodingLookupOutcome> {
  if (
    !Number.isFinite(latitude) ||
    latitude < -90 ||
    latitude > 90 ||
    !Number.isFinite(longitude) ||
    longitude < -180 ||
    longitude > 180
  ) {
    return Promise.resolve({
      kind: "invalidInput",
      httpStatus: 0,
      message: "Enter valid latitude and longitude coordinates.",
    });
  }
  return postGeocoding("reverse", { latitude, longitude }, options);
}
