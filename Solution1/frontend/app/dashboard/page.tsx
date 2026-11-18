"use client";

import Image from "next/image";
import Link from "next/link";
import {
  CSSProperties,
  MouseEvent as ReactMouseEvent,
  PointerEvent,
  useEffect,
  useRef,
  useState,
} from "react";
import { useRouter } from "next/navigation";
import styles from "./page.module.css";

type Urgency = "Critical" | "High" | "Medium" | "Low";
type RequestStatus =
  | "Submitted"
  | "PendingReview"
  | "Approved"
  | "Deferred"
  | "Assigned"
  | "Fulfilled"
  | "Rejected";
type Layer = "req" | "sh";
type Selection = { type: Layer; index: number } | null;
type ViewBox = { x: number; y: number; w: number; h: number };

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
  x?: number;
  y?: number;
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
  name: string;
  capacity: number;
  currentOccupancy: number;
  latitude: number | null;
  longitude: number | null;
  x?: number;
  y?: number;
};

type Resource = {
  id: string;
  name: string;
  category: string;
  quantityAvailable: number;
  description: string | null;
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

const initialViewBox: ViewBox = { x: 0, y: 0, w: 900, h: 520 };

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

function isApiShelter(value: unknown): value is Shelter {
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

function isApiResource(value: unknown): value is Resource {
  if (!value || typeof value !== "object") return false;
  const resource = value as Record<string, unknown>;
  return (
    typeof resource.id === "string" &&
    typeof resource.name === "string" &&
    typeof resource.category === "string" &&
    typeof resource.quantityAvailable === "number" &&
    (resource.description === null || typeof resource.description === "string")
  );
}

function positionMapItems(
  requests: AidRequest[],
  shelters: Shelter[],
): { requests: AidRequest[]; shelters: Shelter[] } {
  const located = [...requests, ...shelters].filter(
    (
      item,
    ): item is (AidRequest | Shelter) & {
      latitude: number;
      longitude: number;
    } => item.latitude !== null && item.longitude !== null,
  );
  if (located.length === 0) return { requests, shelters };

  const latitudes = located.map((request) => request.latitude);
  const longitudes = located.map((request) => request.longitude);
  const minLatitude = Math.min(...latitudes);
  const maxLatitude = Math.max(...latitudes);
  const minLongitude = Math.min(...longitudes);
  const maxLongitude = Math.max(...longitudes);

  function position<T extends AidRequest | Shelter>(item: T): T {
    if (item.latitude === null || item.longitude === null) return item;
    return {
      ...item,
      x:
        maxLongitude === minLongitude
          ? 450
          : 100 +
            ((item.longitude - minLongitude) /
              (maxLongitude - minLongitude)) *
              700,
      y:
        maxLatitude === minLatitude
          ? 260
          : 80 +
            ((maxLatitude - item.latitude) /
              (maxLatitude - minLatitude)) *
              360,
    };
  }

  return {
    requests: requests.map(position),
    shelters: shelters.map(position),
  };
}

function clampViewBox(viewBox: ViewBox): ViewBox {
  const w = Math.max(300, Math.min(900, viewBox.w));
  const h = (w * 520) / 900;
  return {
    x: Math.max(-40, Math.min(940 - w, viewBox.x)),
    y: Math.max(-30, Math.min(550 - h, viewBox.y)),
    w,
    h,
  };
}

export default function DashboardPage() {
  const router = useRouter();
  const [requests, setRequests] = useState<AidRequest[]>([]);
  const [shelters, setShelters] = useState<Shelter[]>([]);
  const [resources, setResources] = useState<Resource[]>([]);
  const [isLoadingRequests, setIsLoadingRequests] = useState(true);
  const [isRedirecting, setIsRedirecting] = useState(false);
  const [requestLoadError, setRequestLoadError] = useState("");
  const [shelterLoadError, setShelterLoadError] = useState("");
  const [resourceLoadError, setResourceLoadError] = useState("");
  const [layers, setLayers] = useState<Record<Layer, boolean>>({
    req: true,
    sh: true,
  });
  const [urgency, setUrgency] = useState("all");
  const [query, setQuery] = useState("");
  const [selection, setSelection] = useState<Selection>(null);
  const [viewBox, setViewBox] = useState(initialViewBox);
  const [toast, setToast] = useState("");
  const [showToast, setShowToast] = useState(false);
  const [isDragging, setIsDragging] = useState(false);
  const mapRef = useRef<SVGSVGElement>(null);
  const mapCardRef = useRef<HTMLElement>(null);
  const dragRef = useRef<{ x: number; y: number; viewX: number; viewY: number } | null>(null);
  const toastTimer = useRef<ReturnType<typeof setTimeout> | null>(null);
  const normalizedQuery = query.trim().toLowerCase();

  useEffect(() => {
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

    async function loadRequests() {
      try {
        const allItems: ApiAidRequest[] = [];
        let totalPages = 1;
        for (let page = 1; page <= totalPages; page += 1) {
          const response = await fetch(
            `${API_BASE_URL}/api/requests/mine?page=${page}&pageSize=100`,
            {
              method: "GET",
              headers: { Authorization: `Bearer ${token}` },
              signal: controller.signal,
            },
          );

          if (response.status === 401) {
            window.sessionStorage.removeItem("authToken");
            setIsRedirecting(true);
            router.replace("/login");
            return;
          }

          const responseBody: unknown = await response.json();
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
          const result = responseBody as {
            items?: unknown;
            totalPages?: unknown;
          };
          if (!Array.isArray(result.items) || !result.items.every(isApiAidRequest)) {
            throw new Error("The server returned requests in an unexpected format.");
          }
          if (
            typeof result.totalPages !== "number" ||
            !Number.isInteger(result.totalPages) ||
            result.totalPages < 0
          ) {
            throw new Error("The server returned invalid request pagination data.");
          }
          totalPages = result.totalPages;
          allItems.push(...result.items);
        }

        if (active) {
          setRequests(allItems.map(mapApiRequest));
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

    async function loadShelters() {
      try {
        const response = await fetch(`${API_BASE_URL}/api/shelters`, {
          signal: controller.signal,
        });
        if (!response.ok) {
          throw new Error(
            `Unable to load shelters (${response.status} ${response.statusText}).`,
          );
        }
        const responseBody: unknown = await response.json();
        if (!Array.isArray(responseBody) || !responseBody.every(isApiShelter)) {
          throw new Error("The server returned shelters in an unexpected format.");
        }
        if (active) {
          setShelters(responseBody);
          setShelterLoadError("");
        }
      } catch (error) {
        if (
          active &&
          !(error instanceof DOMException && error.name === "AbortError")
        ) {
          setShelterLoadError(
            error instanceof Error ? error.message : "Unable to load shelters.",
          );
        }
      }
    }

    async function loadResources() {
      try {
        const response = await fetch(`${API_BASE_URL}/api/resources`, {
          signal: controller.signal,
        });
        if (!response.ok) {
          throw new Error(
            `Unable to load resources (${response.status} ${response.statusText}).`,
          );
        }
        const responseBody: unknown = await response.json();
        if (!Array.isArray(responseBody) || !responseBody.every(isApiResource)) {
          throw new Error("The server returned resources in an unexpected format.");
        }
        if (active) {
          setResources(responseBody);
          setResourceLoadError("");
        }
      } catch (error) {
        if (
          active &&
          !(error instanceof DOMException && error.name === "AbortError")
        ) {
          setResourceLoadError(
            error instanceof Error ? error.message : "Unable to load resources.",
          );
        }
      }
    }

    void loadRequests();
    void loadShelters();
    void loadResources();
    return () => {
      active = false;
      controller.abort();
    };
  }, [router]);

  function matches(value: string) {
    return value.toLowerCase().includes(normalizedQuery);
  }

  const mapItems = positionMapItems(requests, shelters);
  const visibleRequests = mapItems.requests
    .map((request, index) => ({ request, index }))
    .filter(
      ({ request }) =>
        (urgency === "all" ||
          (urgency === "unassessed"
            ? request.lvl === null
            : urgency === request.lvl)) &&
        matches(
          `${request.id} ${request.cat} ${request.description} ${request.address ?? ""} ${request.lvl ?? ""} ${request.st}`,
        ),
    );

  const matchingShelters = mapItems.shelters
    .map((shelter, index) => ({ shelter, index }))
    .filter(({ shelter }) => matches(shelter.name));
  const matchingResources = resources.filter((resource) =>
    matches(`${resource.name} ${resource.category} ${resource.description ?? ""}`),
  );
  const selectedRequest =
    selection?.type === "req" ? requests[selection.index] : undefined;
  const selectedShelter =
    selection?.type === "sh" ? shelters[selection.index] : undefined;
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
  const shelterCapacity = shelters.reduce(
    (total, shelter) => total + shelter.capacity,
    0,
  );
  const shelterOccupancy = shelters.reduce(
    (total, shelter) => total + shelter.currentOccupancy,
    0,
  );
  const shelterOccupancyPercent =
    shelterCapacity === 0
      ? 0
      : Math.round((shelterOccupancy / shelterCapacity) * 100);
  const availableResourceQuantity = resources.reduce(
    (total, resource) => total + resource.quantityAvailable,
    0,
  );

  function notify(message: string) {
    setToast(message);
    setShowToast(true);
    if (toastTimer.current) clearTimeout(toastTimer.current);
    toastTimer.current = setTimeout(() => setShowToast(false), 3600);
  }

  function zoom(factor: number, centerX?: number, centerY?: number) {
    setViewBox((current) => {
      const cx = centerX ?? current.x + current.w / 2;
      const cy = centerY ?? current.y + current.h / 2;
      const w = current.w * factor;
      return clampViewBox({
        x: cx - (cx - current.x) * (w / current.w),
        y: cy - (cy - current.y) * (w / current.w),
        w,
        h: (w * 520) / 900,
      });
    });
  }

  function handleMapPointerDown(event: PointerEvent<SVGSVGElement>) {
    if ((event.target as Element).closest(".mapMarker")) return;
    dragRef.current = {
      x: event.clientX,
      y: event.clientY,
      viewX: viewBox.x,
      viewY: viewBox.y,
    };
    setIsDragging(true);
    event.currentTarget.setPointerCapture(event.pointerId);
  }

  function handleMapPointerMove(event: PointerEvent<SVGSVGElement>) {
    if (!dragRef.current || !mapRef.current) return;
    const rect = mapRef.current.getBoundingClientRect();
    const scale = viewBox.w / rect.width;
    const drag = dragRef.current;
    setViewBox(
      clampViewBox({
        ...viewBox,
        x: drag.viewX - (event.clientX - drag.x) * scale,
        y: drag.viewY - (event.clientY - drag.y) * scale,
      }),
    );
  }

  function handleMapDoubleClick(event: ReactMouseEvent<SVGSVGElement>) {
    const rect = event.currentTarget.getBoundingClientRect();
    zoom(
      0.65,
      viewBox.x + ((event.clientX - rect.left) / rect.width) * viewBox.w,
      viewBox.y + ((event.clientY - rect.top) / rect.height) * viewBox.h,
    );
  }

  function selectItem(type: Layer, index: number, focusMap = false) {
    setSelection({ type, index });
    setLayers((current) => ({ ...current, [type]: true }));
    if (focusMap) {
      const item =
        type === "req"
          ? mapItems.requests[index]
          : mapItems.shelters[index];
      if (item.x === undefined || item.y === undefined) return;
      const w = Math.min(viewBox.w, 560);
      setViewBox(
        clampViewBox({
          x: item.x - w / 2,
          y: item.y - (w * 520) / 900 / 2 - 10,
          w,
          h: (w * 520) / 900,
        }),
      );
      mapCardRef.current?.scrollIntoView({ block: "nearest", behavior: "smooth" });
    }
  }

  function showAllRequests() {
    setUrgency("all");
    setQuery("");
  }

  function toggleLayer(layer: Layer) {
    setLayers((current) => ({ ...current, [layer]: !current[layer] }));
  }

  const requestCount = layers.req ? visibleRequests.length : 0;
  const shelterCount = layers.sh ? matchingShelters.length : 0;
  const countText = [
    layers.req ? `${requestCount} requests` : "",
    layers.sh ? `${shelterCount} shelters` : "",
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
            <Link href="#map-card">Shelters &amp; Resources</Link>
            <Link href="#queue">Donors &amp; Volunteers</Link>
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
          <button
            className={styles.bell}
            type="button"
            aria-label="Notifications"
            onClick={() => notify("You’re all caught up on notifications.")}
          >
            <svg viewBox="0 0 24 24" aria-hidden="true">
              <path d="M6 8a6 6 0 0 1 12 0c0 7 3 8 3 8H3s3-1 3-8M10 21h4" />
            </svg>
            <i />
          </button>
          <div className={styles.avatar} aria-label="Coordinator initials">CO</div>
          <span className={styles.role}>Coordinator</span>
        </div>
      </header>

      <main className={styles.wrap} id="top">
        <section className={styles.kpis} aria-label="Key figures">
          <article className={styles.card}>
            <div className={styles.kpiHeading}><span className={styles.kpiIcon}><DocumentIcon /></span>My Requests<span className={`${styles.tag} ${styles.redTag}`}>{urgentRequestCount} urgent</span></div>
            <div className={styles.big}>{activeRequestCount}</div><div className={styles.small}>Active requests</div>
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
            <div className={styles.big}>{shelters.length}</div><div className={styles.small}>{shelterOccupancy.toLocaleString()} of {shelterCapacity.toLocaleString()} capacity used</div>
            <div className={styles.bar}><i style={{ width: `${shelterOccupancyPercent}%` }} /></div><div className={styles.row}><span>Occupancy rate</span><b>{shelterOccupancyPercent}%</b></div>
          </article>
          <article className={styles.card}>
            <div className={styles.kpiHeading}><span className={styles.kpiIcon}><BoxIcon /></span>Resources</div>
            <div className={styles.big}>{availableResourceQuantity.toLocaleString()}</div><div className={styles.small}>Available units across {resources.length} resource records</div>
            {resourceLoadError && <div className={styles.small} role="alert">{resourceLoadError}</div>}
          </article>
          <article className={styles.card}>
            <div className={styles.kpiHeading}><span className={styles.kpiIcon}><BellIcon /></span>System Alerts</div>
            <div className={styles.big}>312</div><div className={styles.small}>Notifications sent</div>
            <div className={styles.delivery}><div><div className={styles.deliveryLabel}>Delivery success</div><b>98.4%</b></div><div className={styles.miniBars} aria-hidden="true">{[40, 55, 48, 70, 58, 85, 68].map((height, index) => <i key={index} style={{ height: `${height}%` }} />)}</div></div>
          </article>
        </section>

        <section className={styles.card} id="map-card" aria-labelledby="map-title" ref={mapCardRef}>
          <div className={styles.mapHeading}>
            <h2 id="map-title">Live Aid Map</h2>
            <div className={styles.chips} role="group" aria-label="Map layers">
              {([["req", "Requests"], ["sh", "Shelters"]] as const).map(([layer, label]) => (
                <button className={styles.chip} key={layer} type="button" aria-pressed={layers[layer]} onClick={() => toggleLayer(layer)}><span />{label}</button>
              ))}
            </div>
            <select className={styles.select} aria-label="Filter requests by urgency" value={urgency} onChange={(event) => setUrgency(event.target.value)}>
              <option value="all">All urgency levels</option><option>Critical</option><option>High</option><option>Medium</option><option>Low</option><option value="unassessed">Not assessed</option>
            </select>
            <span className={styles.count} aria-live="polite">
              {isLoadingRequests
                ? "Loading your requests…"
                : requestLoadError
                  ? "Requests unavailable"
                  : countText}
            </span>
          </div>
          <div className={styles.mapBox}>
            <svg
              ref={mapRef}
              className={`${styles.map} ${isDragging ? styles.dragging : ""}`}
              viewBox={`${viewBox.x} ${viewBox.y} ${viewBox.w} ${viewBox.h}`}
              role="group"
              aria-label="Schematic map of requests and shelters"
              onPointerDown={handleMapPointerDown}
              onPointerMove={handleMapPointerMove}
              onPointerUp={() => { dragRef.current = null; setIsDragging(false); }}
              onPointerCancel={() => { dragRef.current = null; setIsDragging(false); }}
              onDoubleClick={handleMapDoubleClick}
            >
              <rect x="-200" y="-200" width="1400" height="960" fill="#EAF6EE" />
              <g fill="#D3EDDC"><path d="M560 20c60-20 140 0 170 40s-20 70-90 70-120-40-80-110z" /><path d="M40 440c60-30 150-20 200 20s0 70-80 70-150-30-120-90z" /><path d="M690 410c50-30 130-20 150 30s-30 80-100 70-90-60-50-100z" /></g>
              <ellipse cx="140" cy="86" rx="100" ry="52" fill="#CFE8F0" />
              <path d="M-40 405C90 340 200 400 310 345S520 285 630 340 800 310 960 335" fill="none" stroke="#BFE3EE" strokeWidth="30" strokeLinecap="round" />
              <path d="M-40 405C90 340 200 400 310 345S520 285 630 340 800 310 960 335" fill="none" stroke="#D9F0F6" strokeWidth="12" strokeLinecap="round" />
              <g fill="none" stroke="#fff" strokeWidth="9" strokeLinecap="round"><path d="M0 230C160 250 300 190 450 250S740 210 900 250" /><path d="M450 -10C430 120 470 190 450 250S430 420 470 540" /><path d="M180 -10C200 100 160 200 220 300" /><path d="M700 -10C720 100 650 180 690 300S760 440 740 540" /><path d="M0 120C120 140 240 100 330 130" /></g>
              <g fill="none" stroke="#fff" strokeWidth="3" opacity=".9"><path d="M380 200h140M380 250h140M380 300h140M400 180v140M450 180v140M500 180v140M560 90l120 60M590 60l120 60" /></g>
              <g fontFamily="Inter,sans-serif" fontSize="12" fontWeight="700" letterSpacing="2" fill="#6B8F7B" opacity=".85"><text x="395" y="172">OLD TOWN</text><text x="205" y="372">RIVERSIDE</text><text x="590" y="88">TEMPLE HILL</text><text x="740" y="440">HARBOUR</text><text x="70" y="64">LAKESIDE</text></g>
              {layers.sh && <g>{matchingShelters.map(({ shelter, index }) => {
                if (shelter.x === undefined || shelter.y === undefined) return null;
                const occupancy = shelter.capacity === 0
                  ? 0
                  : Math.round((shelter.currentOccupancy / shelter.capacity) * 100);
                const color = occupancy >= 80 ? "#D93025" : "#1B6B47";
                const selected = selection?.type === "sh" && selection.index === index;
                return <g className="mapMarker" key={shelter.id} transform={`translate(${shelter.x} ${shelter.y})`} role="button" tabIndex={0} aria-label={`${shelter.name}, ${occupancy}% full`} onClick={() => selectItem("sh", index)} onKeyDown={(event) => { if (event.key === "Enter" || event.key === " ") { event.preventDefault(); selectItem("sh", index); } }}>
                  {selected && <circle className={styles.pulse} r="20" fill={color} opacity=".5" />}
                  <rect x="-16" y="-16" width="32" height="32" rx="10" fill={color} stroke="#fff" strokeWidth={selected ? 4 : 3} />
                  <path d="M-8 1L0-7L8 1V9H-8Z" fill="#fff" /><rect x="-2" y="3" width="4" height="6" fill={color} />
                  <rect x="-20" y="19" width="40" height="17" rx="8.5" fill="#fff" stroke={color} strokeWidth="2" /><text y="31.5" textAnchor="middle" fontFamily="Inter,sans-serif" fontSize="10.5" fontWeight="800" fill={color}>{occupancy}%</text>
                  <title>{shelter.name}</title>
                </g>;
              })}</g>}
              {layers.req && <g>{visibleRequests.map(({ request, index }) => {
                if (request.x === undefined || request.y === undefined) return null;
                const color = request.lvl ? urgencyColors[request.lvl] : "#718096";
                const selected = selection?.type === "req" && selection.index === index;
                return <g className="mapMarker" key={request.id} transform={`translate(${request.x} ${request.y})`} role="button" tabIndex={0} aria-label={`Request ${request.id}, ${request.cat}, ${request.lvl ?? "urgency not assessed"}`} onClick={() => selectItem("req", index)} onKeyDown={(event) => { if (event.key === "Enter" || event.key === " ") { event.preventDefault(); selectItem("req", index); } }}>
                  {selected && <circle className={styles.pulse} r="14" fill={color} opacity=".5" />}
                  <ellipse cy="1" rx="8" ry="3" fill="#000" opacity=".18" />
                  <path d="M0 0C-5-10-16-18-16-30a16 16 0 0 1 32 0C16-18 5-10 0 0z" fill={color} stroke="#fff" strokeWidth={selected ? 4 : 2.5} />
                  <circle cy="-30" r="6" fill="#fff" /><title>{`Request ${request.id}`}</title>
                </g>;
              })}</g>}
            </svg>
            <div className={styles.zoom}>
              <button type="button" aria-label="Zoom in" onClick={() => zoom(0.7)}>+</button>
              <button type="button" aria-label="Zoom out" onClick={() => zoom(1.4)}>−</button>
              <button type="button" aria-label="Reset map view" onClick={() => setViewBox(initialViewBox)}>↻</button>
            </div>
            <div className={styles.mapKey}><span><i className={styles.keyCritical} />Critical</span><span><i className={styles.keyHigh} />High</span><span><i className={styles.keyMedium} />Medium</span><span><i className={styles.keyLow} />Low</span><span><i className={styles.keyShelter} />Shelter</span></div>
            {selection && (
              <div className={styles.info} role="dialog" aria-label="Map item details">
                <button className={styles.closeInfo} type="button" aria-label="Close details" onClick={() => setSelection(null)}>×</button>
                {selectedRequest && <><h3>Request {selectedRequest.id} · {selectedRequest.cat}</h3><p>{selectedRequest.address || "Address not provided"}</p><p>{selectedRequest.description}</p><dl><div><dt>Urgency</dt><dd>{selectedRequest.lvl ? `${selectedRequest.lvl}${selectedRequest.sc === null ? "" : ` · ${selectedRequest.sc}%`}` : "Not assessed"}</dd></div><div><dt>Status</dt><dd><span className={`${styles.pill} ${styles[statusClass[selectedRequest.st]]}`}>{displayStatus(selectedRequest.st)}</span></dd></div><div><dt>Submitted</dt><dd>{new Date(selectedRequest.createdAt).toLocaleDateString()}</dd></div></dl></>}
                {selectedShelter && <><h3>{selectedShelter.name}</h3><p>Shelter</p><dl><div><dt>Occupancy</dt><dd><span className={`${styles.pill} ${selectedShelter.currentOccupancy / Math.max(selectedShelter.capacity, 1) >= 0.8 ? styles.high : styles.low}`}>{Math.round((selectedShelter.currentOccupancy / Math.max(selectedShelter.capacity, 1)) * 100)}% full</span></dd></div><div><dt>Beds used</dt><dd>{selectedShelter.currentOccupancy} of {selectedShelter.capacity}</dd></div></dl><div className={`${styles.bar} ${selectedShelter.currentOccupancy / Math.max(selectedShelter.capacity, 1) >= 0.8 ? styles.redBar : ""}`}><i style={{ width: `${Math.min(100, Math.round((selectedShelter.currentOccupancy / Math.max(selectedShelter.capacity, 1)) * 100))}%` }} /></div></>}
              </div>
            )}
          </div>
          {(shelterLoadError || resourceLoadError) && (
            <p className={styles.note} role="alert">
              {[shelterLoadError, resourceLoadError].filter(Boolean).join(" ")}
            </p>
          )}
          <p className={styles.note}>Markers are plotted relative to the coordinates returned by the API; records without coordinates are not shown on the map. The background is schematic. Resources are listed below because the resources API does not provide locations.</p>
        </section>

        <div className={styles.columns}>
          <section className={styles.card} id="queue" aria-labelledby="queue-title">
            <div className={styles.cardHeading}>
              <h2 id="queue-title">My Assistance Requests</h2>
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
                    <th>Submitted</th>
                  </tr>
                </thead>
                <tbody>
                  {isLoadingRequests && (
                    <tr><td colSpan={6} role="status">Loading your requests…</td></tr>
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
                          Date.parse(b.request.createdAt) -
                          Date.parse(a.request.createdAt),
                      )
                      .map(({ request, index }) => {
                        const markerColor = request.lvl
                          ? urgencyColors[request.lvl]
                          : "#718096";
                        return (
                          <tr
                            className={
                              selection?.type === "req" &&
                              selection.index === index
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
                                onClick={() => selectItem("req", index, true)}
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
                            <td>{new Date(request.createdAt).toLocaleDateString()}</td>
                          </tr>
                        );
                      })}
                </tbody>
              </table>
            </div>
          </section>
          <div className={styles.sideColumn}>
            <section className={styles.card} aria-labelledby="shelters-title">
              <div className={styles.cardHeading}><h2 id="shelters-title">Shelters &amp; Resources</h2><button className={styles.button} type="button" onClick={() => notify("Shelter management is not available yet.")}>Manage</button></div>
              <div>
                {shelters.slice(0, 3).map((shelter, index) => {
                  const occupancy = shelter.capacity === 0
                    ? 0
                    : Math.round((shelter.currentOccupancy / shelter.capacity) * 100);
                  return (
                    <button className={styles.shelterBar} key={shelter.id} type="button" aria-label={`Show ${shelter.name} on map`} onClick={() => selectItem("sh", index, true)}>
                      <span className={styles.shelterTop}><b>{shelter.name}</b><strong className={occupancy >= 80 ? styles.redText : ""}>{occupancy}% full</strong></span>
                      <span className={`${styles.bar} ${occupancy >= 80 ? styles.redBar : ""}`}><i style={{ width: `${Math.min(100, occupancy)}%` }} /></span>
                    </button>
                  );
                })}
                {!shelterLoadError && shelters.length === 0 && <p className={styles.note}>No shelters available.</p>}
              </div>
              <div className={styles.resourceList}>
                {matchingResources.map((resource) => (
                  <div key={resource.id}>
                    <span className={`${styles.pill} ${resource.quantityAvailable === 0 ? styles.critical : styles.normal}`}>
                      {resource.quantityAvailable === 0 ? "Out of stock" : resource.quantityAvailable}
                    </span>
                    {resource.name} · {resource.category}
                  </div>
                ))}
                {resourceLoadError && <p className={styles.note} role="alert">{resourceLoadError}</p>}
                {!resourceLoadError && matchingResources.length === 0 && <p className={styles.note}>No resources available.</p>}
              </div>
            </section>
            <section className={styles.card} aria-labelledby="notifications-title">
              <div className={styles.cardHeading}><h2 id="notifications-title">Recent Notification Logs</h2><span className={`${styles.tag} ${styles.redTag}`}>1 failed</span><button className={styles.button} type="button" onClick={() => notify("Test notification sent.")}>Test Notification</button></div>
              <div className={styles.logs}>
                <LogEntry title="Allocation update" detail="SMS · +94 ***1234" time="09:22" status="Sent" channel="sms" />
                <LogEntry title="Request received" detail="Email · c***@mail.com" time="09:12" status="Sent" channel="email" />
                <LogEntry title="Shelter assignment" detail="SMS · +94 ***5678" time="08:41" status="Failed" channel="sms" />
                <LogEntry title="Status update" detail="Email · v***@mail.com" time="08:20" status="Pending" channel="email" />
              </div>
            </section>
          </div>
        </div>
      </main>
      <div className={`${styles.toast} ${showToast ? styles.toastVisible : ""}`} role="status" aria-live="polite">{toast}</div>
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

function LogEntry({ title, detail, time, status, channel }: { title: string; detail: string; time: string; status: "Sent" | "Pending" | "Failed"; channel: "sms" | "email" }) {
  const className = status === "Sent" ? styles.logSuccess : status === "Pending" ? styles.logPending : styles.logFailed;
  return <div className={styles.logEntry}><span className={styles.logIcon}>{channel === "sms" ? <MessageIcon /> : <MailIcon />}</span><div><b>{title}</b><small>{detail}</small></div><div className={styles.logResult}><span className={`${styles.pill} ${className}`}>{status}</span><small>{time}</small></div></div>;
}

function MessageIcon() {
  return <svg viewBox="0 0 24 24" aria-hidden="true"><path d="M4 5h16v11H9l-5 4z" /></svg>;
}

function MailIcon() {
  return <svg viewBox="0 0 24 24" aria-hidden="true"><rect x="3" y="5" width="18" height="14" rx="2" /><path d="M3 7l9 6 9-6" /></svg>;
}
