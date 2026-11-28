"use client";

import Image from "next/image";
import Link from "next/link";
import {
  CSSProperties,
  type FormEvent,
  useEffect,
  useMemo,
  useRef,
  useState,
} from "react";
import { useRouter } from "next/navigation";
import dynamic from "next/dynamic";
import type { Map as LeafletMapInstance } from "leaflet";
import {
  AUTH_SESSION_LOADING,
  clearSessionCredentials,
  useSessionRole,
} from "../lib/auth-session";
import { forwardGeocode, reverseGeocode } from "../lib/geocoding";
import styles from "./page.module.css";
import mapStyles from "./LiveAidMap.module.css";
import type {
  MapGeocodedPoint,
  MapMarkerType,
  MapRequestPoint,
  MapShelterPoint,
  MapResourcePoint,
  MapRouteOverlay,
  MapServiceArea,
  MapSelection,
} from "./LiveAidMap";
import AlertModal from "./AlertModal";
import type { GeocodingCandidate } from "../lib/geocoding";

const LiveAidMap = dynamic(() => import("./LiveAidMap"), {
  ssr: false,
  loading: () => <div className={mapStyles.mapLoading}>Loading map…</div>,
});

type Urgency = "Critical" | "High" | "Medium" | "Low";
type RequestStatus =
  | "Submitted"
  | "PendingReview"
  | "Approved"
  | "Deferred"
  | "Assigned"
  | "Fulfilled"
  | "Rejected";
type Layer = "req" | "sh" | "res";
type Selection = MapSelection;

type AidRequest = {
  id: string;
  cat: string;
  description: string;
  address: string | null;
  createdAt: string;
  latitude: number | null;
  longitude: number | null;
  lvl: Urgency | null;
  sc: number | null;
  st: RequestStatus;
};

type ApiAidRequest = {
  id: string;
  category: string;
  description: string;
  address: string | null;
  createdAt: string;
  latitude: number | null;
  longitude: number | null;
  urgencyLevel: Urgency | null;
  urgencyScore: number | null;
  status: RequestStatus;
};

type Shelter = {
  id: string;
  n: string;
  p: number;
  u: number;
  c: number;
  latitude: number | null;
  longitude: number | null;
};

type Depot = {
  id: string;
  n: string;
  item: string;
  quantityAvailable: number;
  description: string | null;
  latitude: number | null;
  longitude: number | null;
};

type ApiShelter = {
  id: string;
  name: string;
  capacity: number;
  currentOccupancy: number;
  latitude: number | null;
  longitude: number | null;
};

type ApiResource = {
  id: string;
  name: string;
  category: string;
  quantityAvailable: number;
  description: string | null;
  latitude: number | null;
  longitude: number | null;
};

type NotificationChannel = "Email" | "Sms" | "InApp" | "Unknown";

type ApiNotificationHistoryItem = {
  id: string;
  eventId: string | null;
  recipient: string;
  channel: NotificationChannel;
  createdAt: string;
  status: string;
  succeeded: boolean;
  completedAt: string | null;
  correlationId: string | null;
  attemptCount: number;
  errorCode: string | null;
  errorMessage: string | null;
};

type InboxNotification = {
  id: string;
  title: string;
  message: string;
  eventType: "ApplicationStatus" | "ShelterAssignment" | "EmergencyAnnouncement" | "General";
  language: string;
  createdAt: string;
  isRead: boolean;
};

type ApiMapRoute = {
  distanceKm: number;
  routeGeoJson: string;
  isFallback: boolean;
  message: string;
};

type NotificationDeliveryResult = {
  succeeded: boolean;
  status: string;
  channel: string;
  attemptedAt?: string;
  errorCode?: string | null;
  errorMessage?: string | null;
  correlationId?: string | null;
  attemptCount?: number;
  nextRetryAt?: string | null;
  isDuplicate?: boolean;
};

type NotificationLogEntry = {
  id: string;
  eventId?: string | null;
  recipient: string;
  channel: NotificationChannel;
  createdAt: string;
  status: string;
  succeeded: boolean;
  correlationId?: string | null;
  attemptCount?: number;
  isDuplicate?: boolean;
  errorCode?: string | null;
  errorMessage?: string | null;
};

const urgencyColors: Record<Urgency, string> = {
  Critical: "#A31D1D",
  High: "#D93025",
  Medium: "#F29900",
  Low: "#1FA463",
};

const statusClass: Record<RequestStatus, string> = {
  Submitted: "pending",
  PendingReview: "pending",
  Approved: "approved",
  Deferred: "deferred",
  Assigned: "allocated",
  Fulfilled: "completed",
  Rejected: "rejected",
};

const API_BASE_URL = (
  process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5001"
).replace(/\/+$/, "");

const requestStatuses: RequestStatus[] = [
  "Submitted",
  "PendingReview",
  "Approved",
  "Deferred",
  "Assigned",
  "Fulfilled",
  "Rejected",
];

const urgencyLevels: Urgency[] = ["Critical", "High", "Medium", "Low"];
const requestStaffRoles = new Set([
  "Coordinator",
  "CaseManager",
  "ResourceManager",
  "ShelterAdmin",
  "Admin",
]);
const notificationRoles = new Set(["Admin", "Coordinator", "CaseManager"]);

function isNotificationDeliveryResult(
  value: unknown,
): value is NotificationDeliveryResult {
  if (!value || typeof value !== "object") return false;
  const result = value as Record<string, unknown>;
  return (
    typeof result.succeeded === "boolean" &&
    typeof result.status === "string" &&
    typeof result.channel === "string"
  );
}

function isApiNotificationHistoryItem(
  value: unknown,
): value is ApiNotificationHistoryItem {
  if (!value || typeof value !== "object") return false;
  const item = value as Record<string, unknown>;
  return (
    typeof item.id === "string" &&
    (typeof item.eventId === "string" || item.eventId === null) &&
    typeof item.recipient === "string" &&
    (item.channel === "Email" ||
      item.channel === "Sms" ||
      item.channel === "InApp" ||
      item.channel === "Unknown") &&
    typeof item.createdAt === "string" &&
    typeof item.status === "string" &&
    typeof item.succeeded === "boolean" &&
    (typeof item.completedAt === "string" || item.completedAt === null) &&
    (typeof item.correlationId === "string" || item.correlationId === null) &&
    typeof item.attemptCount === "number" &&
    (typeof item.errorCode === "string" || item.errorCode === null) &&
    (typeof item.errorMessage === "string" || item.errorMessage === null)
  );
}

function isInboxNotification(value: unknown): value is InboxNotification {
  if (!value || typeof value !== "object") return false;
  const item = value as Record<string, unknown>;
  return (
    typeof item.id === "string" &&
    typeof item.title === "string" &&
    typeof item.message === "string" &&
    (item.eventType === "ApplicationStatus" ||
      item.eventType === "ShelterAssignment" ||
      item.eventType === "EmergencyAnnouncement" ||
      item.eventType === "General") &&
    typeof item.language === "string" &&
    typeof item.createdAt === "string" &&
    typeof item.isRead === "boolean"
  );
}

function isApiMapRoute(value: unknown): value is ApiMapRoute {
  if (!value || typeof value !== "object") return false;
  const route = value as Record<string, unknown>;
  return (
    typeof route.distanceKm === "number" &&
    typeof route.routeGeoJson === "string" &&
    typeof route.isFallback === "boolean" &&
    typeof route.message === "string"
  );
}

function isMapServiceArea(value: unknown): value is MapServiceArea {
  if (!value || typeof value !== "object") return false;
  const area = value as Record<string, unknown>;
  return (
    typeof area.id === "string" &&
    typeof area.name === "string" &&
    typeof area.geoJson === "string"
  );
}

function mapApiNotificationHistoryItem(
  item: ApiNotificationHistoryItem,
): NotificationLogEntry {
  return {
    id: item.id,
    eventId: item.eventId,
    recipient: item.recipient,
    channel: item.channel,
    createdAt: item.createdAt,
    status: item.status,
    succeeded: item.succeeded,
    correlationId: item.correlationId,
    attemptCount: item.attemptCount,
    errorCode: item.errorCode,
    errorMessage: item.errorMessage,
  };
}

function isNotificationPersistenceWarning(entry: NotificationLogEntry): boolean {
  return (
    entry.errorCode === "NOTIFICATION_PERSISTENCE_FAILED" ||
    entry.errorCode === "NOTIFICATION_STATUS_PERSISTENCE_FAILED"
  );
}

function getNotificationErrorMessage(value: unknown, fallback: string): string {
  if (typeof value === "string" && value.trim()) return value;
  if (!value || typeof value !== "object") return fallback;
  const response = value as Record<string, unknown>;
  if (response.error && typeof response.error === "object") {
    const error = response.error as Record<string, unknown>;
    if (typeof error.message === "string" && error.message.trim()) {
      return error.message;
    }
  }
  if (typeof response.errorMessage === "string" && response.errorMessage.trim()) {
    return response.errorMessage;
  }
  if (typeof response.detail === "string" && response.detail.trim()) {
    return response.detail;
  }
  if (typeof response.title === "string" && response.title.trim()) {
    return response.title;
  }
  if (typeof response.message === "string" && response.message.trim()) {
    return response.message;
  }
  if (response.errors && typeof response.errors === "object") {
    const messages = Object.values(response.errors).flatMap((messages) =>
      Array.isArray(messages)
        ? messages.filter(
            (message): message is string => typeof message === "string",
          )
        : [],
    );
    if (messages.length > 0) return messages.join(" ");
  }
  return fallback;
}

function maskNotificationRecipient(recipient: string): string {
  const trimmed = recipient.trim();
  const atIndex = trimmed.indexOf("@");
  if (atIndex > 0) {
    return `${trimmed.slice(0, Math.min(2, atIndex))}***${trimmed.slice(atIndex)}`;
  }
  const digits = trimmed.replace(/\D/g, "");
  return digits.length >= 4 ? `***${digits.slice(-4)}` : "***";
}

function isApiAidRequest(value: unknown): value is ApiAidRequest {
  if (!value || typeof value !== "object") return false;
  const request = value as Record<string, unknown>;
  return (
    typeof request.id === "string" &&
    typeof request.category === "string" &&
    typeof request.description === "string" &&
    typeof request.status === "string" &&
    requestStatuses.includes(request.status as RequestStatus) &&
    (request.urgencyLevel === null ||
      typeof request.urgencyLevel === "string" &&
        urgencyLevels.includes(request.urgencyLevel as Urgency)) &&
    (request.urgencyScore === null ||
      typeof request.urgencyScore === "number") &&
    (request.latitude === null || typeof request.latitude === "number") &&
    (request.longitude === null || typeof request.longitude === "number") &&
    (request.address === null || typeof request.address === "string") &&
    typeof request.createdAt === "string"
  );
}

function mapApiRequest(request: ApiAidRequest): AidRequest {
  return {
    id: request.id,
    cat: request.category,
    description: request.description,
    address: request.address,
    createdAt: request.createdAt,
    latitude: request.latitude,
    longitude: request.longitude,
    lvl: request.urgencyLevel,
    sc: request.urgencyScore,
    st: request.status,
  };
}

function isApiShelter(value: unknown): value is ApiShelter {
  if (!value || typeof value !== "object") return false;
  const shelter = value as Record<string, unknown>;
  return (
    typeof shelter.id === "string" &&
    typeof shelter.name === "string" &&
    typeof shelter.capacity === "number" &&
    typeof shelter.currentOccupancy === "number" &&
    (shelter.latitude === null || typeof shelter.latitude === "number") &&
    (shelter.longitude === null || typeof shelter.longitude === "number")
  );
}

function isApiResource(value: unknown): value is ApiResource {
  if (!value || typeof value !== "object") return false;
  const resource = value as Record<string, unknown>;
  return (
    typeof resource.id === "string" &&
    typeof resource.name === "string" &&
    typeof resource.category === "string" &&
    typeof resource.quantityAvailable === "number" &&
    (resource.description === null || typeof resource.description === "string") &&
    (resource.latitude === null || typeof resource.latitude === "number") &&
    (resource.longitude === null || typeof resource.longitude === "number")
  );
}

function mapApiShelters(rows: ApiShelter[]): Shelter[] {
  return rows.map((shelter) => ({
    id: shelter.id,
    n: shelter.name,
    p:
      shelter.capacity > 0
        ? Math.round((shelter.currentOccupancy / shelter.capacity) * 100)
        : 0,
    u: shelter.currentOccupancy,
    c: shelter.capacity,
    latitude: shelter.latitude,
    longitude: shelter.longitude,
  }));
}

function mapResources(rows: ApiResource[]): Depot[] {
  return rows.map((resource) => ({
    id: resource.id,
    n: resource.name,
    item: resource.category,
    quantityAvailable: resource.quantityAvailable,
    description: resource.description,
    latitude: resource.latitude,
    longitude: resource.longitude,
  }));
}

function hasValidCoordinates<
  T extends { latitude: number | null; longitude: number | null },
>(value: T): value is T & { latitude: number; longitude: number } {
  return (
    value.latitude !== null &&
    value.longitude !== null &&
    Number.isFinite(value.latitude) &&
    Number.isFinite(value.longitude) &&
    value.latitude >= -90 &&
    value.latitude <= 90 &&
    value.longitude >= -180 &&
    value.longitude <= 180
  );
}

function distanceKm(
  latitude1: number,
  longitude1: number,
  latitude2: number,
  longitude2: number,
) {
  const earthRadiusKm = 6371;
  const toRadians = (degrees: number) => (degrees * Math.PI) / 180;
  const deltaLatitude = toRadians(latitude2 - latitude1);
  const deltaLongitude = toRadians(longitude2 - longitude1);
  const lat1 = toRadians(latitude1);
  const lat2 = toRadians(latitude2);
  const a =
    Math.sin(deltaLatitude / 2) * Math.sin(deltaLatitude / 2) +
    Math.cos(lat1) *
      Math.cos(lat2) *
      Math.sin(deltaLongitude / 2) *
      Math.sin(deltaLongitude / 2);
  return earthRadiusKm * 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a));
}

export default function DashboardPage() {
  const router = useRouter();
  const accountRole = useSessionRole();
  const [requests, setRequests] = useState<AidRequest[]>([]);
  const [requestPage, setRequestPage] = useState(1);
  const [totalRequestCount, setTotalRequestCount] = useState(0);
  const [shelters, setShelters] = useState<Shelter[]>([]);
  const [depots, setDepots] = useState<Depot[]>([]);
  const [isLoadingRequests, setIsLoadingRequests] = useState(true);
  const [isLoadingShelters, setIsLoadingShelters] = useState(true);
  const [isLoadingResources, setIsLoadingResources] = useState(true);
  const [isRedirecting, setIsRedirecting] = useState(false);
  const [requestLoadError, setRequestLoadError] = useState("");
  const [requestActionError, setRequestActionError] = useState("");
  const [updatingRequestId, setUpdatingRequestId] = useState<string | null>(null);
  const [shelterLoadError, setShelterLoadError] = useState("");
  const [resourceLoadError, setResourceLoadError] = useState("");
  const [layers, setLayers] = useState<Record<Layer, boolean>>({
    req: true,
    sh: true,
    res: true,
  });
  const [urgency, setUrgency] = useState("all");
  const [query, setQuery] = useState("");
  const [selection, setSelection] = useState<Selection>(null);
  const [toast, setToast] = useState("");
  const [showToast, setShowToast] = useState(false);
  const [notificationRecipient, setNotificationRecipient] = useState("");
  const [notificationChannel, setNotificationChannel] =
    useState<NotificationChannel>("Email");
  const [notificationLogs, setNotificationLogs] = useState<
    NotificationLogEntry[]
  >([]);
  const [isLoadingNotificationHistory, setIsLoadingNotificationHistory] =
    useState(false);
  const [notificationHistoryError, setNotificationHistoryError] = useState("");
  const [notificationRefreshVersion, setNotificationRefreshVersion] =
    useState(0);
  const [inboxNotifications, setInboxNotifications] = useState<
    InboxNotification[]
  >([]);
  const [isLoadingInbox, setIsLoadingInbox] = useState(false);
  const [notificationInboxError, setNotificationInboxError] = useState("");
  const [isNotificationsOpen, setIsNotificationsOpen] = useState(false);
  const [notificationTab, setNotificationTab] = useState<"All" | "Unread">(
    "All",
  );
  const [activeAlert, setActiveAlert] = useState<InboxNotification | null>(null);
  const [isMarkingAlertRead, setIsMarkingAlertRead] = useState(false);
  const [serviceAreas, setServiceAreas] = useState<MapServiceArea[]>([]);
  const [serviceAreaError, setServiceAreaError] = useState("");
  const [mapRoute, setMapRoute] = useState<MapRouteOverlay | null>(null);
  const [routeMessage, setRouteMessage] = useState("");
  const [isSendingTestNotification, setIsSendingTestNotification] =
    useState(false);
  const [mapInstance, setMapInstance] = useState<LeafletMapInstance | null>(
    null,
  );
  const [userLocation, setUserLocation] = useState<{
    latitude: number;
    longitude: number;
  } | null>(null);
  const [locationMessage, setLocationMessage] = useState("");
  const [locationQuery, setLocationQuery] = useState("");
  const [geocodingFeedback, setGeocodingFeedback] = useState<{
    kind: "status" | "error";
    message: string;
    details?: string[];
  } | null>(null);
  const [geocodingCandidates, setGeocodingCandidates] = useState<
    GeocodingCandidate[]
  >([]);
  const [geocodedLocation, setGeocodedLocation] =
    useState<MapGeocodedPoint | null>(null);
  const [isGeocoding, setIsGeocoding] = useState(false);
  const geocodingController = useRef<AbortController | null>(null);
  const mapCardRef = useRef<HTMLElement>(null);
  const notificationPopoverRef = useRef<HTMLDivElement>(null);
  const lastAlertId = useRef<string | null>(null);
  const toastTimer = useRef<ReturnType<typeof setTimeout> | null>(null);
  const normalizedQuery = query.trim().toLowerCase();
  const normalizedLocationQuery = locationQuery.trim();
  const isLocationQueryValid =
    normalizedLocationQuery.length >= 4 &&
    normalizedLocationQuery.length <= 300;

  useEffect(() => {
    return () => {
      const controller = geocodingController.current;
      geocodingController.current = null;
      controller?.abort();
      if (toastTimer.current) clearTimeout(toastTimer.current);
    };
  }, []);

  useEffect(() => {
    const controller = new AbortController();
    let active = true;
    const token = window.sessionStorage.getItem("authToken");
    const role = accountRole;

    if (role === AUTH_SESSION_LOADING) {
      return () => {
        active = false;
        controller.abort();
      };
    }

    if (!token || !role) {
      router.replace("/login");
      return () => {
        active = false;
        controller.abort();
      };
    }

    if (role === "Admin") {
      router.replace("/admin");
      return () => {
        active = false;
        controller.abort();
      };
    }

    if (role !== "Citizen" && !requestStaffRoles.has(role)) {
      router.replace("/portal");
      return () => {
        active = false;
        controller.abort();
      };
    }

    async function loadRequests() {
      try {
        const requestPath =
          role === "Citizen"
            ? `/api/requests/mine?page=${requestPage}&pageSize=100`
            : `/api/requests?page=${requestPage}&pageSize=100`;
        const response = await fetch(`${API_BASE_URL}${requestPath}`, {
          method: "GET",
          headers: { Authorization: `Bearer ${token}` },
          signal: controller.signal,
        });

        if (response.status === 401) {
          clearSessionCredentials();
          setIsRedirecting(true);
          router.replace("/login");
          return;
        }

        const responseText = await response.text();
        let responseBody: unknown;
        if (responseText) {
          try {
            responseBody = JSON.parse(responseText);
          } catch {
            if (response.ok) {
              throw new Error("The server returned an invalid requests response.");
            }
          }
        }

        if (!response.ok) {
          let message: string | undefined;
          if (responseBody && typeof responseBody === "object") {
            const body = responseBody as {
              detail?: unknown;
              message?: unknown;
              title?: unknown;
            };
            const errorMessage = body.detail ?? body.message ?? body.title;
            if (typeof errorMessage === "string") message = errorMessage;
          }
          throw new Error(
            message ||
              `Unable to load requests (${response.status} ${response.statusText}).`,
          );
        }

        if (!responseBody || typeof responseBody !== "object") {
          throw new Error("The server returned an invalid requests response.");
        }
        const items = (responseBody as { items?: unknown }).items;
        const totalCount = (responseBody as { totalCount?: unknown }).totalCount;
        if (
          !Array.isArray(items) ||
          !items.every(isApiAidRequest) ||
          typeof totalCount !== "number"
        ) {
          throw new Error("The server returned requests in an unexpected format.");
        }

        if (active) {
          setRequests(items.map(mapApiRequest));
          setTotalRequestCount(totalCount);
          setRequestLoadError("");
        }
      } catch (error) {
        if (
          active &&
          !(error instanceof DOMException && error.name === "AbortError")
        ) {
          setRequestLoadError(
            error instanceof Error
              ? error.message
              : "Unable to load requests. Please try again.",
          );
        }
      } finally {
        if (active) setIsLoadingRequests(false);
      }
    }

    void loadRequests();
    return () => {
      active = false;
      controller.abort();
    };
  }, [accountRole, requestPage, router]);

  useEffect(() => {
    if (
      accountRole === AUTH_SESSION_LOADING ||
      !notificationRoles.has(accountRole)
    ) {
      return;
    }

    const controller = new AbortController();
    let active = true;
    const token = window.sessionStorage.getItem("authToken");

    if (!token) {
      router.replace("/login");
      return () => {
        active = false;
        controller.abort();
      };
    }

    async function loadNotificationHistory() {
      setIsLoadingNotificationHistory(true);
      setNotificationHistoryError("");

      try {
        const response = await fetch(
          `${API_BASE_URL}/api/notifications?limit=50`,
          {
            headers: { Authorization: `Bearer ${token}` },
            signal: controller.signal,
          },
        );

        if (response.status === 401) {
          clearSessionCredentials();
          setIsRedirecting(true);
          router.replace("/login");
          return;
        }

        const responseBody: unknown = await response.json().catch(() => undefined);
        if (!response.ok) {
          throw new Error(
            getNotificationErrorMessage(
              responseBody,
              `Unable to load notifications (${response.status}).`,
            ),
          );
        }

        if (
          !Array.isArray(responseBody) ||
          !responseBody.every(isApiNotificationHistoryItem)
        ) {
          throw new Error(
            "The server returned notifications in an unexpected format.",
          );
        }

        if (active) {
          const historyEntries = responseBody.map(mapApiNotificationHistoryItem);
          setNotificationLogs((currentLogs) => {
            const localPersistenceOutcomes = currentLogs.filter(
              (entry) =>
                Boolean(entry.eventId) &&
                isNotificationPersistenceWarning(entry),
            );
            const localOutcomesByEventId = new Map<
              string,
              NotificationLogEntry
            >();
            for (const entry of localPersistenceOutcomes) {
              if (entry.eventId) {
                localOutcomesByEventId.set(entry.eventId, entry);
              }
            }
            const serverEventIds = new Set(
              historyEntries
                .map((entry) => entry.eventId)
                .filter((eventId): eventId is string => Boolean(eventId)),
            );
            const mergedHistory = historyEntries.map((entry) =>
              entry.eventId
                ? (localOutcomesByEventId.get(entry.eventId) ?? entry)
                : entry,
            );
            const unpersistedOutcomes = localPersistenceOutcomes.filter(
              (entry) =>
                !entry.eventId || !serverEventIds.has(entry.eventId),
            );

            return [...unpersistedOutcomes, ...mergedHistory].slice(0, 50);
          });
          setNotificationHistoryError("");
        }
      } catch (error) {
        if (
          active &&
          !(error instanceof DOMException && error.name === "AbortError")
        ) {
          setNotificationHistoryError(
            error instanceof Error
              ? error.message
              : "Unable to load notifications. Please try again.",
          );
        }
      } finally {
        if (active) setIsLoadingNotificationHistory(false);
      }
    }

    void loadNotificationHistory();
    return () => {
      active = false;
      controller.abort();
    };
  }, [accountRole, notificationRefreshVersion, router]);

  useEffect(() => {
    if (accountRole === AUTH_SESSION_LOADING) return;

    const controller = new AbortController();
    let active = true;
    const token = window.sessionStorage.getItem("authToken");
    if (!token) {
      router.replace("/login");
      return () => {
        active = false;
        controller.abort();
      };
    }

    async function loadInbox() {
      setIsLoadingInbox(true);
      setNotificationInboxError("");
      try {
        const response = await fetch(
          `${API_BASE_URL}/api/notifications/inbox?limit=30`,
          {
            headers: { Authorization: `Bearer ${token}` },
            signal: controller.signal,
          },
        );
        if (response.status === 401) {
          clearSessionCredentials();
          setIsRedirecting(true);
          router.replace("/login");
          return;
        }
        const body: unknown = await response.json().catch(() => undefined);
        if (!response.ok) {
          throw new Error(
            getNotificationErrorMessage(
              body,
              `Unable to load your notifications (${response.status}).`,
            ),
          );
        }
        if (!Array.isArray(body) || !body.every(isInboxNotification)) {
          throw new Error("The inbox response had an unexpected format.");
        }
        if (active) setInboxNotifications(body);
      } catch (error) {
        if (
          active &&
          !(error instanceof DOMException && error.name === "AbortError")
        ) {
          setNotificationInboxError(
            error instanceof Error
              ? error.message
              : "Unable to load your notifications.",
          );
        }
      } finally {
        if (active) setIsLoadingInbox(false);
      }
    }

    void loadInbox();
    return () => {
      active = false;
      controller.abort();
    };
  }, [accountRole, notificationRefreshVersion, router]);

  useEffect(() => {
    if (accountRole === AUTH_SESSION_LOADING) return;

    const controller = new AbortController();
    let active = true;
    const token = window.sessionStorage.getItem("authToken");
    if (!token) return () => controller.abort();

    async function loadServiceAreas() {
      setServiceAreaError("");
      try {
        const response = await fetch(
          `${API_BASE_URL}/api/mapping/service-areas`,
          {
            headers: { Authorization: `Bearer ${token}` },
            signal: controller.signal,
          },
        );
        if (response.status === 401) {
          clearSessionCredentials();
          setIsRedirecting(true);
          router.replace("/login");
          return;
        }
        const body: unknown = await response.json().catch(() => undefined);
        if (!response.ok) {
          throw new Error(`Service areas are unavailable (${response.status}).`);
        }
        if (!Array.isArray(body) || !body.every(isMapServiceArea)) {
          throw new Error("The service-area response had an unexpected format.");
        }
        if (active) setServiceAreas(body);
      } catch (error) {
        if (
          active &&
          !(error instanceof DOMException && error.name === "AbortError")
        ) {
          setServiceAreaError(
            error instanceof Error
              ? error.message
              : "Service-area layers are unavailable.",
          );
        }
      }
    }

    void loadServiceAreas();
    return () => {
      active = false;
      controller.abort();
    };
  }, [accountRole, router]);

  useEffect(() => {
    if (!userLocation || !geocodedLocation) {
      setMapRoute(null);
      setRouteMessage("");
      return;
    }

    const destinationLocation = geocodedLocation;
    const controller = new AbortController();
    let active = true;
    const token = window.sessionStorage.getItem("authToken");
    if (!token) {
      setMapRoute(null);
      setRouteMessage("Sign in to calculate a route estimate.");
      return () => controller.abort();
    }

    async function loadRoute() {
      setRouteMessage("Calculating straight-line distance…");
      try {
        const response = await fetch(`${API_BASE_URL}/api/mapping/routes`, {
          method: "POST",
          headers: {
            Authorization: `Bearer ${token}`,
            "Content-Type": "application/json",
          },
          body: JSON.stringify({
            origin: userLocation,
            destination: {
              latitude: destinationLocation.latitude,
              longitude: destinationLocation.longitude,
            },
          }),
          signal: controller.signal,
        });
        if (response.status === 401) {
          clearSessionCredentials();
          setIsRedirecting(true);
          router.replace("/login");
          return;
        }
        const body: unknown = await response.json().catch(() => undefined);
        if (!response.ok || !isApiMapRoute(body)) {
          throw new Error("Route estimate is temporarily unavailable.");
        }
        if (active) {
          setMapRoute({
            distanceKm: body.distanceKm,
            routeGeoJson: body.routeGeoJson,
            message: body.message,
          });
          setRouteMessage(
            `${body.distanceKm.toFixed(1)} km · ${body.message}`,
          );
        }
      } catch (error) {
        if (
          active &&
          !(error instanceof DOMException && error.name === "AbortError")
        ) {
          setMapRoute(null);
          setRouteMessage(
            error instanceof Error
              ? error.message
              : "Route estimate is unavailable; the map remains usable.",
          );
        }
      }
    }

    void loadRoute();
    return () => {
      active = false;
      controller.abort();
    };
  }, [geocodedLocation, router, userLocation]);

  useEffect(() => {
    const emergencyNotification = inboxNotifications.find(
      (notification) =>
        !notification.isRead &&
        notification.eventType === "EmergencyAnnouncement",
    );
    if (
      emergencyNotification &&
      emergencyNotification.id !== lastAlertId.current
    ) {
      lastAlertId.current = emergencyNotification.id;
      setActiveAlert(emergencyNotification);
    }
  }, [inboxNotifications]);

  useEffect(() => {
    if (!isNotificationsOpen) return;
    function dismissPopover(event: MouseEvent) {
      if (
        notificationPopoverRef.current &&
        !notificationPopoverRef.current.contains(event.target as Node)
      ) {
        setIsNotificationsOpen(false);
      }
    }
    function dismissOnEscape(event: KeyboardEvent) {
      if (event.key === "Escape") setIsNotificationsOpen(false);
    }
    document.addEventListener("mousedown", dismissPopover);
    document.addEventListener("keydown", dismissOnEscape);
    return () => {
      document.removeEventListener("mousedown", dismissPopover);
      document.removeEventListener("keydown", dismissOnEscape);
    };
  }, [isNotificationsOpen]);

  useEffect(() => {
    const controller = new AbortController();
    let active = true;
    const token = window.sessionStorage.getItem("authToken");

    if (
      accountRole === AUTH_SESSION_LOADING ||
      !token ||
      !accountRole ||
      (accountRole !== "Citizen" && !requestStaffRoles.has(accountRole))
    ) {
      return () => {
        active = false;
        controller.abort();
      };
    }

    async function loadShelters() {
      try {
        const response = await fetch(`${API_BASE_URL}/api/shelters`, {
          signal: controller.signal,
        });
        const body: unknown = await response.json().catch(() => undefined);
        if (!response.ok) {
          throw new Error(`Unable to load shelters (${response.status}).`);
        }
        if (!Array.isArray(body) || !body.every(isApiShelter)) {
          throw new Error("The server returned shelters in an unexpected format.");
        }
        if (active) {
          setShelters(mapApiShelters(body));
          setShelterLoadError("");
        }
      } catch (loadError) {
        if (active && !(loadError instanceof DOMException && loadError.name === "AbortError")) {
          setShelterLoadError(
            loadError instanceof Error ? loadError.message : "Unable to load shelters.",
          );
        }
      } finally {
        if (active) setIsLoadingShelters(false);
      }
    }

    async function loadResources() {
      try {
        const response = await fetch(`${API_BASE_URL}/api/resources`, {
          signal: controller.signal,
        });
        const body: unknown = await response.json().catch(() => undefined);
        if (!response.ok) {
          throw new Error(`Unable to load resources (${response.status}).`);
        }
        if (!Array.isArray(body) || !body.every(isApiResource)) {
          throw new Error("The server returned resources in an unexpected format.");
        }
        if (active) {
          setDepots(mapResources(body));
          setResourceLoadError("");
        }
      } catch (loadError) {
        if (active && !(loadError instanceof DOMException && loadError.name === "AbortError")) {
          setResourceLoadError(
            loadError instanceof Error ? loadError.message : "Unable to load resources.",
          );
        }
      } finally {
        if (active) setIsLoadingResources(false);
      }
    }

    void loadShelters();
    void loadResources();
    return () => {
      active = false;
      controller.abort();
    };
  }, [accountRole]);

  const visibleRequests = useMemo(
    () =>
      requests
        .filter(
          (request) =>
        (urgency === "all" ||
          (urgency === "unassessed"
            ? request.lvl === null
            : urgency === request.lvl)) &&
        `${request.id} ${request.cat} ${request.description} ${request.address ?? ""} ${request.lvl ?? ""} ${request.st}`
          .toLowerCase()
          .includes(normalizedQuery),
        ),
    [requests, urgency, normalizedQuery],
  );
  const matchingShelters = useMemo(
    () =>
      shelters
        .map((shelter, index) => ({ shelter, index }))
        .filter(({ shelter }) =>
          shelter.n.toLowerCase().includes(normalizedQuery),
        ),
    [shelters, normalizedQuery],
  );
  const mapRequests = useMemo<MapRequestPoint[]>(
    () =>
      accountRole !== "Citizen" && layers.req
        ? visibleRequests.flatMap((request) => {
            if (!hasValidCoordinates(request)) return [];
            return [
              {
                id: request.id,
                category: request.cat,
                address: request.address,
                latitude: request.latitude,
                longitude: request.longitude,
                urgency: request.lvl,
                status: request.st,
              },
            ];
          })
        : [],
    [accountRole, layers.req, visibleRequests],
  );
  const mapShelterPoints = useMemo<MapShelterPoint[]>(
    () =>
      layers.sh
        ? matchingShelters
            .flatMap(({ shelter }) => {
              if (!hasValidCoordinates(shelter)) return [];
              if (
                accountRole === "Citizen" &&
                userLocation &&
                distanceKm(
                  userLocation.latitude,
                  userLocation.longitude,
                  shelter.latitude,
                  shelter.longitude,
                ) > 50
              ) {
                return [];
              }
              return [
                {
                  id: shelter.id,
                  name: shelter.n,
                  latitude: shelter.latitude,
                  longitude: shelter.longitude,
                  currentOccupancy: shelter.u,
                  capacity: shelter.c,
                },
              ];
            })
        : [],
    [accountRole, layers.sh, matchingShelters, userLocation],
  );
  const mapResourcePoints = useMemo<MapResourcePoint[]>(
    () =>
      accountRole === "Citizen" && layers.res
        ? depots.flatMap((resource) => {
            if (!hasValidCoordinates(resource)) return [];
            if (
              userLocation &&
              distanceKm(
                userLocation.latitude,
                userLocation.longitude,
                resource.latitude,
                resource.longitude,
              ) > 50
            ) {
              return [];
            }
            return [
              {
                id: resource.id,
                name: resource.n,
                category: resource.item,
                quantityAvailable: resource.quantityAvailable,
                latitude: resource.latitude,
                longitude: resource.longitude,
              },
            ];
          })
        : [],
    [accountRole, depots, layers.res, userLocation],
  );
  const selectedRequest =
    selection?.type === "request"
      ? requests.find((request) => request.id === selection.id)
      : undefined;
  const selectedShelter =
    selection?.type === "shelter"
      ? shelters.find((shelter) => shelter.id === selection.id)
      : undefined;
  const selectedDepot =
    selection?.type === "resource"
      ? depots.find((resource) => resource.id === selection.id)
      : undefined;
  const urgentRequestCount = requests.filter(
    (request) => request.lvl === "Critical" || request.lvl === "High",
  ).length;
  const activeRequestCount = requests.filter(
    (request) => request.st !== "Fulfilled" && request.st !== "Rejected",
  ).length;
  const urgencyCounts = urgencyLevels.map((level) => ({
    level,
    count: requests.filter((request) => request.lvl === level).length,
  }));
  const mediumRequestCount =
    urgencyCounts.find(({ level }) => level === "Medium")?.count ?? 0;
  const lowRequestCount =
    urgencyCounts.find(({ level }) => level === "Low")?.count ?? 0;
  const unassessedRequestCount = requests.filter(
    (request) => request.lvl === null,
  ).length;
  const totalShelterCapacity = shelters.reduce((total, shelter) => total + shelter.c, 0);
  const totalShelterOccupancy = shelters.reduce((total, shelter) => total + shelter.u, 0);
  const shelterOccupancyRate =
    totalShelterCapacity > 0
      ? Math.round((totalShelterOccupancy / totalShelterCapacity) * 100)
      : 0;
  const totalResourceQuantity = depots.reduce(
    (total, resource) => total + resource.quantityAvailable,
    0,
  );
  const needsReviewCount = requests.filter(
    (request) => request.st === "Submitted" || request.st === "PendingReview",
  ).length;
  const failedNotificationCount = notificationRoles.has(accountRole)
    ? notificationLogs.filter((log) => !log.succeeded).length
    : 0;
  const unreadNotificationCount = inboxNotifications.filter(
    (notification) => !notification.isRead,
  ).length;
  const visibleInboxNotifications =
    notificationTab === "Unread"
      ? inboxNotifications.filter((notification) => !notification.isRead)
      : inboxNotifications;
  const notificationHistorySummary = !notificationRoles.has(accountRole)
    ? "Notification history is restricted to authorized staff."
    : isLoadingNotificationHistory
      ? "Loading recent notification history."
      : notificationHistoryError
        ? `Notification history is unavailable: ${notificationHistoryError}`
        : notificationLogs.length === 0
          ? "No recent notification attempts."
          : `${notificationLogs.length} recent notification attempts; ${failedNotificationCount} failed.`;
  function signOut() {
    clearSessionCredentials();
    router.replace("/login");
  }

  function notify(message: string) {
    setToast(message);
    setShowToast(true);
    if (toastTimer.current) clearTimeout(toastTimer.current);
    toastTimer.current = setTimeout(() => setShowToast(false), 3600);
  }

  function openNotificationHistory() {
    setIsNotificationsOpen(false);
    document
      .getElementById("notification-history")
      ?.scrollIntoView({ behavior: "smooth", block: "start" });
  }

  async function markNotificationRead(notificationId: string): Promise<boolean> {
    const token = window.sessionStorage.getItem("authToken");
    if (!token) {
      router.replace("/login");
      return false;
    }

    try {
      const response = await fetch(
        `${API_BASE_URL}/api/notifications/inbox/${notificationId}/read`,
        {
          method: "PATCH",
          headers: { Authorization: `Bearer ${token}` },
        },
      );
      if (response.status === 401) {
        clearSessionCredentials();
        setIsRedirecting(true);
        router.replace("/login");
        return false;
      }
      if (!response.ok && response.status !== 404) {
        throw new Error(`Unable to update notification (${response.status}).`);
      }
      setInboxNotifications((items) =>
        items.map((item) =>
          item.id === notificationId ? { ...item, isRead: true } : item,
        ),
      );
      setNotificationInboxError("");
      return true;
    } catch (error) {
      setNotificationInboxError(
        error instanceof Error
          ? error.message
          : "Unable to update the notification.",
      );
      return false;
    }
  }

  async function markAllNotificationsRead() {
    const token = window.sessionStorage.getItem("authToken");
    if (!token) {
      router.replace("/login");
      return;
    }

    try {
      const response = await fetch(
        `${API_BASE_URL}/api/notifications/inbox/read-all`,
        {
          method: "PATCH",
          headers: { Authorization: `Bearer ${token}` },
        },
      );
      if (response.status === 401) {
        clearSessionCredentials();
        setIsRedirecting(true);
        router.replace("/login");
        return;
      }
      const body: unknown = await response.json().catch(() => undefined);
      if (!response.ok) {
        throw new Error(
          getNotificationErrorMessage(
            body,
            `Unable to mark notifications as read (${response.status}).`,
          ),
        );
      }
      setInboxNotifications((items) =>
        items.map((item) => ({ ...item, isRead: true })),
      );
      setNotificationInboxError("");
    } catch (error) {
      setNotificationInboxError(
        error instanceof Error
          ? error.message
          : "Unable to mark notifications as read.",
      );
    }
  }

  async function confirmEmergencyAlert() {
    if (!activeAlert) return;
    setIsMarkingAlertRead(true);
    try {
      if (await markNotificationRead(activeAlert.id)) {
        setActiveAlert(null);
      }
    } finally {
      setIsMarkingAlertRead(false);
    }
  }

  async function sendTestNotification(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const token = window.sessionStorage.getItem("authToken");
    if (!token) {
      router.replace("/login");
      return;
    }

    const eventId = window.crypto.randomUUID();
    const recipient = notificationRecipient.trim();
    const channel = notificationChannel;
    const createdAt = new Date().toISOString();
    setIsSendingTestNotification(true);

    try {
      const response = await fetch(`${API_BASE_URL}/api/notifications/send`, {
        method: "POST",
        headers: {
          Authorization: `Bearer ${token}`,
          "Content-Type": "application/json",
          Accept: "application/json",
        },
        body: JSON.stringify({
          eventId,
          recipient,
          channel,
          subject: "Dashboard test notification",
          message: "This is a test notification sent from the dashboard.",
        }),
      });

      if (response.status === 401) {
        clearSessionCredentials();
        setNotificationLogs([]);
        setIsRedirecting(true);
        router.replace("/login");
        return;
      }

      const responseBody: unknown = await response.json().catch(() => undefined);
      const delivery = isNotificationDeliveryResult(responseBody)
        ? responseBody
        : undefined;
      const succeeded = response.ok && (delivery?.succeeded ?? false);
      const status = delivery?.status ?? (response.ok ? "Unknown" : "Failed");
      const fallbackError = response.ok
        ? "The API response did not include a delivery result."
        : `Notification request failed with HTTP ${response.status}.`;
      const errorMessage = succeeded
        ? null
        : getNotificationErrorMessage(responseBody, fallbackError);

      setNotificationLogs((logs) => [
        {
          id: eventId,
          eventId,
          recipient: maskNotificationRecipient(recipient),
          channel,
          createdAt,
          status: succeeded ? status : status === "Unknown" ? "Failed" : status,
          succeeded,
          correlationId: delivery?.correlationId,
          attemptCount: delivery?.attemptCount,
          isDuplicate: delivery?.isDuplicate,
          errorCode: delivery?.errorCode,
          errorMessage: delivery?.errorMessage ?? errorMessage,
        },
        ...logs,
      ]);
      setNotificationRefreshVersion((version) => version + 1);
      notify(
        succeeded
          ? `Notification result: ${status}.`
          : `Notification failed: ${errorMessage}`,
      );
    } catch (error) {
      const errorMessage =
        error instanceof Error
          ? error.message
          : "The notification request could not be completed.";
      setNotificationLogs((logs) => [
        {
          id: eventId,
          eventId,
          recipient: maskNotificationRecipient(recipient),
          channel,
          createdAt,
          status: "Failed",
          succeeded: false,
          errorMessage,
        },
        ...logs,
      ]);
      setNotificationRefreshVersion((version) => version + 1);
      notify(`Notification failed: ${errorMessage}`);
    } finally {
      setIsSendingTestNotification(false);
    }
  }

  async function updateRequest(
    request: AidRequest,
    endpoint: "review" | "status",
    payload: { action?: string; status?: RequestStatus; reason?: string },
  ) {
    const token = window.sessionStorage.getItem("authToken");
    if (!token) {
      router.replace("/login");
      return;
    }

    if (payload.action === "Rejected" || payload.action === "Deferred") {
      const reason = window.prompt("Enter a reason for this decision:");
      if (!reason?.trim()) return;
      payload.reason = reason.trim();
    }

    setUpdatingRequestId(request.id);
    setRequestActionError("");
    try {
      const response = await fetch(
        `${API_BASE_URL}/api/requests/${request.id}/${endpoint}`,
        {
          method: "PATCH",
          headers: {
            Authorization: `Bearer ${token}`,
            "Content-Type": "application/json",
          },
          body: JSON.stringify(payload),
        },
      );

      const responseBody: unknown = await response.json().catch(() => undefined);
      if (response.status === 401) {
        clearSessionCredentials();
        router.replace("/login");
        return;
      }

      if (!response.ok) {
        const body =
          responseBody && typeof responseBody === "object"
            ? (responseBody as { detail?: unknown; title?: unknown })
            : {};
        const message = body.detail ?? body.title;
        throw new Error(
          typeof message === "string"
            ? message
            : `Unable to update request (${response.status}).`,
        );
      }

      const updatedStatus =
        responseBody &&
        typeof responseBody === "object" &&
        typeof (responseBody as { status?: unknown }).status === "string"
          ? (responseBody as { status: string }).status
          : null;
      if (!updatedStatus || !requestStatuses.includes(updatedStatus as RequestStatus)) {
        throw new Error("The server returned an invalid request status.");
      }

      setRequests((current) =>
        current.map((item) =>
          item.id === request.id
            ? { ...item, st: updatedStatus as RequestStatus }
            : item,
        ),
      );
    } catch (error) {
      setRequestActionError(
        error instanceof Error
          ? error.message
          : "Unable to update the request. Please try again.",
      );
    } finally {
      setUpdatingRequestId(null);
    }
  }

  function findNearbyPlaces() {
    if (!navigator.geolocation) {
      setLocationMessage("Location is not available on this device.");
      return;
    }

    setLocationMessage("Finding your location…");
    navigator.geolocation.getCurrentPosition(
      (position) => {
        const location = {
          latitude: position.coords.latitude,
          longitude: position.coords.longitude,
        };
        setUserLocation(location);
        setLocationMessage("Showing mapped places within 50 km.");
        mapInstance?.flyTo(
          [location.latitude, location.longitude],
          11,
        );
      },
      () => {
        setLocationMessage(
          "Location permission was unavailable. Enable GPS and try again.",
        );
      },
      { enableHighAccuracy: true, timeout: 10000 },
    );
  }

  async function lookupMapLocation(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!isLocationQueryValid) {
      setGeocodingFeedback({
        kind: "error",
        message: "Enter a location between 4 and 300 characters.",
      });
      return;
    }

    geocodingController.current?.abort();
    const controller = new AbortController();
    geocodingController.current = controller;
    setIsGeocoding(true);
    setGeocodingFeedback({
      kind: "status",
      message: "Searching for the location…",
    });
    setGeocodingCandidates([]);
    setGeocodedLocation(null);

    try {
      const accessToken = window.sessionStorage.getItem("authToken") ?? undefined;
      if (!accessToken) {
        clearSessionCredentials();
        setIsRedirecting(true);
        router.replace("/login");
        return;
      }
      const outcome = await forwardGeocode(normalizedLocationQuery, {
        accessToken,
        signal: controller.signal,
      });
      if (geocodingController.current !== controller) return;

      if (outcome.kind === "unauthorized") {
        clearSessionCredentials();
        setIsRedirecting(true);
        router.replace("/login");
      } else if (outcome.kind === "forbidden") {
        setGeocodingFeedback({
          kind: "error",
          message:
            "You do not have permission to search for locations with this account.",
        });
      } else if (
        outcome.kind === "found" &&
        outcome.result &&
        hasValidCoordinates(outcome.result)
      ) {
        const label = outcome.result.displayAddress ?? normalizedLocationQuery;
        setGeocodedLocation({
          latitude: outcome.result.latitude,
          longitude: outcome.result.longitude,
          label,
        });
        setGeocodingFeedback({
          kind: "status",
          message: `Showing ${label} on the map.`,
        });
      } else if (outcome.kind === "ambiguous" && outcome.result) {
        const candidates = outcome.result.candidates.filter(
          hasValidCoordinates,
        );
        setGeocodingCandidates(candidates);
        setGeocodingFeedback({
          kind: candidates.length > 0 ? "status" : "error",
          message:
            candidates.length > 0
              ? "Choose a matching location to preview it on the map."
              : "The geocoder returned no valid location candidates.",
        });
      } else if (outcome.kind === "cancelled") {
        setGeocodingFeedback({
          kind: "status",
          message: outcome.message ?? "Location search cancelled.",
        });
      } else {
        const resultMessage =
          "result" in outcome ? outcome.result?.errorMessage : null;
        const details =
          outcome.kind === "validationError"
            ? Object.entries(outcome.errors).flatMap(([field, messages]) =>
                messages.map((message) => `${field}: ${message}`),
              )
            : undefined;
        setGeocodingFeedback({
          kind: "error",
          message:
            outcome.message ??
            resultMessage ??
            "The location could not be found. Try a more specific search.",
          details,
        });
      }
    } catch (error) {
      if (geocodingController.current === controller) {
        const wasCancelled =
          error instanceof DOMException && error.name === "AbortError";
        setGeocodingFeedback({
          kind: wasCancelled ? "status" : "error",
          message: wasCancelled
            ? "Location search cancelled."
            : error instanceof Error
              ? error.message
              : "Unable to search for this location.",
        });
      }
    } finally {
      if (geocodingController.current === controller) {
        geocodingController.current = null;
        setIsGeocoding(false);
      }
    }
  }

  async function handleMapPinDrop(latitude: number, longitude: number) {
    if (
      !Number.isFinite(latitude) ||
      latitude < -90 ||
      latitude > 90 ||
      !Number.isFinite(longitude) ||
      longitude < -180 ||
      longitude > 180
    ) {
      return;
    }

    geocodingController.current?.abort();
    const controller = new AbortController();
    geocodingController.current = controller;
    setIsGeocoding(true);
    setGeocodedLocation({
      latitude,
      longitude,
      label: `${latitude.toFixed(5)}, ${longitude.toFixed(5)}`,
    });
    setGeocodingFeedback({
      kind: "status",
      message: "Pin dropped. Looking up the address…",
    });

    try {
      const accessToken =
        window.sessionStorage.getItem("authToken") ?? undefined;
      if (!accessToken) {
        clearSessionCredentials();
        setIsRedirecting(true);
        router.replace("/login");
        return;
      }

      const outcome = await reverseGeocode(latitude, longitude, {
        accessToken,
        signal: controller.signal,
      });
      if (geocodingController.current !== controller) return;
      if (outcome.kind === "unauthorized") {
        clearSessionCredentials();
        setIsRedirecting(true);
        router.replace("/login");
        return;
      }
      if (
        outcome.kind === "found" &&
        outcome.result &&
        hasValidCoordinates(outcome.result)
      ) {
        const label =
          outcome.result.displayAddress ??
          `${latitude.toFixed(5)}, ${longitude.toFixed(5)}`;
        setLocationQuery(label);
        setGeocodedLocation({ latitude, longitude, label });
        setGeocodingFeedback({
          kind: "status",
          message: `Pinned ${label} on the map.`,
        });
      } else {
        setGeocodingFeedback({
          kind: "error",
          message:
            outcome.message ??
            (outcome.kind === "providerUnavailable"
              ? "Address lookup is unavailable. The dropped pin remains on the map."
              : "No address was found. The dropped pin remains on the map."),
        });
      }
    } catch (error) {
      if (geocodingController.current === controller) {
        const wasCancelled =
          error instanceof DOMException && error.name === "AbortError";
        setGeocodingFeedback({
          kind: wasCancelled ? "status" : "error",
          message: wasCancelled
            ? "Address lookup cancelled."
            : error instanceof Error
              ? `Address lookup failed. The dropped pin remains on the map. ${error.message}`
              : "Address lookup failed. The dropped pin remains on the map.",
        });
      }
    } finally {
      if (geocodingController.current === controller) {
        geocodingController.current = null;
        setIsGeocoding(false);
      }
    }
  }

  function cancelMapLocationLookup() {
    const controller = geocodingController.current;
    if (!controller) return;

    geocodingController.current = null;
    controller.abort();
    setIsGeocoding(false);
    setGeocodingFeedback({
      kind: "status",
      message: "Location search cancelled.",
    });
  }

  function chooseGeocodingCandidate(candidate: GeocodingCandidate) {
    if (!hasValidCoordinates(candidate)) {
      setGeocodingFeedback({
        kind: "error",
        message: "This candidate has invalid coordinates.",
      });
      return;
    }
    setGeocodedLocation({
      latitude: candidate.latitude,
      longitude: candidate.longitude,
      label: candidate.displayAddress,
    });
    setGeocodingCandidates([]);
    setGeocodingFeedback({
      kind: "status",
      message: `Showing ${candidate.displayAddress} on the map.`,
    });
  }

  function selectItem(type: Layer, index: number, focusMap = false) {
    const item =
      type === "req"
        ? requests[index]
        : type === "sh"
          ? shelters[index]
          : depots[index];
    if (!item) return;

    const selectionType: MapMarkerType =
      type === "req" ? "request" : type === "sh" ? "shelter" : "resource";
    setSelection({ type: selectionType, id: item.id });
    setLayers((current) => ({ ...current, [type]: true }));
    if (focusMap) {
      if (hasValidCoordinates(item)) {
        mapInstance?.flyTo(
          [item.latitude, item.longitude],
          Math.max(mapInstance.getZoom(), 12),
        );
      }
      mapCardRef.current?.scrollIntoView({ block: "nearest", behavior: "smooth" });
    }
  }

  function selectMapItem(type: MapMarkerType, id: string) {
    const layer: Layer =
      type === "request" ? "req" : type === "shelter" ? "sh" : "res";
    const index =
      type === "request"
        ? requests.findIndex((request) => request.id === id)
        : type === "shelter"
          ? shelters.findIndex((shelter) => shelter.id === id)
          : depots.findIndex((resource) => resource.id === id);
    if (index >= 0) selectItem(layer, index);
  }

  function showAllRequests() {
    setUrgency("all");
    setQuery("");
    setRequestPage(1);
  }

  function toggleLayer(layer: Layer) {
    setLayers((current) => ({ ...current, [layer]: !current[layer] }));
  }

  const totalRequestPages = Math.max(1, Math.ceil(totalRequestCount / 100));
  const mapLoading =
    (accountRole !== "Citizen" && layers.req && isLoadingRequests) ||
    (layers.sh && isLoadingShelters) ||
    (accountRole === "Citizen" && layers.res && isLoadingResources);
  const mapError = [
    accountRole !== "Citizen" && layers.req ? requestLoadError : "",
    layers.sh ? shelterLoadError : "",
    accountRole === "Citizen" && layers.res ? resourceLoadError : "",
    serviceAreaError,
  ].filter(Boolean).join(" ");
  const countText = [
    accountRole !== "Citizen" && layers.req
      ? `${mapRequests.length} mapped requests (page ${requestPage} of ${totalRequestPages})`
      : "",
    layers.sh ? `${mapShelterPoints.length} mapped shelters` : "",
    accountRole === "Citizen" && layers.res
      ? `${mapResourcePoints.length} mapped resources`
      : "",
  ]
    .filter(Boolean)
    .join(" · ");

  if (isLoadingRequests || isRedirecting) {
    return (
      <main className={styles.page}>
        <p className={styles.wrap} role="status">
          {isRedirecting ? "Redirecting to login…" : "Loading your requests…"}
        </p>
      </main>
    );
  }

  return (
    <div className={styles.page}>
      <header className={styles.header}>
        <div className={styles.nav}>
          <Link className={styles.brand} href="/" aria-label="Humanitarian Aid Platform home">
            <Image
              className={styles.logo}
              src="/humanitarian-aid-platform-logo.svg"
              alt=""
              width={54}
              height={54}
            />
            <span>Humanitarian Aid Platform</span>
          </Link>
          <nav className={styles.tabs} aria-label="Main">
            <Link className={styles.activeTab} href="/dashboard">Dashboard</Link>
            <Link href="#queue">Requests</Link>
            {requestStaffRoles.has(accountRole) && <Link href="#map-card">Staff Map</Link>}
            {accountRole === "Citizen" && <Link href="#map-card">Nearby Map</Link>}
            <Link href="#shelters-resources">Shelters &amp; Resources</Link>
            <Link href="/portal">Account</Link>
          </nav>
          <label className={styles.search}>
            <svg viewBox="0 0 24 24" aria-hidden="true">
              <circle cx="11" cy="11" r="7" />
              <path d="m20 20-4-4" />
            </svg>
            <input
              type="search"
              placeholder="Search requests, shelters, resources…"
              aria-label="Search requests, shelters, resources"
              value={query}
              onChange={(event) => setQuery(event.target.value)}
            />
          </label>
          <div
            ref={notificationPopoverRef}
            className={styles.notificationControl}
          >
            <button
              className={styles.bell}
              type="button"
              aria-label={`Notifications, ${unreadNotificationCount} unread`}
              aria-expanded={isNotificationsOpen}
              aria-controls="notification-popover"
              onClick={() => setIsNotificationsOpen((open) => !open)}
            >
              <svg viewBox="0 0 24 24" aria-hidden="true">
                <path d="M6 8a6 6 0 0 1 12 0c0 7 3 8 3 8H3s3-1 3-8M10 21h4" />
              </svg>
              {unreadNotificationCount > 0 && (
                <span className={styles.bellBadge} aria-hidden="true">
                  {unreadNotificationCount > 9 ? "9+" : unreadNotificationCount}
                </span>
              )}
            </button>
            {isNotificationsOpen && (
              <section
                className={styles.notificationPopover}
                id="notification-popover"
                aria-label="Notifications"
              >
                <div className={styles.popoverHeading}>
                  <h2>Notifications</h2>
                  <button
                    className={styles.textButton}
                    type="button"
                    onClick={() => void markAllNotificationsRead()}
                    disabled={unreadNotificationCount === 0}
                  >
                    Mark all as read
                  </button>
                </div>
                <div className={styles.notificationTabs} role="tablist">
                  {(["All", "Unread"] as const).map((tab) => (
                    <button
                      className={
                        notificationTab === tab
                          ? styles.notificationTabActive
                          : styles.notificationTab
                      }
                      id={`notification-tab-${tab.toLowerCase()}`}
                      key={tab}
                      type="button"
                      role="tab"
                      aria-selected={notificationTab === tab}
                      onClick={() => setNotificationTab(tab)}
                    >
                      {tab}
                      {tab === "Unread" && unreadNotificationCount > 0
                        ? ` (${unreadNotificationCount})`
                        : ""}
                    </button>
                  ))}
                </div>
                <div className={styles.inboxList} role="tabpanel">
                  {isLoadingInbox ? (
                    <p className={styles.note} role="status">
                      Loading notifications…
                    </p>
                  ) : notificationInboxError ? (
                    <p className={styles.notificationHistoryError} role="alert">
                      {notificationInboxError}
                    </p>
                  ) : visibleInboxNotifications.length === 0 ? (
                    <p className={styles.note}>
                      {notificationTab === "Unread"
                        ? "You are all caught up."
                        : "No notifications yet."}
                    </p>
                  ) : (
                    visibleInboxNotifications.map((notification) => (
                      <button
                        className={`${styles.inboxItem} ${
                          notification.isRead ? "" : styles.inboxItemUnread
                        }`}
                        type="button"
                        key={notification.id}
                        onClick={() => {
                          if (!notification.isRead) {
                            void markNotificationRead(notification.id);
                          }
                        }}
                      >
                        <span className={styles.inboxItemHeading}>
                          <strong>{notification.title}</strong>
                          {!notification.isRead && (
                            <span
                              className={styles.unreadDot}
                              aria-label="Unread"
                            />
                          )}
                        </span>
                        <span className={styles.inboxMessage}>
                          {notification.message}
                        </span>
                        <time dateTime={notification.createdAt}>
                          {new Date(notification.createdAt).toLocaleString()}
                        </time>
                      </button>
                    ))
                  )}
                </div>
                <div className={styles.popoverFooter}>
                  {notificationRoles.has(accountRole) && (
                    <p className={styles.note}>{notificationHistorySummary}</p>
                  )}
                  <a href="#notification-history" onClick={openNotificationHistory}>
                    View notification delivery history
                  </a>
                </div>
              </section>
            )}
          </div>
          <div className={styles.avatar} aria-label={`${accountRole} account`}>
            {accountRole.slice(0, 2).toUpperCase()}
          </div>
          <span className={styles.role}>{accountRole}</span>
          <button className={styles.button} type="button" onClick={signOut}>
            Sign out
          </button>
        </div>
      </header>

      <main className={styles.wrap} id="top">
        <section className={styles.kpis} aria-label="Key figures">
          <article className={styles.card}>
            <div className={styles.kpiHeading}><span className={styles.kpiIcon}><DocumentIcon /></span>{accountRole === "Citizen" ? "My Requests" : "Requests"}<span className={`${styles.tag} ${styles.redTag}`}>{urgentRequestCount} urgent</span></div>
            <div className={styles.big}>{activeRequestCount}</div><div className={styles.small}>Active requests on this page</div>
            <div className={styles.segment}>
              <i style={{ flex: urgentRequestCount }} />
              <i style={{ flex: mediumRequestCount }} />
              <i style={{ flex: lowRequestCount }} />
            </div>
            <div className={styles.legend}>
              {urgencyCounts.filter(({ count }) => count > 0).map(({ level, count }) => (
                <span className={level === "Critical" || level === "High" ? styles.highDot : level === "Medium" ? styles.mediumDot : styles.lowDot} key={level}>
                  {level} {count}
                </span>
              ))}
              {unassessedRequestCount > 0 && <span>{`Not assessed ${unassessedRequestCount}`}</span>}
            </div>
          </article>
          <article className={styles.card}>
            <div className={styles.kpiHeading}><span className={styles.kpiIcon}><ShelterIcon /></span>Shelters</div>
            <div className={styles.big}>{isLoadingShelters ? "…" : shelterLoadError ? "—" : shelters.length}</div>
            <div className={styles.small}>{shelterLoadError || (isLoadingShelters ? "Loading shelter data…" : `${totalShelterOccupancy} of ${totalShelterCapacity} capacity used`)}</div>
            <div className={styles.bar}><i style={{ width: `${isLoadingShelters || shelterLoadError ? 0 : shelterOccupancyRate}%` }} /></div><div className={styles.row}><span>Occupancy rate</span><b>{isLoadingShelters ? "…" : shelterLoadError ? "—" : `${shelterOccupancyRate}%`}</b></div>
          </article>
          <article className={styles.card}>
            <div className={styles.kpiHeading}><span className={styles.kpiIcon}><BoxIcon /></span>Resources</div>
            <div className={styles.big}>{isLoadingResources ? "…" : resourceLoadError ? "—" : totalResourceQuantity.toLocaleString()}</div>
            <div className={styles.small}>{resourceLoadError || (isLoadingResources ? "Loading inventory…" : `${depots.length} inventory records`)}</div>
            {resourceLoadError && <div className={styles.kpiBadges}><span className={`${styles.tag} ${styles.amberTag}`}>Inventory unavailable</span></div>}
          </article>
          <article className={styles.card}>
            <div className={styles.kpiHeading}><span className={styles.kpiIcon}><BellIcon /></span>Needs review</div>
            <div className={styles.big}>{needsReviewCount}</div><div className={styles.small}>Requests awaiting staff review</div>
            <div className={styles.kpiBadges}><span className={`${styles.tag} ${needsReviewCount > 0 ? styles.amberTag : styles.normal}`}>{needsReviewCount > 0 ? "Review required" : "All reviewed"}</span></div>
          </article>
        </section>

        {(accountRole === "Citizen" || requestStaffRoles.has(accountRole)) && (
          <section
            className={styles.card}
            id="map-card"
            aria-labelledby="map-title"
            ref={mapCardRef}
          >
            <div className={styles.mapHeading}>
              <h2 id="map-title">
                {accountRole === "Citizen"
                  ? "Nearby Shelters & Resources"
                  : "Staff Aid Map"}
              </h2>
              <div className={styles.chips} role="group" aria-label="Map layers">
                {(
                  accountRole === "Citizen"
                    ? [
                        ["sh", "Shelters"],
                        ["res", "Resources"],
                      ] as const
                    : [
                        ["req", "Requests"],
                        ["sh", "Shelters"],
                      ] as const
                ).map(([layer, label]) => (
                  <button
                    className={styles.chip}
                    key={layer}
                    type="button"
                    aria-pressed={layers[layer]}
                    onClick={() => toggleLayer(layer)}
                  >
                    <span />
                    {label}
                  </button>
                ))}
              </div>
              {accountRole === "Citizen" ? (
                <button
                  className={styles.button}
                  type="button"
                  onClick={findNearbyPlaces}
                >
                  Use my location
                </button>
              ) : (
                <select
                  className={styles.select}
                  aria-label="Filter requests by urgency"
                  value={urgency}
                  onChange={(event) => setUrgency(event.target.value)}
                >
                  <option value="all">All urgency levels</option>
                  <option>Critical</option>
                  <option>High</option>
                  <option>Medium</option>
                  <option>Low</option>
                  <option value="unassessed">Not assessed</option>
                </select>
              )}
              <span className={styles.count} aria-live="polite">
                {accountRole === "Citizen"
                  ? locationMessage ||
                    `${mapShelterPoints.length} shelters · ${mapResourcePoints.length} resources`
                  : layers.req && requestLoadError
                  ? "Requests unavailable"
                  : countText || "No map layers selected"}
              </span>
            </div>
            <form
              className={mapStyles.locationLookup}
              onSubmit={lookupMapLocation}
            >
              <label
                className={mapStyles.lookupLabel}
                htmlFor="map-location-query"
              >
                Preview a searched location
              </label>
              <div className={mapStyles.lookupRow}>
                <input
                  id="map-location-query"
                  className={mapStyles.lookupInput}
                  type="search"
                  value={locationQuery}
                  maxLength={300}
                  disabled={isGeocoding}
                  aria-invalid={
                    locationQuery.length > 0 && !isLocationQueryValid
                  }
                  aria-describedby={
                    geocodingFeedback?.details?.length
                      ? "map-location-query-hint map-location-query-errors"
                      : "map-location-query-hint"
                  }
                  placeholder="Enter an address or place"
                  onChange={(event) => {
                    setLocationQuery(event.target.value);
                    setGeocodingFeedback(null);
                    setGeocodingCandidates([]);
                    setGeocodedLocation(null);
                  }}
                />
                <button
                  className={mapStyles.lookupButton}
                  type="submit"
                  disabled={isGeocoding || !isLocationQueryValid}
                >
                  {isGeocoding ? "Searching…" : "Find location"}
                </button>
                {isGeocoding && (
                  <button
                    className={mapStyles.cancelButton}
                    type="button"
                    onClick={cancelMapLocationLookup}
                  >
                    Cancel
                  </button>
                )}
                {geocodedLocation && (
                  <button
                    className={mapStyles.clearButton}
                    type="button"
                    onClick={() => {
                      setGeocodedLocation(null);
                      setGeocodingFeedback({
                        kind: "status",
                        message: "Location preview cleared.",
                      });
                    }}
                  >
                    Clear preview
                  </button>
                )}
              </div>
              <p
                id="map-location-query-hint"
                className={mapStyles.lookupHint}
                aria-live="polite"
              >
                {isLocationQueryValid
                  ? "Search accepts 4-300 characters."
                  : "Enter a location between 4 and 300 characters."}
              </p>
              {geocodingFeedback?.kind === "error" && (
                <div className={mapStyles.lookupError} role="alert">
                  <p className={mapStyles.lookupMessage}>
                    {geocodingFeedback.message}
                  </p>
                  {geocodingFeedback.details &&
                    geocodingFeedback.details.length > 0 && (
                      <ul
                        id="map-location-query-errors"
                        className={mapStyles.lookupErrorDetails}
                      >
                        {geocodingFeedback.details.map((detail, index) => (
                          <li key={`${detail}-${index}`}>{detail}</li>
                        ))}
                      </ul>
                    )}
                </div>
              )}
              {geocodingFeedback?.kind === "status" && (
                <p
                  className={mapStyles.lookupMessage}
                  role="status"
                  aria-live="polite"
                >
                  {geocodingFeedback.message}
                </p>
              )}
              {routeMessage && (
                <p className={mapStyles.lookupMessage} role="status">
                  {routeMessage}
                </p>
              )}
              {geocodingCandidates.length > 0 && (
                <ul
                  className={mapStyles.candidateList}
                  aria-label="Matching locations"
                >
                  {geocodingCandidates.map((candidate, index) => (
                    <li key={`${candidate.latitude},${candidate.longitude},${index}`}>
                      <button
                        className={mapStyles.candidateButton}
                        type="button"
                        onClick={() => chooseGeocodingCandidate(candidate)}
                      >
                        {candidate.displayAddress}
                      </button>
                    </li>
                  ))}
                </ul>
              )}
            </form>
            <div className={styles.mapBox}>
              <LiveAidMap
                mode={accountRole === "Citizen" ? "citizen" : "staff"}
                requests={mapRequests}
                shelters={mapShelterPoints}
                resources={mapResourcePoints}
                selection={selection}
                onSelect={selectMapItem}
                onMapReady={setMapInstance}
                userLocation={userLocation}
                geocodedLocation={geocodedLocation}
                serviceAreas={serviceAreas}
                route={mapRoute}
                onPinDrop={handleMapPinDrop}
                isLoading={mapLoading}
                error={mapError || null}
                emptyMessage={
                  accountRole === "Citizen"
                    ? "No nearby mapped shelters or resources are available."
                    : "No visible requests or shelters have saved map coordinates."
                }
              />
              {selection && (
                <div className={styles.info} role="dialog" aria-label="Map item details">
                  <button
                    className={styles.closeInfo}
                    type="button"
                    aria-label="Close details"
                    onClick={() => setSelection(null)}
                  >
                    ×
                  </button>
                  {selectedRequest && (
                    <>
                      <h3>
                        Request {selectedRequest.id} · {selectedRequest.cat}
                      </h3>
                      <p>{selectedRequest.address || "Address not provided"}</p>
                      <p>{selectedRequest.description}</p>
                      <dl>
                        <div>
                          <dt>Urgency</dt>
                          <dd>
                            {selectedRequest.lvl
                              ? `${selectedRequest.lvl}${
                                  selectedRequest.sc === null
                                    ? ""
                                    : ` · ${selectedRequest.sc}%`
                                }`
                              : "Not assessed"}
                          </dd>
                        </div>
                        <div>
                          <dt>Status</dt>
                          <dd>
                            <span
                              className={`${styles.pill} ${styles[statusClass[selectedRequest.st]]}`}
                            >
                              {displayStatus(selectedRequest.st)}
                            </span>
                          </dd>
                        </div>
                        <div>
                          <dt>Submitted</dt>
                          <dd>
                            {new Date(selectedRequest.createdAt).toLocaleDateString()}
                          </dd>
                        </div>
                      </dl>
                    </>
                  )}
                  {selectedShelter && (
                    <>
                      <h3>{selectedShelter.n}</h3>
                      <p>Shelter</p>
                      <dl>
                        <div>
                          <dt>Occupancy</dt>
                          <dd>
                            <span
                              className={`${styles.pill} ${
                                selectedShelter.p >= 80
                                  ? styles.high
                                  : styles.low
                              }`}
                            >
                              {selectedShelter.p}% full
                            </span>
                          </dd>
                        </div>
                        <div>
                          <dt>Beds used</dt>
                          <dd>
                            {selectedShelter.u} of {selectedShelter.c}
                          </dd>
                        </div>
                      </dl>
                      <div
                        className={`${styles.bar} ${
                          selectedShelter.p >= 80 ? styles.redBar : ""
                        }`}
                      >
                        <i style={{ width: `${selectedShelter.p}%` }} />
                      </div>
                    </>
                  )}
                  {selectedDepot && (
                    <>
                      <h3>{selectedDepot.n}</h3>
                      <p>{selectedDepot.item} resource</p>
                      <dl>
                        <div>
                          <dt>Available</dt>
                          <dd>{selectedDepot.quantityAvailable}</dd>
                        </div>
                        {selectedDepot.description && (
                          <div>
                            <dt>Description</dt>
                            <dd>{selectedDepot.description}</dd>
                          </div>
                        )}
                      </dl>
                    </>
                  )}
                </div>
              )}
            </div>
            <div className={styles.mapKey}>
              {accountRole === "Citizen" ? (
                <>
                  <span>
                    <i className={styles.keyShelter} />
                    Shelter
                  </span>
                  <span>
                    <i className={styles.keyResource} />
                    Resource
                  </span>
                </>
              ) : (
                <>
                  <span><i className={styles.keyCritical} />Critical</span>
                  <span><i className={styles.keyHigh} />High</span>
                  <span><i className={styles.keyMedium} />Medium</span>
                  <span><i className={styles.keyLow} />Low</span>
                  <span><i className={styles.keyUnassessed} />Not assessed</span>
                  <span><i className={styles.keyShelter} />Shelter</span>
                </>
              )}
            </div>
            <p className={styles.note}>
              {accountRole === "Citizen"
                ? "Tap “Use my location” to show mapped shelters and resources within 50 km. Your location is used only by this browser and is not saved."
                : "Exact request locations are visible only to authorized staff. Shelters and requests without saved coordinates are not plotted."}
            </p>
          </section>
        )}

        <div className={styles.columns}>
          <section className={styles.card} id="queue" aria-labelledby="queue-title">
            <div className={styles.cardHeading}>
              <h2 id="queue-title">{accountRole === "Citizen" ? "My Assistance Requests" : "Assistance Requests"}</h2>
              {accountRole === "Citizen" && <Link className={styles.button} href="/request-aid">New request</Link>}
              <button className={styles.button} type="button" onClick={showAllRequests}>View all</button>
            </div>
            <div className={styles.tableScroll}>
              <table>
                <colgroup>
                  <col className={styles.colId} />
                  <col className={styles.colCategory} />
                  <col className={styles.colName} />
                  <col className={styles.colUrgency} />
                  <col className={styles.colStatus} />
                  <col className={styles.colActions} />
                </colgroup>
                <thead>
                  <tr>
                    <th>Request ID</th>
                    <th>Category</th>
                    <th>Description</th>
                    <th>Urgency</th>
                    <th>Status</th>
                    <th>{requestStaffRoles.has(accountRole) ? "Actions" : "Submitted"}</th>
                  </tr>
                </thead>
                <tbody>
                  {isLoadingRequests && (
                    <tr><td colSpan={6} role="status">{accountRole === "Citizen" ? "Loading your requests…" : "Loading requests…"}</td></tr>
                  )}
                  {!isLoadingRequests && requestLoadError && (
                    <tr><td colSpan={6} role="alert">{requestLoadError}</td></tr>
                  )}
                  {!isLoadingRequests && !requestLoadError && visibleRequests.length === 0 && (
                    <tr><td colSpan={6}>No requests found.</td></tr>
                  )}
                  {!isLoadingRequests && !requestLoadError &&
                    visibleRequests
                      .slice()
                      .sort(
                        (a, b) =>
                          Date.parse(b.createdAt) -
                          Date.parse(a.createdAt),
                      )
                      .map((request) => {
                        const markerColor = request.lvl
                          ? urgencyColors[request.lvl]
                          : "#718096";
                        return (
                          <tr
                            className={
                              selection?.type === "request" &&
                              selection.id === request.id
                                ? styles.highlightedRow
                                : ""
                            }
                            key={request.id}
                            style={{ "--level-color": markerColor } as CSSProperties}
                          >
                            <td>
                              <button
                                className={styles.requestId}
                                type="button"
                                aria-label={`Show request ${request.id} on map`}
                                onClick={() =>
                                  selectItem(
                                    "req",
                                    requests.findIndex((item) => item.id === request.id),
                                    true,
                                  )
                                }
                              >
                                #{request.id}
                                <svg viewBox="0 0 24 24" aria-hidden="true">
                                  <path d="M12 21s-7-6-7-11a7 7 0 0 1 14 0c0 5-7 11-7 11z" />
                                  <circle cx="12" cy="10" r="2.5" />
                                </svg>
                              </button>
                            </td>
                            <td className={styles.category}>{request.cat}</td>
                            <td className={styles.citizen}>{request.description}</td>
                            <td>
                              <div className={styles.urgencyCell}>
                                <span className={`${styles.pill} ${styles[urgencyClass(request.lvl)]}`}>
                                  {request.lvl ?? "Not assessed"}
                                  {request.sc === null ? "" : ` · ${request.sc}%`}
                                </span>
                                {request.sc !== null && (
                                  <span className={styles.meter}>
                                    <i
                                      style={{
                                        width: `${request.sc}%`,
                                        background: markerColor,
                                      }}
                                    />
                                  </span>
                                )}
                              </div>
                            </td>
                            <td>
                              <span className={`${styles.pill} ${styles[statusClass[request.st]]}`}>
                                {displayStatus(request.st)}
                              </span>
                            </td>
                            <td>
                              {requestStaffRoles.has(accountRole) ? (
                                <div className={styles.rowActions}>
                                  {request.st === "Submitted" && (
                                    <button
                                      className={`${styles.button} ${styles.primaryButton}`}
                                      type="button"
                                      disabled={updatingRequestId === request.id}
                                      onClick={() =>
                                        void updateRequest(request, "status", {
                                          status: "PendingReview",
                                        })
                                      }
                                    >
                                      Send to review
                                    </button>
                                  )}
                                  {request.st === "PendingReview" && (
                                    <>
                                      <button
                                        className={`${styles.button} ${styles.primaryButton}`}
                                        type="button"
                                        disabled={updatingRequestId === request.id}
                                        onClick={() =>
                                          void updateRequest(request, "review", {
                                            action: "Approved",
                                          })
                                        }
                                      >
                                        Approve
                                      </button>
                                      <button
                                        className={styles.button}
                                        type="button"
                                        disabled={updatingRequestId === request.id}
                                        onClick={() =>
                                          void updateRequest(request, "review", {
                                            action: "Deferred",
                                          })
                                        }
                                      >
                                        Defer
                                      </button>
                                      <button
                                        className={styles.button}
                                        type="button"
                                        disabled={updatingRequestId === request.id}
                                        onClick={() =>
                                          void updateRequest(request, "review", {
                                            action: "Rejected",
                                          })
                                        }
                                      >
                                        Reject
                                      </button>
                                    </>
                                  )}
                                  {request.st === "Approved" && (
                                    <button
                                      className={`${styles.button} ${styles.primaryButton}`}
                                      type="button"
                                      disabled={updatingRequestId === request.id}
                                      onClick={() =>
                                        void updateRequest(request, "status", {
                                          status: "Assigned",
                                        })
                                      }
                                    >
                                      Assign
                                    </button>
                                  )}
                                  {request.st === "Assigned" && (
                                    <button
                                      className={`${styles.button} ${styles.primaryButton}`}
                                      type="button"
                                      disabled={updatingRequestId === request.id}
                                      onClick={() =>
                                        void updateRequest(request, "status", {
                                          status: "Fulfilled",
                                        })
                                      }
                                    >
                                      Mark fulfilled
                                    </button>
                                  )}
                                  {updatingRequestId === request.id && (
                                    <span className={styles.note} role="status">
                                      Updating…
                                    </span>
                                  )}
                                </div>
                              ) : (
                                new Date(request.createdAt).toLocaleDateString()
                              )}
                            </td>
                          </tr>
                        );
                      })}
                </tbody>
              </table>
            </div>
            {requestActionError && (
              <p className={styles.note} role="alert">
                {requestActionError}
              </p>
            )}
            <div className={styles.row}>
              <span>
                {totalRequestCount === 0
                  ? "No requests"
                  : `Showing ${(requestPage - 1) * 100 + 1}–${Math.min(
                      requestPage * 100,
                      totalRequestCount,
                    )} of ${totalRequestCount}`}
              </span>
              <div className={styles.row}>
                <button
                  className={styles.button}
                  type="button"
                  disabled={requestPage <= 1 || isLoadingRequests}
                  onClick={() => setRequestPage((page) => page - 1)}
                >
                  Previous
                </button>
                <span>Page {requestPage} of {totalRequestPages}</span>
                <button
                  className={styles.button}
                  type="button"
                  disabled={requestPage >= totalRequestPages || isLoadingRequests}
                  onClick={() => setRequestPage((page) => page + 1)}
                >
                  Next
                </button>
              </div>
            </div>
          </section>
          <div className={styles.sideColumn}>
            <section
              className={styles.card}
              id="shelters-resources"
              aria-labelledby="shelters-title"
            >
              <div className={styles.cardHeading}>
                <h2 id="shelters-title">Shelters &amp; Resources</h2>
                <button
                  className={styles.button}
                  type="button"
                  onClick={() =>
                    notify("Shelter management is not available yet.")
                  }
                >
                  Manage
                </button>
              </div>
              {isLoadingShelters ? (
                <p role="status">Loading shelters…</p>
              ) : shelterLoadError ? (
                <p role="alert">{shelterLoadError}</p>
              ) : shelters.length === 0 ? (
                <p>No shelters are available.</p>
              ) : (
                <div>
                  {shelters.slice(0, 3).map((shelter, index) => {
                    const details = (
                      <>
                        <span className={styles.shelterTop}>
                          <b>{shelter.n}</b>
                          <strong className={shelter.p >= 80 ? styles.redText : ""}>
                            {shelter.p}% full
                          </strong>
                        </span>
                        <span
                          className={`${styles.bar} ${
                            shelter.p >= 80 ? styles.redBar : ""
                          }`}
                        >
                          <i style={{ width: `${shelter.p}%` }} />
                        </span>
                      </>
                    );

                    return requestStaffRoles.has(accountRole) ? (
                      <button
                        className={styles.shelterBar}
                        key={shelter.id}
                        type="button"
                        aria-label={`Show ${shelter.n} on map`}
                        onClick={() => selectItem("sh", index, true)}
                      >
                        {details}
                      </button>
                    ) : (
                      <div className={styles.shelterBar} key={shelter.id}>
                        {details}
                      </div>
                    );
                  })}
                </div>
              )}
              <div className={styles.resourceList}>
                {isLoadingResources ? (
                  <p role="status">Loading inventory…</p>
                ) : resourceLoadError ? (
                  <p role="alert">{resourceLoadError}</p>
                ) : depots.length === 0 ? (
                  <p>No resource inventory is available.</p>
                ) : (
                  depots.slice(0, 5).map((resource) => (
                    <div key={resource.id}>
                      <span className={`${styles.pill} ${styles.normal}`}>{resource.quantityAvailable} available</span>
                      {resource.item}: {resource.n}
                    </div>
                  ))
                )}
              </div>
            </section>
            <section
              className={styles.card}
              id="notification-history"
              aria-labelledby="notifications-title"
            >
              <div className={styles.cardHeading}>
                <h2 id="notifications-title">Notification delivery history</h2>
                <span className={`${styles.tag} ${styles.redTag}`}>
                  {failedNotificationCount} failed
                </span>
              </div>
              {notificationRoles.has(accountRole) ? (
                <>
                  <div className={styles.notificationTestTools}>
                    <h3>Staff test-send</h3>
                    <p className={styles.note}>
                      Test attempts only. Sandbox mode reports MockSent and
                      does not contact an email or SMS recipient.
                    </p>
                    <form
                      className={styles.notificationForm}
                      onSubmit={sendTestNotification}
                    >
                      <label>
                        Test recipient
                        <input
                          type={notificationChannel === "Email" ? "email" : "tel"}
                          required
                          value={notificationRecipient}
                          onChange={(event) =>
                            setNotificationRecipient(event.target.value)
                          }
                          placeholder={
                            notificationChannel === "Email"
                              ? "name@example.com"
                              : "+1 555 555 0100"
                          }
                          autoComplete="off"
                        />
                      </label>
                      <label>
                        Channel
                        <select
                          value={notificationChannel}
                          onChange={(event) =>
                            setNotificationChannel(
                              event.target.value === "Sms" ? "Sms" : "Email",
                            )
                          }
                        >
                          <option value="Email">Email</option>
                          <option value="Sms">SMS</option>
                        </select>
                      </label>
                      <button
                        className={`${styles.button} ${styles.primaryButton}`}
                        type="submit"
                        disabled={isSendingTestNotification}
                      >
                        {isSendingTestNotification
                          ? "Sending…"
                          : "Send Test Notification"}
                      </button>
                    </form>
                  </div>
                  <h3 className={styles.notificationSubheading}>
                    Recent attempts
                  </h3>
                </>
              ) : (
                <p className={styles.note}>
                  Notification tests are available to authorized staff.
                </p>
              )}
              <div className={styles.logs}>
                {!notificationRoles.has(accountRole) ? (
                  <p className={styles.note}>
                    Notification history is available to authorized staff.
                  </p>
                ) : isLoadingNotificationHistory ? (
                  <p role="status">Loading notification history…</p>
                ) : (
                  <>
                    {notificationHistoryError ? (
                      <div className={styles.notificationHistoryError} role="alert">
                        <p>{notificationHistoryError}</p>
                        <button
                          className={styles.button}
                          type="button"
                          onClick={() =>
                            setNotificationRefreshVersion((version) => version + 1)
                          }
                        >
                          Retry
                        </button>
                      </div>
                    ) : notificationLogs.length === 0 ? (
                      <p className={styles.note}>
                        No notification attempts are recorded in recent history.
                      </p>
                    ) : null}
                    {notificationLogs.map((log) => (
                      <LogEntry key={log.id} entry={log} />
                    ))}
                  </>
                )}
              </div>
            </section>
          </div>
        </div>
      </main>
      <div
        className={`${styles.toast} ${showToast ? styles.toastVisible : ""}`}
        role="status"
        aria-live="polite"
      >
        {toast}
      </div>
    </div>
  );
}

function urgencyClass(level: Urgency | null) {
  if (level === null) return "unassessed";
  if (level === "Critical") return "critical";
  return level === "High" ? "high" : level === "Medium" ? "medium" : "low";
}

function displayStatus(status: RequestStatus) {
  return status.replace(/([a-z])([A-Z])/g, "$1 $2");
}

function DocumentIcon() {
  return <svg viewBox="0 0 24 24" aria-hidden="true"><path d="M14 3H7a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h10a2 2 0 0 0 2-2V8zM14 3v5h5M9 13h6M9 17h4" /></svg>;
}

function ShelterIcon() {
  return <svg viewBox="0 0 24 24" aria-hidden="true"><path d="M3 11l9-7 9 7v9a1 1 0 0 1-1 1H4a1 1 0 0 1-1-1zM9 21v-7h6v7" /></svg>;
}

function BoxIcon() {
  return <svg viewBox="0 0 24 24" aria-hidden="true"><path d="M21 8l-9-5-9 5v8l9 5 9-5zM3 8l9 5 9-5M12 13v8" /></svg>;
}

function BellIcon() {
  return <svg viewBox="0 0 24 24" aria-hidden="true"><path d="M6 8a6 6 0 0 1 12 0c0 7 3 8 3 8H3s3-1 3-8M10 21h4" /></svg>;
}

function LogEntry({ entry }: { entry: NotificationLogEntry }) {
  const className = entry.succeeded
    ? entry.status === "MockSent"
      ? styles.logPending
      : styles.logSuccess
    : styles.logFailed;
  const timestamp = new Date(entry.createdAt).toLocaleString([], {
    dateStyle: "short",
    timeStyle: "short",
  });
  const details = [
    entry.eventId ? `Event ${entry.eventId}` : null,
    entry.correlationId ? `Correlation ${entry.correlationId}` : null,
    entry.attemptCount !== undefined
      ? `Attempts ${entry.attemptCount}`
      : null,
    entry.isDuplicate ? "Duplicate suppressed" : null,
    entry.errorCode ?? null,
    entry.errorMessage ?? null,
  ].filter((detail): detail is string => Boolean(detail));

  return (
    <div className={styles.logEntry}>
      <span className={styles.logIcon}>
        {entry.channel === "Sms" ? <MessageIcon /> : <MailIcon />}
      </span>
      <div>
        <b>Notification attempt</b>
        <small>
          {entry.channel === "Sms"
            ? "SMS"
            : entry.channel === "InApp"
              ? "In-app"
              : entry.channel === "Unknown"
                ? "Unknown channel"
                : "Email"}{" "}
          · {entry.recipient}
        </small>
        {details.map((detail) => (
          <small className={styles.notificationDetail} key={detail}>
            {detail}
          </small>
        ))}
      </div>
      <div className={styles.logResult}>
        <span className={`${styles.pill} ${className}`}>{entry.status}</span>
        <small>{timestamp}</small>
      </div>
    </div>
  );
}

function MessageIcon() {
  return <svg viewBox="0 0 24 24" aria-hidden="true"><path d="M4 5h16v11H9l-5 4z" /></svg>;
}

function MailIcon() {
  return <svg viewBox="0 0 24 24" aria-hidden="true"><rect x="3" y="5" width="18" height="14" rx="2" /><path d="M3 7l9 6 9-6" /></svg>;
}
