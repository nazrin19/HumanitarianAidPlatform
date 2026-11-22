"use client";

import { useEffect, useMemo, useState } from "react";
import {
  CircleMarker,
  MapContainer,
  Popup,
  TileLayer,
  useMap,
} from "react-leaflet";
import type { LatLngTuple, Map as LeafletMapInstance } from "leaflet";
import styles from "./page.module.css";

export type MapRequestPoint = {
  index: number;
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
  index: number;
  id: string;
  name: string;
  latitude: number;
  longitude: number;
  currentOccupancy: number;
  capacity: number;
};

export type MapResourcePoint = {
  index: number;
  id: string;
  name: string;
  category: string;
  quantityAvailable: number;
  latitude: number;
  longitude: number;
};

export type MapSelection =
  | { type: "req" | "sh" | "res"; index: number }
  | null;

type Props = {
  mode: "staff" | "citizen";
  requests: MapRequestPoint[];
  shelters: MapShelterPoint[];
  resources: MapResourcePoint[];
  selection: MapSelection;
  onSelect: (type: "req" | "sh" | "res", index: number) => void;
  onMapReady: (map: LeafletMapInstance | null) => void;
  userLocation?: MapUserLocation | null;
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

function MapReady({ onMapReady }: { onMapReady: Props["onMapReady"] }) {
  const map = useMap();

  useEffect(() => {
    onMapReady(map);
    return () => onMapReady(null);
  }, [map, onMapReady]);

  return null;
}

function FitMapToPoints({
  points,
}: {
  points: LatLngTuple[];
}) {
  const map = useMap();

  useEffect(() => {
    if (points.length === 1) {
      map.setView(points[0], 12);
    } else if (points.length > 1) {
      map.fitBounds(points, { padding: [36, 36], maxZoom: 12 });
    }
  }, [map, points]);

  return null;
}

export default function LiveAidMap({
  mode,
  requests,
  shelters,
  resources,
  selection,
  onSelect,
  onMapReady,
  userLocation = null,
  isLoading = false,
  error = null,
  emptyMessage = "No mapped locations are available.",
}: Props) {
  const [tileError, setTileError] = useState(false);
  const points = useMemo(
    () =>
      [
        ...(userLocation
          ? [[userLocation.latitude, userLocation.longitude] as LatLngTuple]
          : []),
        ...(mode === "staff"
          ? requests.map(
              (request) =>
                [request.latitude, request.longitude] as LatLngTuple,
            )
          : []),
        ...shelters.map(
          (shelter) => [shelter.latitude, shelter.longitude] as LatLngTuple,
        ),
        ...resources.map(
          (resource) =>
            [resource.latitude, resource.longitude] as LatLngTuple,
        ),
      ],
    [mode, requests, shelters, resources, userLocation],
  );
  const hasPoints = points.length > 0;
  const notice = tileError
    ? "Map tiles could not be loaded. Check your connection and try again."
    : error || (isLoading ? "Loading map locations…" : !hasPoints ? emptyMessage : "");

  return (
    <MapContainer
      center={defaultCenter}
      zoom={7}
      scrollWheelZoom
      className="liveAidLeafletMap"
    >
      <MapReady onMapReady={onMapReady} />
      <FitMapToPoints points={points} />
      <TileLayer
        attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
        url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
        maxZoom={19}
        eventHandlers={{
          tileerror: () => setTileError(true),
          load: () => setTileError(false),
        }}
      />
      {userLocation && (
        <CircleMarker
          center={[userLocation.latitude, userLocation.longitude]}
          radius={9}
          pathOptions={{
            color: "#fff",
            weight: 3,
            fillColor: "#246BCE",
            fillOpacity: 1,
          }}
        >
          <Popup>
            <strong>You are here</strong>
          </Popup>
        </CircleMarker>
      )}
      {mode === "staff" && requests.map((request) => {
        const selected =
          selection?.type === "req" && selection.index === request.index;
        const color = urgencyColors[request.urgency ?? ""] ?? "#718096";

        return (
          <CircleMarker
            key={`request-${request.id}`}
            center={[request.latitude, request.longitude]}
            radius={selected ? 10 : 7}
            pathOptions={{
              color: "#fff",
              weight: selected ? 3 : 2,
              fillColor: color,
              fillOpacity: 0.9,
            }}
            eventHandlers={{
              click: () => onSelect("req", request.index),
            }}
          >
            <Popup>
              <strong>Request · {request.category}</strong>
              {request.address?.trim() && (
                <>
                  <br />
                  {request.address}
                </>
              )}
              <br />
              Urgency: {request.urgency ?? "Not assessed"}
              <br />
              Status: {request.status}
            </Popup>
          </CircleMarker>
        );
      })}
      {notice && (
        <div
          className={`${styles.mapNotice}${tileError || error ? ` ${styles.mapNoticeError}` : ""}`}
          role={tileError || error ? "alert" : "status"}
          aria-live="polite"
        >
          {notice}
        </div>
      )}
      {mode === "citizen" && resources.map((resource) => {
        const selected =
          selection?.type === "res" && selection.index === resource.index;

        return (
          <CircleMarker
            key={`resource-${resource.id}`}
            center={[resource.latitude, resource.longitude]}
            radius={selected ? 10 : 8}
            pathOptions={{
              color: "#fff",
              weight: selected ? 3 : 2,
              fillColor: "#246BCE",
              fillOpacity: 0.9,
            }}
            eventHandlers={{
              click: () => onSelect("res", resource.index),
            }}
          >
            <Popup>
              <strong>{resource.name}</strong>
              <br />
              {resource.category}: {resource.quantityAvailable} available
            </Popup>
          </CircleMarker>
        );
      })}
      {shelters.map((shelter) => {
        const selected =
          selection?.type === "sh" && selection.index === shelter.index;
        const occupancy =
          shelter.capacity > 0
            ? Math.round(
                (shelter.currentOccupancy / shelter.capacity) * 100,
              )
            : 0;
        const color = occupancy >= 80 ? "#D93025" : "#1B6B47";

        return (
          <CircleMarker
            key={`shelter-${shelter.id}`}
            center={[shelter.latitude, shelter.longitude]}
            radius={selected ? 10 : 8}
            pathOptions={{
              color: "#fff",
              weight: selected ? 3 : 2,
              fillColor: color,
              fillOpacity: 0.9,
            }}
            eventHandlers={{
              click: () => onSelect("sh", shelter.index),
            }}
          >
            <Popup>
              <strong>{shelter.name}</strong>
              <br />
              Occupancy: {occupancy}% ({shelter.currentOccupancy}/
              {shelter.capacity})
            </Popup>
          </CircleMarker>
        );
      })}
    </MapContainer>
  );
}
