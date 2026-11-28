"use client";

import { useEffect, useMemo, useRef, useState } from "react";
import L from "leaflet";
import type { LatLngTuple, Map as LeafletMapInstance } from "leaflet";
import styles from "./LiveAidMap.module.css";

export type MapRequestPoint = {
  id: string;
  category: string;
  address?: string | null;
  latitude: number;
  longitude: number;
  urgency: string | null;
  status: string;
};

export type MapUserLocation = {
  latitude: number;
  longitude: number;
};

export type MapShelterPoint = {
  id: string;
  name: string;
  latitude: number;
  longitude: number;
  currentOccupancy: number;
  capacity: number;
};

export type MapResourcePoint = {
  id: string;
  name: string;
  category: string;
  quantityAvailable: number;
  latitude: number;
  longitude: number;
};

export type MapGeocodedPoint = {
  latitude: number;
  longitude: number;
  label: string;
};

export type MapServiceArea = {
  id: string;
  name: string;
  geoJson: string;
};

export type MapRouteOverlay = {
  distanceKm: number;
  routeGeoJson: string;
  message: string;
};

export type MapMarkerType = "request" | "shelter" | "resource";
export type MapSelection =
  | { type: MapMarkerType; id: string }
  | null;

export type LiveAidMapProps = {
  mode: "staff" | "citizen";
  requests: MapRequestPoint[];
  shelters: MapShelterPoint[];
  resources: MapResourcePoint[];
  selection: MapSelection;
  onSelect: (type: MapMarkerType, id: string) => void;
  onMapReady: (map: LeafletMapInstance | null) => void;
  userLocation?: MapUserLocation | null;
  geocodedLocation?: MapGeocodedPoint | null;
  serviceAreas?: MapServiceArea[];
  route?: MapRouteOverlay | null;
  onPinDrop?: (latitude: number, longitude: number) => void;
  isLoading?: boolean;
  error?: string | null;
  emptyMessage?: string;
};

const defaultCenter: LatLngTuple = [7.8731, 80.7718];
const urgencyColors: Record<string, string> = {
  Critical: "#A31D1D",
  High: "#D93025",
  Medium: "#F29900",
  Low: "#1FA463",
};

export default function LiveAidMap({
  mode,
  requests,
  shelters,
  resources,
  selection,
  onSelect,
  onMapReady,
  userLocation = null,
  geocodedLocation = null,
  serviceAreas = [],
  route = null,
  onPinDrop,
  isLoading = false,
  error = null,
  emptyMessage = "No mapped locations are available.",
}: LiveAidMapProps) {
  const containerRef = useRef<HTMLDivElement>(null);
  const mapRef = useRef<LeafletMapInstance | null>(null);
  const markerLayerRef = useRef<L.LayerGroup | null>(null);
  const onPinDropRef = useRef(onPinDrop);
  const [failedTileCount, setFailedTileCount] = useState(0);
  const [overlayError, setOverlayError] = useState("");

  onPinDropRef.current = onPinDrop;

  const aidPoints = useMemo(
    () =>
      [
        ...(mode === "staff"
          ? requests.map(
              (request) =>
                [request.latitude, request.longitude] as LatLngTuple,
            )
          : []),
        ...shelters.map(
          (shelter) => [shelter.latitude, shelter.longitude] as LatLngTuple,
        ),
        ...(mode === "citizen"
          ? resources.map(
              (resource) =>
                [resource.latitude, resource.longitude] as LatLngTuple,
            )
          : []),
      ],
    [mode, requests, shelters, resources],
  );

  const points = useMemo(
    () => [
      ...(userLocation
        ? [[userLocation.latitude, userLocation.longitude] as LatLngTuple]
        : []),
      ...(geocodedLocation
        ? [[geocodedLocation.latitude, geocodedLocation.longitude] as LatLngTuple]
        : []),
      ...aidPoints,
    ],
    [aidPoints, geocodedLocation, userLocation],
  );

  useEffect(() => {
    if (!containerRef.current || mapRef.current) return;

    const map = L.map(containerRef.current, {
      center: defaultCenter,
      zoom: 7,
      scrollWheelZoom: true,
    });
    mapRef.current = map;
    markerLayerRef.current = L.layerGroup().addTo(map);
    map.on("click", (event) => {
      onPinDropRef.current?.(event.latlng.lat, event.latlng.lng);
    });

    L.tileLayer("https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png", {
      attribution: "&copy; OpenStreetMap contributors",
      maxZoom: 19,
    })
      .on("tileerror", () => setFailedTileCount((count) => count + 1))
      .on("tileload", () =>
        setFailedTileCount((count) => Math.max(0, count - 1)),
      )
      .addTo(map);

    onMapReady(map);

    return () => {
      onMapReady(null);
      map.remove();
      mapRef.current = null;
      markerLayerRef.current = null;
    };
  }, [onMapReady]);

  useEffect(() => {
    const map = mapRef.current;
    const markerLayer = markerLayerRef.current;
    if (!map || !markerLayer) return;

    markerLayer.clearLayers();
    setOverlayError("");

    try {
      serviceAreas.forEach((area) => {
        const geoJson = JSON.parse(area.geoJson) as GeoJSON.GeoJsonObject;
        L.geoJSON(geoJson, {
          style: {
            color: "#1b6b47",
            weight: 2,
            fillColor: "#4ca77d",
            fillOpacity: 0.16,
          },
        })
          .bindTooltip(area.name)
          .addTo(markerLayer);
      });

      if (route) {
        const routeGeoJson = JSON.parse(
          route.routeGeoJson,
        ) as GeoJSON.GeoJsonObject;
        L.geoJSON(routeGeoJson, {
          style: { color: "#246bce", weight: 4, dashArray: "8 6" },
        }).addTo(markerLayer);
      }
    } catch {
      setOverlayError("A map layer could not be displayed.");
    }

    if (userLocation) {
      L.circleMarker([userLocation.latitude, userLocation.longitude], {
        radius: 9,
        color: "#fff",
        weight: 3,
        fillColor: "#246BCE",
        fillOpacity: 1,
      })
        .bindPopup("<strong>You are here</strong>")
        .addTo(markerLayer);
    }

    if (geocodedLocation) {
      L.marker([geocodedLocation.latitude, geocodedLocation.longitude], {
        icon: L.divIcon({
          className: styles.geocodedLocationIcon,
          html: "<span></span>",
          iconSize: [24, 36],
          iconAnchor: [12, 36],
          popupAnchor: [0, -32],
        }),
      })
        .bindPopup(
          `<strong>Location preview</strong><br>${geocodedLocation.label}`,
        )
        .addTo(markerLayer);
    }

    if (mode === "staff") {
      requests.forEach((request) => {
        const selected =
          selection?.type === "request" && selection.id === request.id;
        const color = urgencyColors[request.urgency ?? ""] ?? "#718096";
        L.circleMarker([request.latitude, request.longitude], {
          radius: selected ? 10 : 7,
          color: "#fff",
          weight: selected ? 3 : 2,
          fillColor: color,
          fillOpacity: 0.9,
        })
          .on("click", () => onSelect("request", request.id))
          .bindPopup(
            `<strong>Request · ${request.category}</strong>${request.address?.trim() ? `<br>${request.address}` : ""}<br>Urgency: ${request.urgency ?? "Not assessed"}<br>Status: ${request.status}`,
          )
          .addTo(markerLayer);
      });
    }

    if (mode === "citizen") {
      resources.forEach((resource) => {
        const selected =
          selection?.type === "resource" && selection.id === resource.id;
        L.circleMarker([resource.latitude, resource.longitude], {
          radius: selected ? 10 : 8,
          color: "#fff",
          weight: selected ? 3 : 2,
          fillColor: "#246BCE",
          fillOpacity: 0.9,
        })
          .on("click", () => onSelect("resource", resource.id))
          .bindPopup(
            `<strong>${resource.name}</strong><br>${resource.category}: ${resource.quantityAvailable} available`,
          )
          .addTo(markerLayer);
      });
    }

    shelters.forEach((shelter) => {
      const selected =
        selection?.type === "shelter" && selection.id === shelter.id;
      const occupancy =
        shelter.capacity > 0
          ? Math.round(
              (shelter.currentOccupancy / shelter.capacity) * 100,
            )
          : 0;
      L.circleMarker([shelter.latitude, shelter.longitude], {
        radius: selected ? 10 : 8,
        color: "#fff",
        weight: selected ? 3 : 2,
        fillColor: occupancy >= 80 ? "#D93025" : "#1B6B47",
        fillOpacity: 0.9,
      })
        .on("click", () => onSelect("shelter", shelter.id))
        .bindPopup(
          `<strong>${shelter.name}</strong><br>Occupancy: ${occupancy}% (${shelter.currentOccupancy}/${shelter.capacity})`,
        )
        .addTo(markerLayer);
    });

    if (points.length === 1) {
      map.setView(points[0], 12);
    } else if (points.length > 1) {
      map.fitBounds(points, { padding: [36, 36], maxZoom: 12 });
    }
  }, [
    geocodedLocation,
    mode,
    onSelect,
    points,
    route,
    requests,
    resources,
    selection,
    serviceAreas,
    shelters,
    userLocation,
  ]);

  const tileError = failedTileCount > 0;
  const notice = tileError
    ? "Map tiles could not be loaded. Check your connection and try again."
    : error ||
    overlayError ||
      (isLoading
        ? "Loading map locations…"
        : aidPoints.length === 0
          ? emptyMessage
          : "");

  return (
    <div
      ref={containerRef}
      className={styles.map}
      aria-label="Aid locations map"
    >
      {notice && (
        <div
          className={`${styles.mapNotice}${tileError || error ? ` ${styles.mapNoticeError}` : ""}`}
          role={tileError || error || overlayError ? "alert" : "status"}
        >
          {notice}
        </div>
      )}
    </div>
  );
}
