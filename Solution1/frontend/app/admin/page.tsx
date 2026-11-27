
"use client";

import Image from "next/image";
import { FormEvent, useCallback, useEffect, useMemo, useState } from "react";
import { useRouter } from "next/navigation";
import {
  AUTH_SESSION_LOADING,
  clearSessionCredentials,
  useSessionRole,
  useSessionToken,
} from "../lib/auth-session";
import styles from "./page.module.css";

type PendingApplication = {
  id: string;
  fullName: string;
  email: string;
  accountType: string;
  phone: string | null;
  skills: string | null;
  donorType: string | null;
  createdAt: string;
};

const API_BASE_URL = (
  process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5001"
).replace(/\/+$/, "");

const STAFF_ROLES = [
  "Coordinator",
  "CaseManager",
  "ResourceManager",
  "ShelterAdmin",
  "VolunteerCoordinator",
  "DonorOfficer",
  "PartnerOfficer",
] as const;

function getErrorMessage(body: unknown, fallback: string) {
  if (body && typeof body === "object") {
    const error = body as {
      detail?: unknown;
      message?: unknown;
      title?: unknown;
    };

    const message = error.detail ?? error.message ?? error.title;

    if (typeof message === "string" && message) return message;
  }

  return fallback;
}

function isPendingApplication(
  value: unknown
): value is PendingApplication {
  if (!value || typeof value !== "object") return false;

  const application = value as Record<string, unknown>;

  return (
    typeof application.id === "string" &&
    typeof application.fullName === "string" &&
    typeof application.email === "string" &&
    typeof application.accountType === "string" &&
    (application.phone === null || typeof application.phone === "string") &&
    (application.skills === null || typeof application.skills === "string") &&
    (application.donorType === null || typeof application.donorType === "string") &&
    typeof application.createdAt === "string"
  );
}

async function loadPendingApplications(
  token: string
): Promise<PendingApplication[]> {
  const response = await fetch(
    `${API_BASE_URL}/api/admin/users/pending-applications`,
    {
      headers: {
        Authorization: `Bearer ${token}`,
      },
    }
  );

  const body: unknown = await response.json().catch(() => undefined);

  if (!response.ok) {
    throw new Error(
      getErrorMessage(
        body,
        `Unable to load applications (${response.status}).`
      )
    );
  }

  if (!Array.isArray(body) || !body.every(isPendingApplication)) {
    throw new Error(
      "The server returned an invalid applications response."
    );
  }

  return body;
}

function initials(name: string) {
  return name
    .split(/\s+/)
    .filter(Boolean)
    .slice(0, 2)
    .map((part) => part[0]?.toUpperCase())
    .join("");
}

function formatDate(value: string) {
  const date = new Date(value);

  return Number.isNaN(date.getTime())
    ? "Unknown"
    : date.toLocaleDateString();
}

export default function AdminPage() {
  const router = useRouter();
  const token = useSessionToken();
  const accountRole = useSessionRole();

  const [applications, setApplications] = useState<PendingApplication[]>([]);
  const [createdStaff, setCreatedStaff] = useState<PendingApplication[]>([]);
  const [error, setError] = useState("");
  const [notice, setNotice] = useState("");
  const [busyId, setBusyId] = useState("");
  const [isLoading, setIsLoading] = useState(true);

  const [fullName, setFullName] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [role, setRole] = useState<(typeof STAFF_ROLES)[number]>(
    STAFF_ROLES[0]
  );

  const [isCreating, setIsCreating] = useState(false);
  const [query, setQuery] = useState("");
  const [mobileMenuOpen, setMobileMenuOpen] = useState(false);

  const refresh = useCallback(async (accessToken: string) => {
    setError("");
    setIsLoading(true);

    try {
      setApplications(await loadPendingApplications(accessToken));
    } catch (loadError) {
      setError(
        loadError instanceof Error
          ? loadError.message
          : "Unable to load applications."
      );
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    if (
      token === AUTH_SESSION_LOADING ||
      accountRole === AUTH_SESSION_LOADING
    ) {
      return;
    }

    if (!token || accountRole !== "Admin") {
      router.replace(accountRole ? "/portal" : "/login");
      return;
    }

    let active = true;

    loadPendingApplications(token)
      .then((pendingApplications) => {
        if (active) {
          setApplications(pendingApplications);
          setError("");
        }
      })
      .catch((loadError: unknown) => {
        if (active) {
          setError(
            loadError instanceof Error
              ? loadError.message
              : "Unable to load applications."
          );
        }
      })
      .finally(() => {
        if (active) setIsLoading(false);
      });

    return () => {
      active = false;
    };
  }, [accountRole, router, token]);

  const filteredApplications = useMemo(() => {
    const normalized = query.trim().toLowerCase();

    if (!normalized) return applications;

    return applications.filter((application) =>
      `${application.fullName} ${application.email} ${application.accountType}`
        .toLowerCase()
        .includes(normalized)
    );
  }, [applications, query]);

  async function decide(
    application: PendingApplication,
    decision: "approve" | "reject"
  ) {
    if (!token || token === AUTH_SESSION_LOADING) return;

    setBusyId(application.id);
    setError("");
    setNotice("");

    try {
      const response = await fetch(
        `${API_BASE_URL}/api/admin/users/${encodeURIComponent(application.id)}/${decision}`,
        {
          method: "POST",
          headers: {
            Authorization: `Bearer ${token}`,
          },
        }
      );

      if (!response.ok) {
        const body: unknown = await response.json().catch(() => undefined);

        throw new Error(
          getErrorMessage(
            body,
            `Unable to ${decision} this application (${response.status}).`
          )
        );
      }

      setNotice(
        `${application.fullName}'s application was ${
          decision === "approve" ? "approved" : "rejected"
        }.`
      );

      await refresh(token);
    } catch (actionError) {
      setError(
        actionError instanceof Error
          ? actionError.message
          : `Unable to ${decision} this application.`
      );
    } finally {
      setBusyId("");
    }
  }

  async function createStaffAccount(
    event: FormEvent<HTMLFormElement>
  ) {
    event.preventDefault();

    if (!token || token === AUTH_SESSION_LOADING) return;

    setError("");
    setNotice("");

    if (password.length < 12) {
      setError("Staff passwords must be at least 12 characters.");
      return;
    }

    setIsCreating(true);

    try {
      const response = await fetch(
        `${API_BASE_URL}/api/admin/users/staff`,
        {
          method: "POST",
          headers: {
            Authorization: `Bearer ${token}`,
            "Content-Type": "application/json",
          },
          body: JSON.stringify({
            fullName: fullName.trim(),
            email: email.trim(),
            password,
            role,
          }),
        }
      );

      const body: unknown = await response.json().catch(() => undefined);

      if (!response.ok) {
        throw new Error(
          getErrorMessage(
            body,
            `Unable to create staff account (${response.status}).`
          )
        );
      }

      const created =
        body && typeof body === "object"
          ? (body as Record<string, unknown>)
          : {};

      setCreatedStaff((current) =>
        [
          {
            id:
              typeof created.id === "string"
                ? created.id
                : `${email}-${Date.now()}`,
            fullName:
              typeof created.fullName === "string"
                ? created.fullName
                : fullName.trim(),
            email:
              typeof created.email === "string"
                ? created.email
                : email.trim(),
            accountType:
              typeof created.role === "string" ? created.role : role,
            phone: null,
            skills: null,
            donorType: null,
            createdAt: new Date().toISOString(),
          },
          ...current,
        ].slice(0, 5)
      );

      setNotice(`Staff account created for ${email.trim()}.`);

      setFullName("");
      setEmail("");
      setPassword("");
    } catch (createError) {
      setError(
        createError instanceof Error
          ? createError.message
          : "Unable to create staff account."
      );
    } finally {
      setIsCreating(false);
    }
  }

  function signOut() {
    clearSessionCredentials();
    router.replace("/login");
  }

  if (
    token === AUTH_SESSION_LOADING ||
    accountRole === AUTH_SESSION_LOADING
  ) {
    return (
      <main className={styles.statusPage} role="status">
        <span className={styles.loader} />
        Checking administrator access...
      </main>
    );
  }

  if (!token || accountRole !== "Admin") {
    return (
      <main className={styles.statusPage} role="status">
        Redirecting...
      </main>
    );
  }

  return (
    <div className={styles.page}>
      {mobileMenuOpen && (
        <button
          className={styles.mobileOverlay}
          type="button"
          aria-label="Close admin menu"
          onClick={() => setMobileMenuOpen(false)}
        />
      )}

      {/* SIDEBAR */}
      <aside
        className={`${styles.sidebar} ${
          mobileMenuOpen ? styles.sidebarOpen : ""
        }`}
      >
        <button
          className={styles.brand}
          type="button"
          onClick={() =>
            window.scrollTo({ top: 0, behavior: "smooth" })
          }
        >
          <Image
            src="/humanitarian-aid-platform-logo.svg"
            alt=""
            width={39}
            height={39}
          />

          <span>
            <strong>HUMANITARIAN</strong>
            <small>AID PLATFORM</small>
          </span>
        </button>

        <p className={styles.navLabel}>ADMINISTRATION</p>

        <nav className={styles.sideNav} aria-label="Admin navigation">
          <a
            className={styles.activeNav}
            href="#top"
            onClick={() => setMobileMenuOpen(false)}
          >
            <span>⌂</span>
            Overview
          </a>

          <a
            href="#applications"
            onClick={() => setMobileMenuOpen(false)}
          >
            <span>✓</span>
            Applications

            {applications.length > 0 && (
              <b>{applications.length}</b>
            )}
          </a>

          <a
            href="#staff"
            onClick={() => setMobileMenuOpen(false)}
          >
            <span>♙</span>
            Create Staff
          </a>
        </nav>

        <div className={styles.sidebarFooter}>
          <span className={styles.avatar}>SA</span>

          <div>
            <strong>System Admin</strong>
            <small>Administrator</small>
          </div>

          <button
            className={styles.logoutButton}
            type="button"
            onClick={signOut}
          >
            ↗ Sign out
          </button>
        </div>
      </aside>

      {/* MAIN CONTENT */}
      <main className={styles.main} id="top">
        <header className={styles.topbar}>
          <button
            className={styles.menuButton}
            type="button"
            aria-label={
              mobileMenuOpen ? "Close admin menu" : "Open admin menu"
            }
            aria-expanded={mobileMenuOpen}
            onClick={() => setMobileMenuOpen((open) => !open)}
          >
            {mobileMenuOpen ? "×" : "☰"}
          </button>

          <div className={styles.breadcrumb}>
            Admin <span>/</span> Overview
          </div>

          <div className={styles.topbarRight}>
            <span className={styles.statusDot} />
            Administrator access
          </div>
        </header>

        <div className={styles.content}>
          {/* PAGE HEADING */}
          <section className={styles.pageHeading}>
            <div>
              <p className={styles.eyebrow}>
                HUMANITARIAN AID PLATFORM / ADMIN
              </p>

              <h1>Administrator Dashboard</h1>

              <p className={styles.subtitle}>
                Review applications and manage platform staff access.
              </p>
            </div>

            <button
              className={styles.secondaryButton}
              type="button"
              disabled={isLoading}
              onClick={() => void refresh(token)}
            >
              ↻ Refresh
            </button>
          </section>

          {/* NOTIFICATIONS */}
          {error && (
            <p className={styles.errorMessage} role="alert">
              {error}
            </p>
          )}

          {notice && (
            <p className={styles.successMessage} role="status">
              {notice}
            </p>
          )}

          {/* STATISTICS */}
          <section
            className={styles.statsGrid}
            aria-label="Administrator summary"
          >
            <article className={styles.statCard}>
              <span className={styles.statIcon}>✓</span>
              <p>Pending Applications</p>
              <strong>{isLoading ? "..." : applications.length}</strong>
              <span>Applications awaiting review</span>
            </article>

            <article className={styles.statCard}>
              <span className={styles.statIcon}>♙</span>
              <p>Recently Created Staff</p>
              <strong>{createdStaff.length}</strong>
              <span>Created in this browser session</span>
            </article>

            <article className={styles.statCard}>
              <span className={styles.statIcon}>◆</span>
              <p>Administrator Role</p>
              <strong>Admin</strong>
              <span>System management access</span>
            </article>
          </section>

          {/* PENDING APPLICATIONS */}
          <section
            className={styles.tablePanel}
            id="applications"
            aria-labelledby="applications-heading"
          >
            <div className={styles.panelHeading}>
              <div>
                <h2 id="applications-heading">
                  Pending Applications
                </h2>

                <p>
                  Review volunteer and donor accounts awaiting approval.
                </p>
              </div>

              <span className={styles.countBadge}>
                {applications.length} Pending
              </span>
            </div>

            <div className={styles.toolbar}>
              <label className={styles.searchBox}>
                <span>⌕</span>

                <input
                  type="search"
                  placeholder="Search by name, email or account type..."
                  value={query}
                  onChange={(event) => setQuery(event.target.value)}
                />
              </label>
            </div>

            {isLoading ? (
              <div className={styles.emptyState}>
                <span className={styles.loader} />
                <strong>Loading applications...</strong>
              </div>
            ) : filteredApplications.length === 0 ? (
              <div className={styles.emptyState}>
                <span className={styles.emptyIcon}>✓</span>

                <strong>No pending applications</strong>

                <p>
                  There are no applications matching your search.
                </p>
              </div>
            ) : (
              <div className={styles.tableScroll}>
                <table className={styles.applicationsTable}>
                  <thead>
                    <tr>
                      <th>Applicant</th>
                      <th>Application</th>
                      <th>Profile</th>
                      <th>Applied Date</th>
                      <th>Actions</th>
                    </tr>
                  </thead>

                  <tbody>
                    {filteredApplications.map((application) => (
                      <tr key={application.id}>
                        <td>
                          <div className={styles.applicantCell}>
                            <span className={styles.applicantAvatar}>
                              {initials(application.fullName)}
                            </span>

                            <div className={styles.applicantInfo}>
                              <strong>{application.fullName}</strong>
                              <small>{application.email}</small>

                              {application.phone && (
                                <small>{application.phone}</small>
                              )}
                            </div>
                          </div>
                        </td>

                        <td>
                          <span
                            className={
                              application.accountType === "Volunteer"
                                ? `${styles.applicationTag} ${styles.volunteerTag}`
                                : `${styles.applicationTag} ${styles.donorTag}`
                            }
                          >
                            {application.accountType}
                          </span>
                        </td>

                        <td>
                          <div className={styles.profileDetail}>
                            {application.skills && (
                              <span>{application.skills}</span>
                            )}

                            {application.donorType && (
                              <span>{application.donorType}</span>
                            )}

                            {!application.skills &&
                              !application.donorType && (
                                <span>Profile submitted</span>
                              )}
                          </div>
                        </td>

                        <td>{formatDate(application.createdAt)}</td>

                        <td>
                          <div className={styles.rowActions}>
                            <button
                              className={styles.approveButton}
                              type="button"
                              disabled={busyId === application.id}
                              onClick={() =>
                                void decide(application, "approve")
                              }
                            >
                              {busyId === application.id
                                ? "Please wait..."
                                : "Approve"}
                            </button>

                            <button
                              className={styles.rejectButton}
                              type="button"
                              disabled={busyId === application.id}
                              onClick={() =>
                                void decide(application, "reject")
                              }
                            >
                              Reject
                            </button>
                          </div>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </section>

          {/* STAFF MANAGEMENT */}
          <div className={styles.lowerGrid}>
            <section
              className={styles.tablePanel}
              id="staff"
              aria-labelledby="staff-heading"
            >
              <div className={styles.panelHeading}>
                <div>
                  <h2 id="staff-heading">Create Staff Account</h2>

                  <p>
                    Create an active staff account with a platform role.
                  </p>
                </div>
              </div>

              <form
                className={styles.staffForm}
                onSubmit={createStaffAccount}
              >
                <label className={styles.formLabel}>
                  Full Name

                  <input
                    required
                    maxLength={100}
                    placeholder="Enter staff member name"
                    value={fullName}
                    onChange={(event) => setFullName(event.target.value)}
                  />
                </label>

                <label className={styles.formLabel}>
                  Work Email

                  <input
                    required
                    type="email"
                    maxLength={256}
                    placeholder="staff@example.com"
                    value={email}
                    onChange={(event) => setEmail(event.target.value)}
                  />
                </label>

                <label className={styles.formLabel}>
                  Password

                  <input
                    required
                    type="password"
                    minLength={12}
                    maxLength={100}
                    autoComplete="new-password"
                    placeholder="At least 12 characters"
                    value={password}
                    onChange={(event) => setPassword(event.target.value)}
                  />
                </label>

                <label className={styles.formLabel}>
                  Staff Role

                  <select
                    value={role}
                    onChange={(event) =>
                      setRole(
                        event.target.value as (typeof STAFF_ROLES)[number]
                      )
                    }
                  >
                    {STAFF_ROLES.map((staffRole) => (
                      <option key={staffRole} value={staffRole}>
                        {staffRole}
                      </option>
                    ))}
                  </select>
                </label>

                <button
                  className={styles.primaryButton}
                  type="submit"
                  disabled={isCreating}
                >
                  {isCreating
                    ? "Creating Account..."
                    : "+ Create Staff Account"}
                </button>
              </form>
            </section>

            {/* RECENTLY CREATED STAFF */}
            <section className={styles.tablePanel}>
              <div className={styles.panelHeading}>
                <div>
                  <h2>Recently Created Staff</h2>

                  <p>
                    Staff accounts created during this browser session.
                  </p>
                </div>
              </div>

              {createdStaff.length === 0 ? (
                <div className={styles.emptyState}>
                  <span className={styles.emptyIcon}>♙</span>

                  <strong>No staff accounts yet</strong>

                  <p>
                    Newly created staff accounts will appear here.
                  </p>
                </div>
              ) : (
                <div className={styles.staffList}>
                  {createdStaff.map((staff) => (
                    <article key={staff.id}>
                      <span className={styles.staffAvatar}>
                        {initials(staff.fullName)}
                      </span>

                      <div>
                        <strong>{staff.fullName}</strong>
                        <small>{staff.email}</small>
                        <small>{staff.accountType}</small>
                      </div>

                      <b>Active</b>
                    </article>
                  ))}
                </div>
              )}
            </section>
          </div>
        </div>
      </main>
    </div>
  );
}
