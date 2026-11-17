"use client";

import Image from "next/image";
import Link from "next/link";
import {
  CSSProperties,
  MouseEvent as ReactMouseEvent,
  PointerEvent,
  useRef,
  useState,
} from "react";
import styles from "./page.module.css";

type Urgency = "High" | "Medium" | "Low";
type RequestStatus = "Pending" | "Approved" | "Allocated" | "Completed";
type Layer = "req" | "sh" | "res";
type Selection = { type: Layer; index: number } | null;
type ViewBox = { x: number; y: number; w: number; h: number };

type AidRequest = {
  id: number;
  cat: string;
  name: string;
  lvl: Urgency;
  sc: number;
  st: RequestStatus;
  x: number;
  y: number;
  area: string;
};

type Shelter = {
  n: string;
  p: number;
  u: number;
  c: number;
  x: number;
  y: number;
};

type Depot = {
  n: string;
  item: string;
  s: "Normal" | "Low stock" | "Critical";
  x: number;
  y: number;
};

const initialRequests: AidRequest[] = [
  { id: 1042, cat: "Temporary shelter", name: "S. Fernando", lvl: "High", sc: 92, st: "Pending", x: 402, y: 222, area: "Old Town" },
  { id: 1041, cat: "Medicine", name: "K. Rajan", lvl: "High", sc: 84, st: "Approved", x: 292, y: 318, area: "Riverside" },
  { id: 1040, cat: "Food", name: "N. Perera", lvl: "Medium", sc: 67, st: "Pending", x: 612, y: 222, area: "Temple Hill" },
  { id: 1039, cat: "Water", name: "M. Rahman", lvl: "High", sc: 88, st: "Allocated", x: 722, y: 148, area: "Temple Hill" },
  { id: 1038, cat: "Transport", name: "D. Silva", lvl: "Low", sc: 71, st: "Pending", x: 520, y: 318, area: "Old Town" },
  { id: 1035, cat: "Accessibility help", name: "P. Jayasinghe", lvl: "Medium", sc: 74, st: "Approved", x: 212, y: 196, area: "Lakeside" },
  { id: 1031, cat: "Food", name: "L. Wickrama", lvl: "Low", sc: 69, st: "Completed", x: 742, y: 372, area: "Harbour" },
];

const shelters: Shelter[] = [
  { n: "City Center Shelter", p: 85, u: 255, c: 300, x: 446, y: 262 },
  { n: "Riverside Hall", p: 54, u: 162, c: 300, x: 262, y: 352 },
  { n: "Temple Grounds", p: 39, u: 117, c: 300, x: 652, y: 172 },
  { n: "Harbour Hall", p: 71, u: 142, c: 200, x: 790, y: 402 },
  { n: "Lakeside Centre", p: 22, u: 44, c: 200, x: 152, y: 166 },
  { n: "Hilltop School", p: 48, u: 96, c: 200, x: 566, y: 446 },
];

const depots: Depot[] = [
  { n: "Central Food Depot", item: "Food", s: "Normal", x: 500, y: 128 },
  { n: "Medical Store", item: "Medical Kits", s: "Low stock", x: 362, y: 432 },
  { n: "Water Store", item: "Water", s: "Critical", x: 116, y: 408 },
];

const urgencyColors: Record<Urgency, string> = {
  High: "#D93025",
  Medium: "#F29900",
  Low: "#1FA463",
};

const statusClass: Record<RequestStatus, string> = {
  Pending: "pending",
  Approved: "approved",
  Allocated: "allocated",
  Completed: "completed",
};

const stockClass: Record<Depot["s"], string> = {
  Normal: "normal",
  "Low stock": "lowStock",
  Critical: "critical",
};

const initialViewBox: ViewBox = { x: 0, y: 0, w: 900, h: 520 };

function isDone(request: AidRequest) {
  return request.st === "Allocated" || request.st === "Completed";
}

function getSource(request: AidRequest) {
  const matchingItem: Record<string, string> = {
    Medicine: "Medical Kits",
    Water: "Water",
    Food: "Food",
  };
  if (matchingItem[request.cat]) {
    return depots.find((depot) => depot.item === matchingItem[request.cat]);
  }
  return shelters
    .filter((shelter) => shelter.p < 80)
    .sort(
      (a, b) =>
        Math.hypot(a.x - request.x, a.y - request.y) -
        Math.hypot(b.x - request.x, b.y - request.y),
    )[0];
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
  const [requests, setRequests] = useState(initialRequests);
  const [layers, setLayers] = useState<Record<Layer, boolean>>({
    req: true,
    sh: true,
    res: true,
  });
  const [urgency, setUrgency] = useState("all");
  const [query, setQuery] = useState("");
  const [selection, setSelection] = useState<Selection>(null);
  const [routes, setRoutes] = useState<{ id: number; source: Shelter | Depot }[]>([]);
  const [viewBox, setViewBox] = useState(initialViewBox);
  const [toast, setToast] = useState("");
  const [showToast, setShowToast] = useState(false);
  const [isDragging, setIsDragging] = useState(false);
  const mapRef = useRef<SVGSVGElement>(null);
  const mapCardRef = useRef<HTMLElement>(null);
  const dragRef = useRef<{ x: number; y: number; viewX: number; viewY: number } | null>(null);
  const toastTimer = useRef<ReturnType<typeof setTimeout> | null>(null);
  const normalizedQuery = query.trim().toLowerCase();

  function matches(value: string) {
    return value.toLowerCase().includes(normalizedQuery);
  }

  const visibleRequests = requests
    .map((request, index) => ({ request, index }))
    .filter(
      ({ request }) =>
        (urgency === "all" || urgency === request.lvl) &&
        matches(
          `${request.id} ${request.cat} ${request.name} ${request.area} ${request.lvl} ${request.st}`,
        ),
    );

  const matchingShelters = shelters.filter((shelter) => matches(shelter.n));
  const matchingDepots = depots.filter((depot) =>
    matches(`${depot.n} ${depot.item}`),
  );
  const selectedRequest =
    selection?.type === "req" ? requests[selection.index] : undefined;
  const selectedShelter =
    selection?.type === "sh" ? shelters[selection.index] : undefined;
  const selectedDepot =
    selection?.type === "res" ? depots[selection.index] : undefined;

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
          ? requests[index]
          : type === "sh"
            ? shelters[index]
            : depots[index];
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

  function allocate(index: number) {
    const request = requests[index];
    if (isDone(request)) return;
    const source = getSource(request);
    if (!source) {
      notify(`No suitable source is available for request #REQ-${request.id}.`);
      return;
    }
    setRoutes((current) => [...current, { id: request.id, source }]);
    setRequests((current) =>
      current.map((item, itemIndex) =>
        itemIndex === index ? { ...item, st: "Allocated" } : item,
      ),
    );
    selectItem("req", index, true);
    notify(`Allocation triggered: ${source.n} → #REQ-${request.id}`);
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
  const depotCount = layers.res ? matchingDepots.length : 0;
  const countText = [
    layers.req ? `${requestCount} requests` : "",
    layers.sh ? `${shelterCount} shelters` : "",
    layers.res ? `${depotCount} depots` : "",
  ]
    .filter(Boolean)
    .join(" · ");

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
            <div className={styles.kpiHeading}><span className={styles.kpiIcon}><DocumentIcon /></span>Assistance Requests<span className={`${styles.tag} ${styles.redTag}`}>34 urgent</span></div>
            <div className={styles.big}>128</div><div className={styles.small}>Total active requests</div>
            <div className={styles.segment}><i /><i /><i /></div>
            <div className={styles.legend}><span className={styles.highDot}>High 34</span><span className={styles.mediumDot}>Medium 61</span><span className={styles.lowDot}>Low 33</span></div>
          </article>
          <article className={styles.card}>
            <div className={styles.kpiHeading}><span className={styles.kpiIcon}><ShelterIcon /></span>Shelters</div>
            <div className={styles.big}>17</div><div className={styles.small}>842 of 1,240 capacity used</div>
            <div className={styles.bar}><i style={{ width: "68%" }} /></div><div className={styles.row}><span>Occupancy rate</span><b>68%</b></div>
          </article>
          <article className={styles.card}>
            <div className={styles.kpiHeading}><span className={styles.kpiIcon}><BoxIcon /></span>Resources</div>
            <div className={styles.big}>3,640</div><div className={styles.small}>Available inventory items</div>
            <div className={styles.kpiBadges}><span className={`${styles.tag} ${styles.amberTag}`}>4 low-stock</span><span className={`${styles.tag} ${styles.redTag}`}>Water critical</span></div>
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
              {([["req", "Requests"], ["sh", "Shelters"], ["res", "Resources"]] as const).map(([layer, label]) => (
                <button className={styles.chip} key={layer} type="button" aria-pressed={layers[layer]} onClick={() => toggleLayer(layer)}><span />{label}</button>
              ))}
            </div>
            <select className={styles.select} aria-label="Filter requests by urgency" value={urgency} onChange={(event) => setUrgency(event.target.value)}>
              <option value="all">All urgency levels</option><option>High</option><option>Medium</option><option>Low</option>
            </select>
            <span className={styles.count} aria-live="polite">{countText}</span>
          </div>
          <div className={styles.mapBox}>
            <svg
              ref={mapRef}
              className={`${styles.map} ${isDragging ? styles.dragging : ""}`}
              viewBox={`${viewBox.x} ${viewBox.y} ${viewBox.w} ${viewBox.h}`}
              role="group"
              aria-label="Schematic map of requests, shelters and resource depots"
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
              <g fill="none" stroke="#1B6B47" strokeWidth="4" strokeLinecap="round">
                {routes.map(({ id, source }) => {
                  const request = requests.find((item) => item.id === id);
                  if (!request) return null;
                  return <path className={styles.route} key={id} d={`M${source.x} ${source.y}Q${(source.x + request.x) / 2} ${Math.min(source.y, request.y) - 60} ${request.x} ${request.y - 14}`} />;
                })}
              </g>
              {layers.res && <g>{matchingDepots.map((depot) => {
                const index = depots.indexOf(depot);
                const color = depot.s === "Normal" ? "#1FA463" : depot.s === "Low stock" ? "#F29900" : "#D93025";
                const selected = selection?.type === "res" && selection.index === index;
                return <g className="mapMarker" key={depot.n} transform={`translate(${depot.x} ${depot.y})`} role="button" tabIndex={0} aria-label={`${depot.n}, ${depot.item} ${depot.s}`} onClick={() => selectItem("res", index)} onKeyDown={(event) => { if (event.key === "Enter" || event.key === " ") { event.preventDefault(); selectItem("res", index); } }}>
                  {selected && <circle className={styles.pulse} r="20" fill={color} opacity=".5" />}
                  <rect x="-14" y="-14" width="28" height="28" rx="7" transform="rotate(45)" fill={color} stroke="#fff" strokeWidth={selected ? 4 : 3} />
                  <path d="M-6 -2h12v9h-12zM-6 -2l3-4h6l3 4" fill="none" stroke="#fff" strokeWidth="2" strokeLinejoin="round" />
                  <title>{depot.n}</title>
                </g>;
              })}</g>}
              {layers.sh && <g>{matchingShelters.map((shelter) => {
                const index = shelters.indexOf(shelter);
                const color = shelter.p >= 80 ? "#D93025" : "#1B6B47";
                const selected = selection?.type === "sh" && selection.index === index;
                return <g className="mapMarker" key={shelter.n} transform={`translate(${shelter.x} ${shelter.y})`} role="button" tabIndex={0} aria-label={`${shelter.n}, ${shelter.p}% full`} onClick={() => selectItem("sh", index)} onKeyDown={(event) => { if (event.key === "Enter" || event.key === " ") { event.preventDefault(); selectItem("sh", index); } }}>
                  {selected && <circle className={styles.pulse} r="20" fill={color} opacity=".5" />}
                  <rect x="-16" y="-16" width="32" height="32" rx="10" fill={color} stroke="#fff" strokeWidth={selected ? 4 : 3} />
                  <path d="M-8 1L0-7L8 1V9H-8Z" fill="#fff" /><rect x="-2" y="3" width="4" height="6" fill={color} />
                  <rect x="-20" y="19" width="40" height="17" rx="8.5" fill="#fff" stroke={color} strokeWidth="2" /><text y="31.5" textAnchor="middle" fontFamily="Inter,sans-serif" fontSize="10.5" fontWeight="800" fill={color}>{shelter.p}%</text>
                  <title>{shelter.n}</title>
                </g>;
              })}</g>}
              {layers.req && <g>{visibleRequests.map(({ request, index }) => {
                const color = urgencyColors[request.lvl];
                const selected = selection?.type === "req" && selection.index === index;
                return <g className="mapMarker" key={request.id} transform={`translate(${request.x} ${request.y})`} role="button" tabIndex={0} aria-label={`Request #REQ-${request.id}, ${request.cat}, ${request.lvl} urgency`} onClick={() => selectItem("req", index)} onKeyDown={(event) => { if (event.key === "Enter" || event.key === " ") { event.preventDefault(); selectItem("req", index); } }}>
                  {selected && <circle className={styles.pulse} r="14" fill={color} opacity=".5" />}
                  <ellipse cy="1" rx="8" ry="3" fill="#000" opacity=".18" />
                  <path d="M0 0C-5-10-16-18-16-30a16 16 0 0 1 32 0C16-18 5-10 0 0z" fill={color} stroke="#fff" strokeWidth={selected ? 4 : 2.5} />
                  <circle cy="-30" r="6" fill="#fff" /><title>{`Request #REQ-${request.id}`}</title>
                </g>;
              })}</g>}
            </svg>
            <div className={styles.zoom}>
              <button type="button" aria-label="Zoom in" onClick={() => zoom(0.7)}>+</button>
              <button type="button" aria-label="Zoom out" onClick={() => zoom(1.4)}>−</button>
              <button type="button" aria-label="Reset map view" onClick={() => setViewBox(initialViewBox)}>↻</button>
            </div>
            <div className={styles.mapKey}><span><i className={styles.keyHigh} />High</span><span><i className={styles.keyMedium} />Medium</span><span><i className={styles.keyLow} />Low request</span><span><i className={styles.keyShelter} />Shelter</span><span><i className={styles.keyDepot} />Resource depot</span></div>
            {selection && (
              <div className={styles.info} role="dialog" aria-label="Map item details">
                <button className={styles.closeInfo} type="button" aria-label="Close details" onClick={() => setSelection(null)}>×</button>
                {selectedRequest && <><h3>#REQ-{selectedRequest.id} · {selectedRequest.cat}</h3><p>{selectedRequest.area} · {selectedRequest.name}</p><dl><div><dt>AI urgency</dt><dd><span className={`${styles.pill} ${styles[urgencyClass(selectedRequest.lvl)]}`}>{selectedRequest.lvl} · {selectedRequest.sc}%</span></dd></div><div><dt>Status</dt><dd><span className={`${styles.pill} ${styles[statusClass[selectedRequest.st]]}`}>{selectedRequest.st}</span></dd></div><div><dt>Suggested source</dt><dd>{getSource(selectedRequest)?.n ?? "—"}</dd></div></dl><div className={styles.infoActions}><button className={`${styles.button} ${styles.primaryButton}`} type="button" disabled={isDone(selectedRequest)} onClick={() => allocate(selection.index)}>Trigger Allocation</button></div></>}
                {selectedShelter && <><h3>{selectedShelter.n}</h3><p>Shelter</p><dl><div><dt>Occupancy</dt><dd><span className={`${styles.pill} ${selectedShelter.p >= 80 ? styles.high : styles.low}`}>{selectedShelter.p}% full</span></dd></div><div><dt>Beds used</dt><dd>{selectedShelter.u} of {selectedShelter.c}</dd></div><div><dt>Open requests nearby</dt><dd>{requests.filter((request) => Math.hypot(request.x - selectedShelter.x, request.y - selectedShelter.y) < 230 && request.st !== "Completed").length}</dd></div></dl><div className={`${styles.bar} ${selectedShelter.p >= 80 ? styles.redBar : ""}`}><i style={{ width: `${selectedShelter.p}%` }} /></div></>}
                {selectedDepot && <><h3>{selectedDepot.n}</h3><p>Resource depot</p><dl><div><dt>Item</dt><dd>{selectedDepot.item}</dd></div><div><dt>Stock level</dt><dd><span className={`${styles.pill} ${styles[stockClass[selectedDepot.s]]}`}>{selectedDepot.s}</span></dd></div></dl></>}
              </div>
            )}
          </div>
          <p className={styles.note}>Schematic map with sample data. Drag to pan, use the buttons to zoom, and select any marker or request ID for details.</p>
        </section>

        <div className={styles.columns}>
          <section className={styles.card} id="queue" aria-labelledby="queue-title">
            <div className={styles.cardHeading}><h2 id="queue-title">Incoming Assistance Requests &amp; Urgency Queue</h2><button className={styles.button} type="button" onClick={showAllRequests}>View all</button></div>
            <div className={styles.tableScroll}><table><colgroup><col className={styles.colId} /><col className={styles.colCategory} /><col className={styles.colName} /><col className={styles.colUrgency} /><col className={styles.colStatus} /><col className={styles.colActions} /></colgroup><thead><tr><th>Request ID</th><th>Category</th><th>Citizen</th><th>AI Urgency</th><th>Status</th><th>Actions</th></tr></thead><tbody>
              {visibleRequests.slice().sort((a, b) => ["High", "Medium", "Low"].indexOf(a.request.lvl) - ["High", "Medium", "Low"].indexOf(b.request.lvl) || b.request.sc - a.request.sc).map(({ request, index }) => (
                <tr className={selection?.type === "req" && selection.index === index ? styles.highlightedRow : ""} key={request.id} style={{ "--level-color": urgencyColors[request.lvl] } as CSSProperties}>
                  <td><button className={styles.requestId} type="button" aria-label={`Show request ${request.id} on map`} onClick={() => selectItem("req", index, true)}>#REQ-{request.id}<svg viewBox="0 0 24 24" aria-hidden="true"><path d="M12 21s-7-6-7-11a7 7 0 0 1 14 0c0 5-7 11-7 11z" /><circle cx="12" cy="10" r="2.5" /></svg></button></td>
                  <td className={styles.category}>{request.cat}</td><td className={styles.citizen}>{request.name}</td>
                  <td><div className={styles.urgencyCell}><span className={`${styles.pill} ${styles[urgencyClass(request.lvl)]}`}>{request.lvl} · {request.sc}%</span><span className={styles.meter}><i style={{ width: `${request.sc}%`, background: urgencyColors[request.lvl] }} /></span></div></td>
                  <td><span className={`${styles.pill} ${styles[statusClass[request.st]]}`}>{request.st}</span></td>
                  <td><div className={styles.rowActions}><button className={styles.button} type="button" onClick={() => selectItem("req", index, true)}>Review</button><button className={`${styles.button} ${styles.primaryButton}`} type="button" disabled={isDone(request)} onClick={() => allocate(index)}>Allocate</button></div></td>
                </tr>
              ))}
            </tbody></table></div>
          </section>
          <div className={styles.sideColumn}>
            <section className={styles.card} aria-labelledby="shelters-title">
              <div className={styles.cardHeading}><h2 id="shelters-title">Shelters &amp; Resources</h2><button className={styles.button} type="button" onClick={() => notify("Shelter management is not available yet.")}>Manage</button></div>
              <div>{shelters.slice(0, 3).map((shelter, index) => <button className={styles.shelterBar} key={shelter.n} type="button" aria-label={`Show ${shelter.n} on map`} onClick={() => selectItem("sh", index, true)}><span className={styles.shelterTop}><b>{shelter.n}</b><strong className={shelter.p >= 80 ? styles.redText : ""}>{shelter.p}% full</strong></span><span className={`${styles.bar} ${shelter.p >= 80 ? styles.redBar : ""}`}><i style={{ width: `${shelter.p}%` }} /></span></button>)}</div>
              <div className={styles.resourceList}><div><span className={`${styles.pill} ${styles.normal}`}>Normal</span>Food</div><div><span className={`${styles.pill} ${styles.lowStock}`}>Low stock</span>Medical Kits</div><div><span className={`${styles.pill} ${styles.critical}`}>Critical</span>Water</div></div>
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

function urgencyClass(level: Urgency) {
  return level === "High" ? "high" : level === "Medium" ? "medium" : "low";
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
