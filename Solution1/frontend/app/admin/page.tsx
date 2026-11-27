"use client";

import Image from "next/image";
import Link from "next/link";
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

const API_BASE_URL =
  (process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5001").replace(/\/+$/, "");

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
    const error = body as { detail?: unknown; message?: unknown; title?: unknown };
    const message = error.detail ?? error.message ?? error.title;
    if (typeof message === "string" && message) return message;
  }
  return fallback;
}

function isPendingApplication(value: unknown): value is PendingApplication {
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

async function loadPendingApplications(token: string): Promise<PendingApplication[]> {
  const response = await fetch(`${API_BASE_URL}/api/admin/users/pending-applications`, {
    headers: { Authorization: `Bearer ${token}` },
  });
  const body: unknown = await response.json().catch(() => undefined);
  if (!response.ok) {
    throw new Error(getErrorMessage(body, `Unable to load applications (${response.status}).`));
  }
  if (!Array.isArray(body) || !body.every(isPendingApplication)) {
    throw new Error("The server returned an invalid applications response.");
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
  return Number.isNaN(date.getTime()) ? "Unknown" : date.toLocaleDateString();
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
  const [role, setRole] = useState<(typeof STAFF_ROLES)[number]>(STAFF_ROLES[0]);
  const [isCreating, setIsCreating] = useState(false);
  const [query, setQuery] = useState("");
  const [mobileMenuOpen, setMobileMenuOpen] = useState(false);

  const refresh = useCallback(async (accessToken: string) => {
    setError("");
    setIsLoading(true);
    try {
      setApplications(await loadPendingApplications(accessToken));
    } catch (loadError) {
      setError(loadError instanceof Error ? loadError.message : "Unable to load applications.");
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    if (token === AUTH_SESSION_LOADING || accountRole === AUTH_SESSION_LOADING) return;
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
          setError(loadError instanceof Error ? loadError.message : "Unable to load applications.");
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
        .includes(normalized),
    );
  }, [applications, query]);

  async function decide(application: PendingApplication, decision: "approve" | "reject") {
    setBusyId(application.id);
    setError("");
    setNotice("");
    try {
      const response = await fetch(
        `${API_BASE_URL}/api/admin/users/${encodeURIComponent(application.id)}/${decision}`,
        {
          method: "POST",
          headers: { Authorization: `Bearer ${token}` },
        },
      );
      if (!response.ok) {
        const body: unknown = await response.json().catch(() => undefined);
        throw new Error(getErrorMessage(body, `Unable to ${decision} this application (${response.status}).`));
      }
      setNotice(`${application.fullName}'s application was ${decision === "approve" ? "approved" : "rejected"}.`);
      await refresh(token);
    } catch (actionError) {
      setError(actionError instanceof Error ? actionError.message : `Unable to ${decision} this application.`);
    } finally {
      setBusyId("");
    }
  }

  async function createStaffAccount(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError("");
    setNotice("");
    if (password.length < 12) {
      setError("Staff passwords must be at least 12 characters.");
      return;
    }

    setIsCreating(true);
    try {
      const response = await fetch(`${API_BASE_URL}/api/admin/users/staff`, {
        method: "POST",
        headers: {
          Authorization: `Bearer ${token}`,
          "Content-Type": "application/json",
        },
        body: JSON.stringify({ fullName: fullName.trim(), email: email.trim(), password, role }),
      });
      const body: unknown = await response.json().catch(() => undefined);
      if (!response.ok) {
        throw new Error(getErrorMessage(body, `Unable to create staff account (${response.status}).`));
      }

      const created = body && typeof body === "object"
        ? body as Record<string, unknown>
        : {};
      setCreatedStaff((current) =>
        [{
          id: typeof created.id === "string" ? created.id : `${email}-${Date.now()}`,
          fullName: typeof created.fullName === "string" ? created.fullName : fullName.trim(),
          email: typeof created.email === "string" ? created.email : email.trim(),
          accountType: typeof created.role === "string" ? created.role : role,
          phone: null,
          skills: null,
          donorType: null,
          createdAt: new Date().toISOString(),
        }, ...current].slice(0, 5));
      setNotice(`Staff account created for ${email.trim()}.`);
      setFullName("");
      setEmail("");
      setPassword("");
    } catch (createError) {
      setError(createError instanceof Error ? createError.message : "Unable to create staff account.");
    } finally {
      setIsCreating(false);
    }
  }

  function signOut() {
    clearSessionCredentials();
    router.replace("/login");
  }

  if (token === AUTH_SESSION_LOADING || accountRole === AUTH_SESSION_LOADING) {
    return <main className={styles.statusPage} role="status">Checking administrator access…</main>;
  }
  if (!token || accountRole !== "Admin") {
    return <main className={styles.statusPage} role="status">Redirecting…</main>;
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
      <aside className={`${styles.sidebar} ${mobileMenuOpen ? styles.sidebarOpen : ""}`}>
        <button className={styles.brand} type="button" onClick={() => window.scrollTo({ top: 0, behavior: "smooth" })}>
          <Image src="/humanitarian-aid-platform-logo.svg" alt="" width={38} height={38} />
          <span><strong>Humanitarian</strong><small>Aid Platform</small></span>
        </button>
        <nav className={styles.sideNav} aria-label="Admin navigation">
          <a className={styles.activeNav} href="#top" onClick={() => setMobileMenuOpen(false)}><span>⌂</span>Overview</a>
          <a href="#applications" onClick={() => setMobileMenuOpen(false)}><span>✓</span>Applications
            {applications.length > 0 && <b>{applications.length}</b>}
          </a>
          <a href="#staff" onClick={() => setMobileMenuOpen(false)}><span>♙</span>Create Staff</a>
          <Link href="/dashboard/donors" onClick={() => setMobileMenuOpen(false)}>Donor management</Link>
          <Link href="/dashboard/volunteers" onClick={() => setMobileMenuOpen(false)}>Volunteer management</Link>
        </nav>
        <div className={styles.sidebarFooter}>
          <span>SA</span>
          <div><strong>System Admin</strong><small>Administrator</small></div>
        </div>
      </aside>

      <main className={styles.main} id="top">
        <header className={styles.topbar}>
          <button
            className={styles.menuButton}
            type="button"
            aria-label={mobileMenuOpen ? "Close admin menu" : "Open admin menu"}
            aria-expanded={mobileMenuOpen}
            onClick={() => setMobileMenuOpen((open) => !open)}
          >
            {mobileMenuOpen ? "×" : "☰"}
          </button>
          <div><strong>Administrator workspace</strong><small>Humanitarian Aid Platform</small></div>
          <button className={styles.signOut} type="button" onClick={signOut}>Sign out</button>
        </header>

        <div className={styles.content}>
          <section className={styles.welcome}>
            <div><p>Humanitarian Aid Platform · Admin</p><h1>Administrator workspace</h1><span>Review applications and manage platform staff access.</span></div>
            <button className={styles.secondaryButton} type="button" disabled={isLoading} onClick={() => void refresh(token)}>↻ Refresh</button>
          </section>

          {error && <p className={styles.errorAlert} role="alert">{error}</p>}
          {notice && <p className={styles.successAlert} role="status">{notice}</p>}

          <section className={styles.metrics} aria-label="Administrator summary">
            <article><span>✓</span><div><small>Pending applications</small><strong>{isLoading ? "…" : applications.length}</strong></div></article>
            <article><span>♙</span><div><small>Recently created staff</small><strong>{createdStaff.length}</strong></div></article>
            <article><span>◆</span><div><small>Administrator role</small><strong>Admin</strong></div></article>
          </section>

          <section className={styles.card} id="applications" aria-labelledby="applications-heading">
            <div className={styles.cardHeader}>
              <div><h2 id="applications-heading">Pending applications</h2><p>Volunteer and donor accounts awaiting administrator approval.</p></div>
              <label className={styles.search}><span>⌕</span><input type="search" placeholder="Search applications" value={query} onChange={(event) => setQuery(event.target.value)} /></label>
            </div>
            {isLoading ? (
              <p className={styles.emptyState} role="status">Loading applications…</p>
            ) : filteredApplications.length === 0 ? (
              <p className={styles.emptyState}>No pending applications match this view.</p>
            ) : (
              <div className={styles.tableWrap}>
                <table>
                  <thead><tr><th>Applicant</th><th>Application</th><th>Profile</th><th>Applied</th><th>Action</th></tr></thead>
                  <tbody>
                    {filteredApplications.map((application) => (
                      <tr key={application.id}>
                        <td><div className={styles.applicant}><span>{initials(application.fullName)}</span><div><strong>{application.fullName}</strong><small>{application.email}</small>{application.phone && <small>{application.phone}</small>}</div></div></td>
                        <td><span className={application.accountType === "Volunteer" ? styles.volunteerTag : styles.donorTag}>{application.accountType}</span></td>
                        <td><div className={styles.profileDetail}>{application.skills && <span>{application.skills}</span>}{application.donorType && <span>{application.donorType}</span>}{!application.skills && !application.donorType && <span>Profile submitted</span>}</div></td>
                        <td>{formatDate(application.createdAt)}</td>
                        <td><div className={styles.rowActions}>
                          <button className={styles.approveButton} type="button" disabled={busyId === application.id} onClick={() => void decide(application, "approve")}>Approve</button>
                          <button className={styles.rejectButton} type="button" disabled={busyId === application.id} onClick={() => void decide(application, "reject")}>Reject</button>
                        </div></td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </section>

          <div className={styles.lowerGrid}>
            <section className={styles.card} id="staff" aria-labelledby="staff-heading">
              <div className={styles.cardHeader}><div><h2 id="staff-heading">Create staff account</h2><p>Create an active staff account with a platform role.</p></div></div>
              <form className={styles.staffForm} onSubmit={createStaffAccount}>
                <label><span>Full name</span><input required maxLength={100} placeholder="Staff member name" value={fullName} onChange={(event) => setFullName(event.target.value)} /></label>
                <label><span>Work email</span><input required type="email" maxLength={256} placeholder="staff@example.com" value={email} onChange={(event) => setEmail(event.target.value)} /></label>
                <label><span>Password</span><input required type="password" minLength={12} maxLength={100} placeholder="At least 12 characters" autoComplete="new-password" value={password} onChange={(event) => setPassword(event.target.value)} /></label>
                <label><span>Staff role</span><select value={role} onChange={(event) => setRole(event.target.value as (typeof STAFF_ROLES)[number])}>{STAFF_ROLES.map((staffRole) => <option key={staffRole}>{staffRole}</option>)}</select></label>
                <button className={styles.primaryButton} type="submit" disabled={isCreating}>{isCreating ? "Creating…" : "Create staff account"}</button>
              </form>
            </section>

            <section className={styles.card} aria-labelledby="recent-heading">
              <div className={styles.cardHeader}><div><h2 id="recent-heading">Recently created staff</h2><p>Accounts created during this browser session.</p></div></div>
              {createdStaff.length === 0 ? <p className={styles.emptyState}>No staff accounts created in this session.</p> : (
                <div className={styles.staffList}>
                  {createdStaff.map((staff) => (
                    <article key={staff.id}><span>{initials(staff.fullName)}</span><div><strong>{staff.fullName}</strong><small>{staff.email}</small></div><b>Active</b></article>
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
