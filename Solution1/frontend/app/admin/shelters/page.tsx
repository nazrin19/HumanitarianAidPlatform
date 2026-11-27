
"use client";

import {
  useCallback,
  useEffect,
  useMemo,
  useState,
  type FormEvent,
} from "react";
import Link from "next/link";
import Image from "next/image";
import { useRouter } from "next/navigation";
import styles from "./page.module.css";
import {
  AUTH_SESSION_LOADING,
  clearSessionCredentials,
  useSessionRole,
  useSessionToken,
} from "../../lib/auth-session";

type Shelter = {
  id: string;
  name: string;
  capacity: number;
  currentOccupancy: number;
  latitude?: number | null;
  longitude?: number | null;
};

type ShelterForm = {
  name: string;
  capacity: string;
  currentOccupancy: string;
  latitude: string;
  longitude: string;
};

const API_BASE_URL = (
  process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5001"
).replace(/\/$/, "");

const EMPTY_FORM: ShelterForm = {
  name: "",
  capacity: "",
  currentOccupancy: "0",
  latitude: "",
  longitude: "",
};

export default function ShelterManagementPage() {
  const router = useRouter();
  const token = useSessionToken();
  const role = useSessionRole();

  const [shelters, setShelters] = useState<Shelter[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [message, setMessage] = useState("");
  const [search, setSearch] = useState("");
  const [filter, setFilter] = useState("all");

  const [modalOpen, setModalOpen] = useState(false);
  const [detailsOpen, setDetailsOpen] = useState(false);
  const [editing, setEditing] = useState<Shelter | null>(null);
  const [selected, setSelected] = useState<Shelter | null>(null);
  const [form, setForm] = useState<ShelterForm>(EMPTY_FORM);
  const [saving, setSaving] = useState(false);
  const [deleting, setDeleting] = useState<string | null>(null);

  const loadShelters = useCallback(async () => {
    if (!token || token === AUTH_SESSION_LOADING) return;

    setLoading(true);
    setError("");

    try {
      const response = await fetch(`${API_BASE_URL}/api/shelters`, {
        headers: { Authorization: `Bearer ${token}` },
        cache: "no-store",
      });

      if (response.status === 401) {
        clearSessionCredentials();
        router.replace("/login");
        return;
      }

      if (!response.ok) {
        throw new Error(`Unable to load shelters (${response.status}).`);
      }

      const data: unknown = await response.json();
      const list = Array.isArray(data)
        ? data
        : data && typeof data === "object" && "items" in data
          ? (data as { items: unknown }).items
          : null;

      if (!Array.isArray(list)) {
        throw new Error("Unexpected response received from the server.");
      }

      setShelters(list as Shelter[]);
    } catch (err) {
      setError(
        err instanceof Error ? err.message : "Unable to connect to the server."
      );
    } finally {
      setLoading(false);
    }
  }, [token, router]);

  useEffect(() => {
    if (
      token === AUTH_SESSION_LOADING ||
      role === AUTH_SESSION_LOADING
    ) return;

    if (!token) {
      router.replace("/login");
      return;
    }

    if (role !== "Admin" && role !== "ShelterAdmin") {
      router.replace("/portal");
      return;
    }

    void loadShelters();
  }, [token, role, router, loadShelters]);

  const filteredShelters = useMemo(() => {
    return shelters.filter((shelter) => {
      const matchesSearch = shelter.name
        .toLowerCase()
        .includes(search.trim().toLowerCase());

      const full = shelter.currentOccupancy >= shelter.capacity;

      const matchesFilter =
        filter === "all" ||
        (filter === "available" && !full) ||
        (filter === "full" && full);

      return matchesSearch && matchesFilter;
    });
  }, [shelters, search, filter]);

  const totalCapacity = shelters.reduce(
    (sum, shelter) => sum + shelter.capacity,
    0
  );

  const totalResidents = shelters.reduce(
    (sum, shelter) => sum + shelter.currentOccupancy,
    0
  );

  const availableBeds = Math.max(0, totalCapacity - totalResidents);

  const occupancyRate = totalCapacity
    ? Math.round((totalResidents / totalCapacity) * 100)
    : 0;

  function openAddModal() {
    setEditing(null);
    setForm(EMPTY_FORM);
    setError("");
    setModalOpen(true);
  }

  function openEditModal(shelter: Shelter) {
    setEditing(shelter);
    setForm({
      name: shelter.name,
      capacity: String(shelter.capacity),
      currentOccupancy: String(shelter.currentOccupancy),
      latitude: shelter.latitude == null ? "" : String(shelter.latitude),
      longitude: shelter.longitude == null ? "" : String(shelter.longitude),
    });
    setError("");
    setModalOpen(true);
  }

  function closeModal() {
    if (saving) return;
    setModalOpen(false);
    setEditing(null);
    setForm(EMPTY_FORM);
    setError("");
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    const capacity = Number(form.capacity);
    const currentOccupancy = Number(form.currentOccupancy);
    const latitude = form.latitude.trim() ? Number(form.latitude) : null;
    const longitude = form.longitude.trim() ? Number(form.longitude) : null;

    if (!form.name.trim()) {
      setError("Please enter a shelter name.");
      return;
    }

    if (!Number.isInteger(capacity) || capacity < 1) {
      setError("Capacity must be a whole number greater than zero.");
      return;
    }

    if (
      !Number.isInteger(currentOccupancy) ||
      currentOccupancy < 0 ||
      currentOccupancy > capacity
    ) {
      setError("Residents must be between zero and the total capacity.");
      return;
    }

    if ((latitude === null) !== (longitude === null)) {
      setError("Please enter both coordinates or leave both empty.");
      return;
    }

    if (
      (latitude !== null &&
        (!Number.isFinite(latitude) || latitude < -90 || latitude > 90)) ||
      (longitude !== null &&
        (!Number.isFinite(longitude) || longitude < -180 || longitude > 180))
    ) {
      setError("Please enter valid location coordinates.");
      return;
    }

    const payload = {
      name: form.name.trim(),
      capacity,
      currentOccupancy,
      latitude,
      longitude,
    };

    setSaving(true);
    setError("");

    try {
      const response = await fetch(
        editing
          ? `${API_BASE_URL}/api/shelters/${editing.id}`
          : `${API_BASE_URL}/api/shelters`,
        {
          method: editing ? "PUT" : "POST",
          headers: {
            "Content-Type": "application/json",
            Authorization: `Bearer ${token}`,
          },
          body: JSON.stringify(payload),
        }
      );

      if (response.status === 401) {
        clearSessionCredentials();
        router.replace("/login");
        return;
      }

      if (!response.ok) {
        const body = await response.json().catch(() => null);
        throw new Error(
          body?.detail ||
          body?.message ||
          `Unable to save shelter (${response.status}).`
        );
      }

      const savedId = editing?.id;
      const wasEditing = Boolean(editing);

      setModalOpen(false);
      setEditing(null);
      setForm(EMPTY_FORM);

      await loadShelters();

      if (savedId) {
        const updated = shelters.find((item) => item.id === savedId);
        if (updated) setSelected(updated);
      }

      setMessage(
        wasEditing
          ? "Shelter updated successfully."
          : "New shelter added successfully."
      );
    } catch (err) {
      setError(err instanceof Error ? err.message : "Unable to save shelter.");
    } finally {
      setSaving(false);
    }
  }

  async function handleDelete(shelter: Shelter) {
    const confirmed = window.confirm(
      `Are you sure you want to delete "${shelter.name}"?`
    );

    if (!confirmed) return;

    setDeleting(shelter.id);
    setError("");
    setMessage("");

    try {
      const response = await fetch(
        `${API_BASE_URL}/api/shelters/${shelter.id}`,
        {
          method: "DELETE",
          headers: { Authorization: `Bearer ${token}` },
        }
      );

      if (response.status === 401) {
        clearSessionCredentials();
        router.replace("/login");
        return;
      }

      if (!response.ok) {
        throw new Error(`Unable to delete shelter (${response.status}).`);
      }

      setShelters((current) =>
        current.filter((item) => item.id !== shelter.id)
      );

      setSelected((current) =>
        current?.id === shelter.id ? null : current
      );

      setDetailsOpen(false);
      setMessage("Shelter deleted successfully.");
    } catch (err) {
      setError(err instanceof Error ? err.message : "Unable to delete shelter.");
    } finally {
      setDeleting(null);
    }
  }

  function signOut() {
    clearSessionCredentials();
    router.replace("/login");
  }

  if (
    token === AUTH_SESSION_LOADING ||
    role === AUTH_SESSION_LOADING
  ) {
    return (
      <div className={styles.loadingScreen}>
        <div className={styles.loader} />
        <p>Loading your workspace...</p>
      </div>
    );
  }

  if (!token || (role !== "Admin" && role !== "ShelterAdmin")) {
    return (
      <div className={styles.loadingScreen}>
        Redirecting to your workspace...
      </div>
    );
  }

  return (
    <div className={styles.layout}>
      <aside className={styles.sidebar}>
        <Link href="/admin" className={styles.brand}>
          <Image
            src="/humanitarian-aid-platform-logo.svg"
            alt="Humanitarian Aid Platform Logo"
            width={42}
            height={42}
            className={styles.brandLogo}
            priority
          />
          <div>
            <strong>HUMANITARIAN</strong>
            <span>AID PLATFORM</span>
          </div>
        </Link>

        <p className={styles.navLabel}>WORKSPACE</p>

        <nav className={styles.navigation}>
          <Link href="/admin" className={styles.navLink}>
            <span>▦</span> Dashboard
          </Link>
          <Link
            href="/admin/shelters"
            className={`${styles.navLink} ${styles.activeLink}`}
          >
            <span>⌂</span> Shelter Management
          </Link>
          <Link href="/admin/resources" className={styles.navLink}>
            <span>▤</span> Resource Management
          </Link>
        </nav>

        <div className={styles.sidebarBottom}>
          <div className={styles.profile}>
            <div className={styles.avatar}>A</div>
            <div>
              <strong>Administrator</strong>
              <span>{role}</span>
            </div>
          </div>
          <button className={styles.logoutButton} onClick={signOut}>
            ↪ Sign out
          </button>
        </div>
      </aside>

      <main className={styles.main}>
        <header className={styles.topbar}>
          <div className={styles.breadcrumb}>
            Workspace <span>/</span> Shelter Management
          </div>
          <div className={styles.topbarRight}>
            <span className={styles.statusDot} />
            System operational
          </div>
        </header>

        <section className={styles.content}>
          <div className={styles.pageHeading}>
            <div>
              <p className={styles.eyebrow}>SHELTER OPERATIONS</p>
              <h1>Shelter Management</h1>
              <p className={styles.subtitle}>
                Manage shelter locations, capacity and resident information.
              </p>
            </div>

            <button className={styles.primaryButton} onClick={openAddModal}>
              + Add New Shelter
            </button>
          </div>

          {message && (
            <div className={styles.successMessage}>{message}</div>
          )}
          {error && (
            <div className={styles.errorMessage}>{error}</div>
          )}

          <div className={styles.statsGrid}>
            <article className={styles.statCard}>
              <div className={styles.statIcon}>⌂</div>
              <p>Total Shelters</p>
              <strong>{shelters.length}</strong>
              <span>Registered locations</span>
            </article>
            <article className={styles.statCard}>
              <div className={styles.statIcon}>♙</div>
              <p>Total Residents</p>
              <strong>{totalResidents.toLocaleString()}</strong>
              <span>Currently accommodated</span>
            </article>
            <article className={styles.statCard}>
              <div className={styles.statIcon}>▣</div>
              <p>Available Beds</p>
              <strong>{availableBeds.toLocaleString()}</strong>
              <span>Remaining capacity</span>
            </article>
            <article className={styles.statCard}>
              <div className={styles.statIcon}>◷</div>
              <p>Occupancy Rate</p>
              <strong>{occupancyRate}%</strong>
              <span>Across all shelters</span>
            </article>
          </div>

          <section className={styles.tablePanel}>
            <div className={styles.panelHeading}>
              <div>
                <h2>Shelter Inventory</h2>
                <p>View and manage registered shelter locations</p>
              </div>
              <span className={styles.countBadge}>
                {filteredShelters.length} shelters
              </span>
            </div>

            <div className={styles.toolbar}>
              <div className={styles.searchBox}>
                <span>⌕</span>
                <input
                  value={search}
                  onChange={(event) => setSearch(event.target.value)}
                  placeholder="Search shelter name..."
                  aria-label="Search shelters"
                />
              </div>

              <select
                className={styles.filterSelect}
                value={filter}
                onChange={(event) => setFilter(event.target.value)}
                aria-label="Filter shelters"
              >
                <option value="all">All shelters</option>
                <option value="available">Available</option>
                <option value="full">Full</option>
              </select>
            </div>

            {loading ? (
              <div className={styles.emptyState}>
                <div className={styles.loader} />
                <p>Loading shelters...</p>
              </div>
            ) : filteredShelters.length === 0 ? (
              <div className={styles.emptyState}>
                <div className={styles.emptyIcon}>⌂</div>
                <strong>No shelters found</strong>
                <p>Try another search or add a new shelter.</p>
              </div>
            ) : (
              <div className={styles.tableScroll}>
                <table className={styles.shelterTable}>
                  <thead>
                    <tr>
                      <th>SHELTER NAME</th>
                      <th>CAPACITY</th>
                      <th>RESIDENTS</th>
                      <th>AVAILABLE BEDS</th>
                      <th>OCCUPANCY</th>
                      <th>STATUS</th>
                      <th>ACTIONS</th>
                    </tr>
                  </thead>
                  <tbody>
                    {filteredShelters.map((shelter) => {
                      const percent = shelter.capacity > 0
                        ? Math.min(
                            100,
                            Math.round(
                              (shelter.currentOccupancy / shelter.capacity) * 100
                            )
                          )
                        : 0;
                      const full = shelter.currentOccupancy >= shelter.capacity;
                      const available = Math.max(
                        0,
                        shelter.capacity - shelter.currentOccupancy
                      );

                      return (
                        <tr key={shelter.id}>
                          <td>
                            <div className={styles.shelterNameCell}>
                              <span className={styles.shelterIcon}>⌂</span>
                              <strong>{shelter.name}</strong>
                            </div>
                          </td>
                          <td>{shelter.capacity}</td>
                          <td>{shelter.currentOccupancy}</td>
                          <td>{available}</td>
                          <td>
                            <div className={styles.occupancyCell}>
                              <span>{percent}%</span>
                              <div className={styles.progressTrack}>
                                <div
                                  className={`${styles.progressFill} ${
                                    full ? styles.fullProgress : ""
                                  }`}
                                  style={{ width: `${percent}%` }}
                                />
                              </div>
                            </div>
                          </td>
                          <td>
                            <span
                              className={`${styles.statusBadge} ${
                                full ? styles.fullBadge : styles.availableBadge
                              }`}
                            >
                              {full ? "Full" : "Available"}
                            </span>
                          </td>
                          <td>
                            <div className={styles.actionButtons}>
                              <button
                                className={styles.viewButton}
                                onClick={() => {
                                  setSelected(shelter);
                                  setDetailsOpen(true);
                                }}
                              >
                                View
                              </button>
                              <button
                                className={styles.editButton}
                                onClick={() => openEditModal(shelter)}
                              >
                                Edit
                              </button>
                              <button
                                className={styles.deleteButton}
                                disabled={deleting === shelter.id}
                                onClick={() => void handleDelete(shelter)}
                              >
                                {deleting === shelter.id ? "..." : "Delete"}
                              </button>
                            </div>
                          </td>
                        </tr>
                      );
                    })}
                  </tbody>
                </table>
              </div>
            )}
          </section>
        </section>
      </main>

      {modalOpen && (
        <div className={styles.modalBackdrop}>
          <section
            className={styles.modal}
            role="dialog"
            aria-modal="true"
            aria-labelledby="shelter-modal-title"
          >
            <div className={styles.modalHeading}>
              <div>
                <p className={styles.eyebrow}>SHELTER INFORMATION</p>
                <h2 id="shelter-modal-title">
                  {editing ? "Edit Shelter Details" : "Add New Shelter"}
                </h2>
              </div>
              <button
                type="button"
                className={styles.closeButton}
                onClick={closeModal}
                aria-label="Close dialog"
              >
                ×
              </button>
            </div>

            <form onSubmit={handleSubmit}>
              <label className={styles.formLabel}>
                Shelter name
                <input
                  required
                  value={form.name}
                  onChange={(event) =>
                    setForm({ ...form, name: event.target.value })
                  }
                  placeholder="Enter shelter name"
                />
              </label>

              <div className={styles.formRow}>
                <label className={styles.formLabel}>
                  Total capacity
                  <input
                    required
                    type="number"
                    min="1"
                    step="1"
                    value={form.capacity}
                    onChange={(event) =>
                      setForm({ ...form, capacity: event.target.value })
                    }
                    placeholder="Enter capacity"
                  />
                </label>
                <label className={styles.formLabel}>
                  Current residents
                  <input
                    required
                    type="number"
                    min="0"
                    step="1"
                    value={form.currentOccupancy}
                    onChange={(event) =>
                      setForm({
                        ...form,
                        currentOccupancy: event.target.value,
                      })
                    }
                  />
                </label>
              </div>

              <div className={styles.formRow}>
                <label className={styles.formLabel}>
                  Latitude <span>(optional)</span>
                  <input
                    type="number"
                    step="any"
                    value={form.latitude}
                    onChange={(event) =>
                      setForm({ ...form, latitude: event.target.value })
                    }
                    placeholder="e.g. 6.9271"
                  />
                </label>
                <label className={styles.formLabel}>
                  Longitude <span>(optional)</span>
                  <input
                    type="number"
                    step="any"
                    value={form.longitude}
                    onChange={(event) =>
                      setForm({ ...form, longitude: event.target.value })
                    }
                    placeholder="e.g. 79.8612"
                  />
                </label>
              </div>

              {error && (
                <div className={styles.errorMessage}>{error}</div>
              )}

              <div className={styles.modalActions}>
                <button
                  type="button"
                  className={styles.cancelButton}
                  onClick={closeModal}
                  disabled={saving}
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  className={styles.primaryButton}
                  disabled={saving}
                >
                  {saving
                    ? "Saving..."
                    : editing
                      ? "Save Changes"
                      : "Create Shelter"}
                </button>
              </div>
            </form>
          </section>
        </div>
      )}

      {detailsOpen && selected && (
        <div
          className={styles.modalBackdrop}
          onMouseDown={(event) => {
            if (event.target === event.currentTarget) {
              setDetailsOpen(false);
            }
          }}
        >
          <section
            className={styles.modal}
            role="dialog"
            aria-modal="true"
            aria-labelledby="shelter-details-title"
          >
            <div className={styles.modalHeading}>
              <div>
                <p className={styles.eyebrow}>SHELTER INFORMATION</p>
                <h2 id="shelter-details-title">View Shelter Details</h2>
              </div>
              <button
                className={styles.closeButton}
                onClick={() => setDetailsOpen(false)}
                aria-label="Close dialog"
              >
                ×
              </button>
            </div>

            <div className={styles.detailsContent}>
              <div className={styles.detailsIcon}>⌂</div>
              <h3>{selected.name}</h3>
              <span
                className={`${styles.statusBadge} ${
                  selected.currentOccupancy >= selected.capacity
                    ? styles.fullBadge
                    : styles.availableBadge
                }`}
              >
                {selected.currentOccupancy >= selected.capacity
                  ? "Full"
                  : "Available"}
              </span>

              <div className={styles.detailsGrid}>
                <div>
                  <span>Total capacity</span>
                  <strong>{selected.capacity}</strong>
                </div>
                <div>
                  <span>Current residents</span>
                  <strong>{selected.currentOccupancy}</strong>
                </div>
                <div>
                  <span>Available beds</span>
                  <strong>
                    {Math.max(0, selected.capacity - selected.currentOccupancy)}
                  </strong>
                </div>
              </div>

              <div className={styles.locationInfo}>
                <strong>Location coordinates</strong>
                <p>Latitude: {selected.latitude ?? "Not provided"}</p>
                <p>Longitude: {selected.longitude ?? "Not provided"}</p>
              </div>
            </div>

            <div className={styles.modalActions}>
              <button
                className={styles.cancelButton}
                onClick={() => setDetailsOpen(false)}
              >
                Close
              </button>
              <button
                className={styles.primaryButton}
                onClick={() => {
                  setDetailsOpen(false);
                  openEditModal(selected);
                }}
              >
                Edit Shelter
              </button>
            </div>
          </section>
        </div>
      )}
    </div>
  );
}
