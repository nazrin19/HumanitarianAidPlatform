
"use client";

import {
  useCallback,
  useEffect,
  useMemo,
  useState,
  type FormEvent,
} from "react";
import Image from "next/image";
import Link from "next/link";
import { useRouter } from "next/navigation";
import styles from "./page.module.css";
import {
  AUTH_SESSION_LOADING,
  clearSessionCredentials,
  useSessionRole,
  useSessionToken,
} from "../../lib/auth-session";

type Resource = {
  id: string;
  name: string;
  category: string;
  quantityAvailable: number;
  description?: string | null;
};

type ResourceForm = {
  name: string;
  category: string;
  quantityAvailable: string;
  description: string;
};

const API_BASE_URL = (
  process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5001"
).replace(/\/$/, "");

const EMPTY_FORM: ResourceForm = {
  name: "",
  category: "Food",
  quantityAvailable: "0",
  description: "",
};

export default function ResourceManagementPage() {
  const router = useRouter();
  const token = useSessionToken();
  const role = useSessionRole();

  const [resources, setResources] = useState<Resource[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [message, setMessage] = useState("");
  const [search, setSearch] = useState("");
  const [filter, setFilter] = useState("all");

  const [modalOpen, setModalOpen] = useState(false);
  const [detailsOpen, setDetailsOpen] = useState(false);
  const [editing, setEditing] = useState<Resource | null>(null);
  const [selected, setSelected] = useState<Resource | null>(null);
  const [form, setForm] = useState<ResourceForm>(EMPTY_FORM);
  const [saving, setSaving] = useState(false);
  const [deleting, setDeleting] = useState<string | null>(null);

  const loadResources = useCallback(async () => {
    if (!token || token === AUTH_SESSION_LOADING) return;

    setLoading(true);
    setError("");

    try {
      const response = await fetch(`${API_BASE_URL}/api/resources`, {
        headers: {
          Authorization: `Bearer ${token}`,
        },
        cache: "no-store",
      });

      if (response.status === 401) {
        clearSessionCredentials();
        router.replace("/login");
        return;
      }

      if (!response.ok) {
        throw new Error(`Unable to load resources (${response.status}).`);
      }

      const data: unknown = await response.json();

      const list = Array.isArray(data)
        ? data
        : data &&
            typeof data === "object" &&
            "items" in data &&
            Array.isArray((data as { items: unknown }).items)
          ? (data as { items: Resource[] }).items
          : data &&
              typeof data === "object" &&
              "resources" in data &&
              Array.isArray((data as { resources: unknown }).resources)
            ? (data as { resources: Resource[] }).resources
            : null;

      if (!Array.isArray(list)) {
        throw new Error("Unexpected response received from the server.");
      }

      setResources(list as Resource[]);
    } catch (err) {
      setError(
        err instanceof Error
          ? err.message
          : "Unable to connect to the server."
      );
    } finally {
      setLoading(false);
    }
  }, [token, router]);

  useEffect(() => {
    if (
      token === AUTH_SESSION_LOADING ||
      role === AUTH_SESSION_LOADING
    ) {
      return;
    }

    if (!token) {
      router.replace("/login");
      return;
    }

    if (role !== "Admin" && role !== "ResourceManager") {
      router.replace("/portal");
      return;
    }

    void loadResources();
  }, [token, role, router, loadResources]);

  const filteredResources = useMemo(() => {
    const term = search.trim().toLowerCase();

    return resources.filter((resource) => {
      const matchesSearch =
        resource.name.toLowerCase().includes(term) ||
        resource.category.toLowerCase().includes(term) ||
        (resource.description ?? "").toLowerCase().includes(term);

      const quantity = Number(resource.quantityAvailable) || 0;

      const matchesFilter =
        filter === "all" ||
        (filter === "available" && quantity > 10) ||
        (filter === "low" && quantity > 0 && quantity <= 10) ||
        (filter === "out" && quantity === 0);

      return matchesSearch && matchesFilter;
    });
  }, [resources, search, filter]);

  const totalQuantity = resources.reduce(
    (sum, resource) =>
      sum + (Number(resource.quantityAvailable) || 0),
    0
  );

  const lowStock = resources.filter((resource) => {
    const quantity = Number(resource.quantityAvailable) || 0;
    return quantity > 0 && quantity <= 10;
  }).length;

  const outOfStock = resources.filter(
    (resource) => Number(resource.quantityAvailable) === 0
  ).length;

  function openAddModal() {
    setEditing(null);
    setForm(EMPTY_FORM);
    setError("");
    setMessage("");
    setModalOpen(true);
  }

  function openEditModal(resource: Resource) {
    setEditing(resource);
    setForm({
      name: resource.name,
      category: resource.category,
      quantityAvailable: String(resource.quantityAvailable),
      description: resource.description ?? "",
    });
    setError("");
    setMessage("");
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

    const quantity = Number(form.quantityAvailable);

    if (!form.name.trim()) {
      setError("Please enter a resource name.");
      return;
    }

    if (!Number.isInteger(quantity) || quantity < 0) {
      setError("Quantity must be a whole number greater than or equal to zero.");
      return;
    }

    setSaving(true);
    setError("");
    setMessage("");

    const isEditing = Boolean(editing);

    const payload = {
      name: form.name.trim(),
      category: form.category,
      quantityAvailable: quantity,
      description: form.description.trim(),
    };

    try {
      const response = await fetch(
        editing
          ? `${API_BASE_URL}/api/resources/${editing.id}`
          : `${API_BASE_URL}/api/resources`,
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
            `Unable to save resource (${response.status}).`
        );
      }

      setModalOpen(false);
      setEditing(null);
      setForm(EMPTY_FORM);

      await loadResources();

      setMessage(
        isEditing
          ? "Resource updated successfully."
          : "New resource added successfully."
      );
    } catch (err) {
      setError(
        err instanceof Error ? err.message : "Unable to save resource."
      );
    } finally {
      setSaving(false);
    }
  }

  async function handleDelete(resource: Resource) {
    const confirmed = window.confirm(
      `Are you sure you want to delete "${resource.name}"?`
    );

    if (!confirmed) return;

    setDeleting(resource.id);
    setError("");
    setMessage("");

    try {
      const response = await fetch(
        `${API_BASE_URL}/api/resources/${resource.id}`,
        {
          method: "DELETE",
          headers: {
            Authorization: `Bearer ${token}`,
          },
        }
      );

      if (response.status === 401) {
        clearSessionCredentials();
        router.replace("/login");
        return;
      }

      if (!response.ok) {
        throw new Error(`Unable to delete resource (${response.status}).`);
      }

      setResources((current) =>
        current.filter((item) => item.id !== resource.id)
      );

      setSelected((current) =>
        current?.id === resource.id ? null : current
      );

      setDetailsOpen(false);
      setMessage("Resource deleted successfully.");
    } catch (err) {
      setError(
        err instanceof Error ? err.message : "Unable to delete resource."
      );
    } finally {
      setDeleting(null);
    }
  }

  function stockLabel(quantity: number) {
    if (quantity === 0) return "Out of Stock";
    if (quantity <= 10) return "Low Stock";
    return "Available";
  }

  function stockClass(quantity: number) {
    if (quantity === 0) return styles.outOfStockBadge;
    if (quantity <= 10) return styles.lowStockBadge;
    return styles.availableBadge;
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

  if (
    !token ||
    (role !== "Admin" && role !== "ResourceManager")
  ) {
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

          <Link href="/admin/shelters" className={styles.navLink}>
            <span>⌂</span> Shelter Management
          </Link>

          <Link
            href="/admin/resources"
            className={`${styles.navLink} ${styles.activeLink}`}
          >
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
            Workspace <span>/</span> Resource Management
          </div>

          <div className={styles.topbarRight}>
            <span className={styles.systemDot} />
            System operational
          </div>
        </header>

        <section className={styles.content}>
          <div className={styles.pageHeading}>
            <div>
              <p className={styles.eyebrow}>RESOURCE OPERATIONS</p>
              <h1>Resource Management</h1>
              <p className={styles.subtitle}>
                Monitor and manage essential resources available for
                humanitarian assistance.
              </p>
            </div>

            <button
              className={styles.primaryButton}
              onClick={openAddModal}
            >
              + Add New Resource
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
              <div className={styles.statIcon}>▤</div>
              <p>Resource Types</p>
              <strong>{resources.length}</strong>
              <span>Registered resources</span>
            </article>

            <article className={styles.statCard}>
              <div className={styles.statIcon}>▣</div>
              <p>Total Quantity</p>
              <strong>{totalQuantity.toLocaleString()}</strong>
              <span>Available units</span>
            </article>

            <article className={styles.statCard}>
              <div className={styles.statIcon}>!</div>
              <p>Low Stock</p>
              <strong>{lowStock}</strong>
              <span>10 units or fewer</span>
            </article>

            <article className={styles.statCard}>
              <div className={styles.statIcon}>○</div>
              <p>Out of Stock</p>
              <strong>{outOfStock}</strong>
              <span>Items needing restock</span>
            </article>
          </div>

          <section className={styles.tablePanel}>
            <div className={styles.panelHeading}>
              <div>
                <h2>Resource Inventory</h2>
                <p>View and manage your registered resources</p>
              </div>

              <span className={styles.countBadge}>
                {filteredResources.length} resources
              </span>
            </div>

            <div className={styles.toolbar}>
              <div className={styles.searchBox}>
                <span>⌕</span>
                <input
                  value={search}
                  onChange={(event) => setSearch(event.target.value)}
                  placeholder="Search resource name..."
                  aria-label="Search resources"
                />
              </div>

              <select
                className={styles.filterSelect}
                value={filter}
                onChange={(event) => setFilter(event.target.value)}
                aria-label="Filter resources"
              >
                <option value="all">All resources</option>
                <option value="available">Available</option>
                <option value="low">Low stock</option>
                <option value="out">Out of stock</option>
              </select>
            </div>

            {loading ? (
              <div className={styles.emptyState}>
                <div className={styles.loader} />
                <p>Loading resources...</p>
              </div>
            ) : filteredResources.length === 0 ? (
              <div className={styles.emptyState}>
                <div className={styles.emptyIcon}>▤</div>
                <strong>No resources found</strong>
                <p>Try another search or add a new resource.</p>
              </div>
            ) : (
              <div className={styles.tableScroll}>
                <table className={styles.resourceTable}>
                  <thead>
                    <tr>
                      <th>RESOURCE NAME</th>
                      <th>CATEGORY</th>
                      <th>AVAILABLE QUANTITY</th>
                      <th>STOCK STATUS</th>
                      <th>ACTIONS</th>
                    </tr>
                  </thead>

                  <tbody>
                    {filteredResources.map((resource) => {
                      const quantity =
                        Number(resource.quantityAvailable) || 0;

                      return (
                        <tr key={resource.id}>
                          <td>
                            <div className={styles.resourceNameCell}>
                              <span className={styles.resourceIcon}>▤</span>
                              <strong>{resource.name}</strong>
                            </div>
                          </td>

                          <td>
                            <span className={styles.categoryBadge}>
                              {resource.category}
                            </span>
                          </td>

                          <td>
                            <strong className={styles.quantityValue}>
                              {quantity.toLocaleString()}
                            </strong>
                            <span className={styles.quantityUnit}> units</span>
                          </td>

                          <td>
                            <span
                              className={`${styles.statusBadge} ${stockClass(
                                quantity
                              )}`}
                            >
                              {stockLabel(quantity)}
                            </span>
                          </td>

                          <td>
                            <div className={styles.actionButtons}>
                              <button
                                className={styles.viewButton}
                                onClick={() => {
                                  setSelected(resource);
                                  setDetailsOpen(true);
                                }}
                              >
                                View
                              </button>

                              <button
                                className={styles.editButton}
                                onClick={() => openEditModal(resource)}
                              >
                                Edit
                              </button>

                              <button
                                className={styles.deleteButton}
                                disabled={deleting === resource.id}
                                onClick={() => void handleDelete(resource)}
                              >
                                {deleting === resource.id ? "..." : "Delete"}
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
        <div
          className={styles.modalBackdrop}
          onMouseDown={(event) => {
            if (event.target === event.currentTarget) {
              closeModal();
            }
          }}
        >
          <section
            className={styles.modal}
            role="dialog"
            aria-modal="true"
            aria-labelledby="resource-modal-title"
          >
            <div className={styles.modalHeading}>
              <div>
                <p className={styles.eyebrow}>RESOURCE INFORMATION</p>
                <h2 id="resource-modal-title">
                  {editing ? "Edit Resource Details" : "Add New Resource"}
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
                Resource name
                <input
                  required
                  value={form.name}
                  onChange={(event) =>
                    setForm({ ...form, name: event.target.value })
                  }
                  placeholder="Enter resource name"
                />
              </label>

              <div className={styles.formRow}>
                <label className={styles.formLabel}>
                  Category
                  <select
                    value={form.category}
                    onChange={(event) =>
                      setForm({ ...form, category: event.target.value })
                    }
                  >
                    <option>Food</option>
                    <option>Water</option>
                    <option>Medical</option>
                    <option>Clothing</option>
                    <option>Hygiene</option>
                    <option>Other</option>
                  </select>
                </label>

                <label className={styles.formLabel}>
                  Available quantity
                  <input
                    required
                    type="number"
                    min="0"
                    step="1"
                    value={form.quantityAvailable}
                    onChange={(event) =>
                      setForm({
                        ...form,
                        quantityAvailable: event.target.value,
                      })
                    }
                    placeholder="Enter quantity"
                  />
                </label>
              </div>

              <label className={styles.formLabel}>
                Description <span>(optional)</span>
                <textarea
                  rows={3}
                  value={form.description}
                  onChange={(event) =>
                    setForm({ ...form, description: event.target.value })
                  }
                  placeholder="Enter resource description"
                />
              </label>

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
                      : "Create Resource"}
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
            aria-labelledby="resource-details-title"
          >
            <div className={styles.modalHeading}>
              <div>
                <p className={styles.eyebrow}>RESOURCE INFORMATION</p>
                <h2 id="resource-details-title">View Resource Details</h2>
              </div>

              <button
                type="button"
                className={styles.closeButton}
                onClick={() => setDetailsOpen(false)}
                aria-label="Close dialog"
              >
                ×
              </button>
            </div>

            <div className={styles.detailsContent}>
              <div className={styles.detailsIcon}>▤</div>

              <h3>{selected.name}</h3>

              <span
                className={`${styles.statusBadge} ${stockClass(
                  Number(selected.quantityAvailable) || 0
                )}`}
              >
                {stockLabel(Number(selected.quantityAvailable) || 0)}
              </span>

              <div className={styles.detailsGrid}>
                <div>
                  <span>Category</span>
                  <strong>{selected.category}</strong>
                </div>

                <div>
                  <span>Available quantity</span>
                  <strong>
                    {Number(selected.quantityAvailable).toLocaleString()}
                  </strong>
                </div>
              </div>

              <div className={styles.descriptionInfo}>
                <strong>Description</strong>
                <p>{selected.description || "No description provided."}</p>
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
                Edit Resource
              </button>
            </div>
          </section>
        </div>
      )}
    </div>
  );
}
