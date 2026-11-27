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
} from "../../lib/auth-session";
import { apiFetch } from "../../../utils/api";
import {
  PendingApplication,
  VolunteerRecord,
  VolunteerTaskRecord,
  errorMessage,
  formatDate,
  isPendingApplication,
  isVolunteerRecord,
  isVolunteerTaskRecord,
  requireRows,
} from "../management";
import styles from "./page.module.css";

const VOLUNTEER_ROLES = new Set(["Admin", "VolunteerCoordinator"]);

export default function VolunteerManagementPage() {
  const router = useRouter();
  const token = useSessionToken();
  const role = useSessionRole();
  const [volunteers, setVolunteers] = useState<VolunteerRecord[]>([]);
  const [tasks, setTasks] = useState<VolunteerTaskRecord[]>([]);
  const [applications, setApplications] = useState<PendingApplication[]>([]);
  const [search, setSearch] = useState("");
  const [filter, setFilter] = useState<"All" | VolunteerRecord["availability"]>("All");
  const [selected, setSelected] = useState<VolunteerRecord | null>(null);
  const [editing, setEditing] = useState<VolunteerRecord | null>(null);
  const [confirmingDelete, setConfirmingDelete] = useState<VolunteerRecord | null>(null);
  const [assignees, setAssignees] = useState<Record<string, string>>({});
  const [error, setError] = useState("");
  const [notice, setNotice] = useState("");
  const [busyId, setBusyId] = useState("");
  const [isLoading, setIsLoading] = useState(true);

  const refresh = useCallback(async () => {
    setError("");
    setIsLoading(true);
    try {
      const [volunteerResult, taskResult, applicationResult] = await Promise.all([
        apiFetch<unknown>("/api/volunteers"),
        apiFetch<unknown>("/api/volunteer-tasks"),
        role === "Admin"
          ? apiFetch<unknown>("/api/admin/users/pending-applications")
          : Promise.resolve([]),
      ]);
      const pendingApplications = requireRows(
        applicationResult,
        isPendingApplication,
        "pending applications",
      ).filter((application) => application.accountType === "Volunteer");
      setVolunteers(
        requireRows(volunteerResult, isVolunteerRecord, "volunteers").filter(
          (volunteer) =>
            !pendingApplications.some(
              (application) =>
                application.email.toLowerCase() === volunteer.email.toLowerCase(),
            ),
        ),
      );
      setTasks(requireRows(taskResult, isVolunteerTaskRecord, "volunteer tasks"));
      setApplications(pendingApplications);
    } catch (loadError) {
      setError(errorMessage(loadError, "Unable to load volunteer data."));
    } finally {
      setIsLoading(false);
    }
  }, [role]);

  useEffect(() => {
    if (token === AUTH_SESSION_LOADING || role === AUTH_SESSION_LOADING) return;
    if (!token || !VOLUNTEER_ROLES.has(role)) {
      router.replace(role === "DonorOfficer" ? "/dashboard/donors" : role ? "/portal" : "/login");
      return;
    }

    void refresh();
  }, [refresh, role, router, token]);

  const filteredVolunteers = useMemo(() => {
    const query = search.trim().toLowerCase();
    return volunteers.filter((volunteer) => {
      const matchesSearch =
        !query ||
        `${volunteer.fullName} ${volunteer.email} ${volunteer.phone ?? ""} ${volunteer.skills ?? ""}`
          .toLowerCase()
          .includes(query);
      return matchesSearch && (filter === "All" || volunteer.availability === filter);
    });
  }, [filter, search, volunteers]);

  const pendingTasks = tasks.filter((task) => !task.volunteerId);

  async function saveVolunteer(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!editing) return;
    setBusyId(editing.id);
    setError("");
    setNotice("");
    try {
      const saved = await apiFetch<unknown>(`/api/volunteers/${encodeURIComponent(editing.id)}`, {
        method: "PUT",
        body: {
          fullName: editing.fullName.trim(),
          email: editing.email.trim(),
          phone: editing.phone?.trim() || null,
          skills: editing.skills?.trim() || null,
          availability: editing.availability,
        },
      });
      if (!isVolunteerRecord(saved)) throw new Error("The server returned an invalid volunteer record.");
      setVolunteers((current) => current.map((volunteer) => volunteer.id === saved.id ? saved : volunteer));
      setSelected(saved);
      setEditing(null);
      setNotice("Volunteer details updated.");
    } catch (saveError) {
      setError(errorMessage(saveError, "Unable to update this volunteer."));
    } finally {
      setBusyId("");
    }
  }

  async function setVolunteerAvailability(volunteer: VolunteerRecord) {
    const availability = volunteer.availability === "Inactive" ? "Available" : "Inactive";
    setBusyId(volunteer.id);
    setError("");
    setNotice("");
    try {
      const saved = await apiFetch<unknown>(`/api/volunteers/${encodeURIComponent(volunteer.id)}`, {
        method: "PUT",
        body: {
          fullName: volunteer.fullName,
          email: volunteer.email,
          phone: volunteer.phone,
          skills: volunteer.skills,
          availability,
        },
      });
      if (!isVolunteerRecord(saved)) throw new Error("The server returned an invalid volunteer record.");
      setVolunteers((current) => current.map((item) => item.id === saved.id ? saved : item));
      if (selected?.id === saved.id) setSelected(saved);
      setNotice(`Volunteer marked ${availability.toLowerCase()}.`);
    } catch (actionError) {
      setError(errorMessage(actionError, "Unable to change volunteer availability."));
    } finally {
      setBusyId("");
    }
  }

  async function decide(application: PendingApplication, decision: "approve" | "reject") {
    setBusyId(application.id);
    setError("");
    setNotice("");
    try {
      await apiFetch<void>(
        `/api/admin/users/${encodeURIComponent(application.id)}/${decision}`,
        { method: "POST" },
      );
      setNotice(`${application.fullName}'s application was ${decision === "approve" ? "approved" : "rejected"}.`);
      await refresh();
    } catch (actionError) {
      setError(errorMessage(actionError, `Unable to ${decision} this application.`));
    } finally {
      setBusyId("");
    }
  }

  async function assignTask(task: VolunteerTaskRecord) {
    const volunteerId = assignees[task.id] || task.volunteerId;
    if (!volunteerId) {
      setError("Select a volunteer before assigning this task.");
      return;
    }
    setBusyId(task.id);
    setError("");
    setNotice("");
    try {
      const updated = await apiFetch<unknown>(
        `/api/volunteer-tasks/${encodeURIComponent(task.id)}/assign`,
        { method: "PUT", body: { volunteerId } },
      );
      if (!isVolunteerTaskRecord(updated)) throw new Error("The server returned an invalid task record.");
      setTasks((current) => current.map((item) => item.id === updated.id ? updated : item));
      setAssignees((current) => {
        const next = { ...current };
        delete next[task.id];
        return next;
      });
      setNotice(`Task "${task.title}" assigned to ${updated.volunteerName || "the selected volunteer"}.`);
    } catch (actionError) {
      setError(errorMessage(actionError, "Unable to assign this task."));
    } finally {
      setBusyId("");
    }
  }

  async function deleteVolunteer() {
    if (!confirmingDelete) return;
    const volunteer = confirmingDelete;
    setBusyId(volunteer.id);
    setError("");
    setNotice("");
    try {
      await apiFetch<void>(`/api/volunteers/${encodeURIComponent(volunteer.id)}`, { method: "DELETE" });
      setVolunteers((current) => current.filter((item) => item.id !== volunteer.id));
      if (selected?.id === volunteer.id) setSelected(null);
      setConfirmingDelete(null);
      setNotice(`${volunteer.fullName} was removed.`);
    } catch (deleteError) {
      setError(errorMessage(deleteError, "Unable to delete this volunteer."));
    } finally {
      setBusyId("");
    }
  }

  function signOut() {
    clearSessionCredentials();
    router.replace("/login");
  }

  if (token === AUTH_SESSION_LOADING || role === AUTH_SESSION_LOADING || !token || !VOLUNTEER_ROLES.has(role)) {
    return <main className={styles.statusPage} role="status">Checking volunteer dashboard access…</main>;
  }

  const tasksForSelected = selected
    ? tasks.filter((task) => task.volunteerId === selected.id)
    : [];

  return (
    <div className={styles.pageContainer}>
      <aside className={styles.sidebar}>
        <div className={styles.sidebarTop}>
          <Link href="/" className={styles.brandLogoBox}>
            <span className={styles.brandIconWrapper}>
              <Image src="/humanitarian-aid-platform-logo.svg" alt="" width={32} height={32} />
            </span>
            <span className={styles.brandTextGroup}>
              <span className={styles.brandHeading}>HUMANITARIAN</span>
              <span className={styles.brandSubheading}>VOLUNTEER MANAGEMENT</span>
            </span>
          </Link>
          <span className={styles.menuSectionLabel}>MANAGEMENT</span>
          <nav className={styles.navMenu} aria-label="Management">
            {role === "Admin" && <Link href="/dashboard/donors" className={styles.navItem}>Donors</Link>}
            <Link href="/dashboard/volunteers" className={`${styles.navItem} ${styles.activeNav}`}>Volunteers</Link>
            {role === "Admin" && <Link href="/admin" className={styles.navItem}>Admin tools</Link>}
            <Link href="/" className={styles.navItem}>Home</Link>
          </nav>
        </div>
        <div className={styles.sidebarBottom}>
          <span className={styles.sectorStatus}><span className={styles.statusDot} /> Signed in</span>
          <span className={styles.versionBadge}>v1</span>
        </div>
      </aside>

      <main className={styles.mainContent}>
        <header className={styles.topBar}>
          <div className={styles.topNavTabs}>
            {role === "Admin" && <Link href="/dashboard/donors" className={styles.tabButton}>Donors</Link>}
            <Link href="/dashboard/volunteers" className={`${styles.tabButton} ${styles.activeTabButton}`}>Volunteers</Link>
          </div>
          <div className={styles.topBarRight}>
            <div className={styles.userProfile}>
              <span className={styles.userAvatar}>{role === "Admin" ? "A" : "V"}</span>
              <span className={styles.userInfo}>
                <span className={styles.userName}>{role}</span>
                <span className={styles.userRole}>Management</span>
              </span>
              <button className={styles.logoutBtn} onClick={signOut} type="button">Sign out</button>
            </div>
          </div>
        </header>

        <div className={styles.pageBody}>
          <div className={styles.pageHeader}>
            <div className={styles.pageTitleGroup}>
              <div className={styles.titleWithBadge}>
                <h1>Volunteer Management</h1>
                <span className={styles.secBadge}>{role}</span>
              </div>
              <p className={styles.pageSubtitle}>Review volunteer applications, maintain responder profiles, and assign tasks.</p>
            </div>
          </div>

          {error && <p className={styles.errorMessage} role="alert">{error}</p>}
          {notice && <p className={styles.noticeMessage} role="status">{notice}</p>}

          <section className={styles.statsGrid} aria-label="Volunteer summary">
            <div className={styles.statCard}><span className={styles.statLabel}>VOLUNTEERS</span><strong className={styles.statNumber}>{volunteers.length}</strong></div>
            <div className={styles.statCard}><span className={styles.statLabel}>AVAILABLE</span><strong className={styles.statNumber}>{volunteers.filter((volunteer) => volunteer.availability === "Available").length}</strong></div>
            <div className={styles.statCard}><span className={styles.statLabel}>PENDING APPLICATIONS</span><strong className={styles.statNumber}>{applications.length}</strong></div>
            <div className={styles.statCard}><span className={styles.statLabel}>UNASSIGNED TASKS</span><strong className={styles.statNumber}>{pendingTasks.length}</strong></div>
          </section>

          {role === "Admin" ? (
          <section className={styles.dataCardShell}>
            <div className={styles.pipelineHeader}>
              <div className={styles.pipelineTitle}>Pending volunteer applications <span className={styles.realtimeBadge}>{applications.length}</span></div>
              <button className={styles.footerSecondaryBtn} type="button" onClick={() => void refresh()} disabled={isLoading}>Refresh</button>
            </div>
            {isLoading ? <p className={styles.emptyState}>Loading applications…</p> : applications.length === 0 ? (
              <p className={styles.emptyState}>There are no pending volunteer applications.</p>
            ) : (
              <div className={styles.tableResponsive}>
                <table className={styles.dataTable}>
                  <thead><tr><th>Name</th><th>Email</th><th>Phone</th><th>Skills</th><th>Applied</th><th>Actions</th></tr></thead>
                  <tbody>
                    {applications.map((application) => (
                      <tr key={application.id}>
                        <td>{application.fullName}</td><td>{application.email}</td><td>{application.phone || "—"}</td><td>{application.skills || "—"}</td><td>{formatDate(application.createdAt)}</td>
                        <td><div className={styles.actionLinks}>
                          <button className={styles.editActionBtn} type="button" disabled={busyId === application.id} onClick={() => void decide(application, "approve")}>Approve</button>
                          <button className={styles.deleteActionBtn} type="button" disabled={busyId === application.id} onClick={() => void decide(application, "reject")}>Reject</button>
                        </div></td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </section>
          ) : (
            <section className={styles.dataCardShell}>
              <div className={styles.pipelineHeader}><div className={styles.pipelineTitle}>Pending applications</div></div>
              <p className={styles.emptyState}>Application decisions are handled by an Admin account.</p>
            </section>
          )}

          <section className={styles.dataCardShell}>
            <div className={styles.filterBar}>
              <input className={styles.tableSearchInput} type="search" aria-label="Search volunteers" placeholder="Search by name, email, phone or skills" value={search} onChange={(event) => setSearch(event.target.value)} />
              <div className={styles.filterPillsGroup}>
                {(["All", "Available", "Busy", "Inactive"] as const).map((value) => (
                  <button key={value} type="button" className={`${styles.filterPill} ${filter === value ? styles.activeFilterPill : ""}`} onClick={() => setFilter(value)}>{value}</button>
                ))}
              </div>
            </div>
            <div className={styles.pipelineHeader}><div className={styles.pipelineTitle}>Volunteer accounts</div><span className={styles.sortSelector}>{filteredVolunteers.length} records</span></div>
            {isLoading ? <p className={styles.emptyState}>Loading volunteer accounts…</p> : filteredVolunteers.length === 0 ? (
              <p className={styles.emptyState}>No volunteers match your search.</p>
            ) : (
              <div className={styles.tableResponsive}>
                <table className={styles.dataTable}>
                  <thead><tr><th>Name</th><th>Email</th><th>Phone</th><th>Skills</th><th>Availability</th><th>Assigned tasks</th><th>Actions</th></tr></thead>
                  <tbody>
                    {filteredVolunteers.map((volunteer) => (
                      <tr key={volunteer.id} onClick={() => setSelected(volunteer)}>
                        <td><div className={styles.identityCell}><span className={`${styles.avatarBadge} ${styles.avatarVolunteer}`}>V</span><span className={styles.identityText}><span className={styles.identityName}>{volunteer.fullName}</span><span className={styles.identityCode}>{formatDate(volunteer.createdAt)}</span></span></div></td>
                        <td>{volunteer.email}</td><td>{volunteer.phone || "—"}</td><td><div className={styles.skillBadgesWrap}>{volunteer.skills?.split(",").map((skill) => skill.trim()).filter(Boolean).map((skill) => <span className={styles.skillPill} key={skill}>{skill}</span>) || "—"}</div></td>
                        <td><span className={`${styles.statusBadge} ${volunteer.availability === "Available" ? styles.statusAvailable : volunteer.availability === "Busy" ? styles.statusBusy : styles.statusInactive}`}><span className={styles.dot} />{volunteer.availability}</span></td>
                        <td>{tasks.filter((task) => task.volunteerId === volunteer.id).length}</td>
                        <td><div className={styles.actionLinks} onClick={(event) => event.stopPropagation()}>
                          <button className={styles.editActionBtn} type="button" onClick={() => setEditing({ ...volunteer })}>Edit</button>
                          <button className={styles.editActionBtn} type="button" disabled={busyId === volunteer.id} onClick={() => void setVolunteerAvailability(volunteer)}>{volunteer.availability === "Inactive" ? "Activate" : "Deactivate"}</button>
                          <button className={styles.deleteActionBtn} type="button" onClick={() => setConfirmingDelete(volunteer)}>Delete</button>
                        </div></td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </section>

          <section className={styles.dataCardShell}>
            <div className={styles.pipelineHeader}><div className={styles.pipelineTitle}>Volunteer task assignments</div><span className={styles.sortSelector}>{tasks.length} tasks</span></div>
            {isLoading ? <p className={styles.emptyState}>Loading tasks…</p> : tasks.length === 0 ? (
              <p className={styles.emptyState}>There are no volunteer tasks to assign.</p>
            ) : (
              <div className={styles.tableResponsive}>
                <table className={styles.dataTable}>
                  <thead><tr><th>Task</th><th>Location</th><th>Due</th><th>Status</th><th>Assigned volunteer</th><th>Action</th></tr></thead>
                  <tbody>
                    {tasks.map((task) => (
                      <tr key={task.id}>
                        <td>{task.title}</td><td>{task.location || "—"}</td><td>{task.dueDate ? formatDate(task.dueDate) : "—"}</td><td>{task.status}</td>
                        <td><select aria-label={`Assign ${task.title}`} value={assignees[task.id] ?? task.volunteerId ?? ""} onChange={(event) => setAssignees((current) => ({ ...current, [task.id]: event.target.value }))}><option value="">Select volunteer</option>{volunteers.filter((volunteer) => volunteer.availability !== "Inactive").map((volunteer) => <option key={volunteer.id} value={volunteer.id}>{volunteer.fullName}</option>)}</select></td>
                        <td><button className={styles.editActionBtn} type="button" disabled={busyId === task.id || !(assignees[task.id] || task.volunteerId)} onClick={() => void assignTask(task)}>{busyId === task.id ? "Assigning…" : "Assign"}</button></td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </section>
        </div>
      </main>

      {selected && (
        <div className={styles.modalBackdrop} onClick={() => setSelected(null)}>
          <section className={styles.modalDialog} role="dialog" aria-modal="true" aria-labelledby="volunteer-details-title" onClick={(event) => event.stopPropagation()}>
            <header className={styles.modalHeader}><div className={styles.modalTitleGroup}><h2 id="volunteer-details-title">{selected.fullName}</h2><p className={styles.modalSubtitle}>{selected.email} · {selected.phone || "No phone"}</p></div><button className={styles.modalCloseBtn} type="button" aria-label="Close volunteer details" onClick={() => setSelected(null)}>×</button></header>
            <p>Availability: <strong>{selected.availability}</strong></p><p>Skills: {selected.skills || "Not provided"}</p>
            <h3>Assigned tasks</h3>
            {tasksForSelected.length ? tasksForSelected.map((task) => <p key={task.id}>{task.title} · {task.status}{task.dueDate ? ` · Due ${formatDate(task.dueDate)}` : ""}</p>) : <p>No tasks assigned.</p>}
          </section>
        </div>
      )}

      {editing && (
        <div className={styles.modalBackdrop} onClick={() => setEditing(null)}>
          <section className={styles.modalDialog} role="dialog" aria-modal="true" aria-labelledby="edit-volunteer-title" onClick={(event) => event.stopPropagation()}>
            <header className={styles.modalHeader}><div className={styles.modalTitleGroup}><h2 id="edit-volunteer-title">Edit volunteer</h2><p className={styles.modalSubtitle}>Update the volunteer profile and availability.</p></div><button className={styles.modalCloseBtn} type="button" aria-label="Close editor" onClick={() => setEditing(null)}>×</button></header>
            <form className={styles.modalForm} onSubmit={saveVolunteer}>
              <label className={styles.formField}>Full name<input required value={editing.fullName} onChange={(event) => setEditing({ ...editing, fullName: event.target.value })} /></label>
              <label className={styles.formField}>Email<input required type="email" value={editing.email} onChange={(event) => setEditing({ ...editing, email: event.target.value })} /></label>
              <label className={styles.formField}>Phone<input type="tel" value={editing.phone ?? ""} onChange={(event) => setEditing({ ...editing, phone: event.target.value || null })} /></label>
              <label className={styles.formField}>Skills<input value={editing.skills ?? ""} onChange={(event) => setEditing({ ...editing, skills: event.target.value || null })} /></label>
              <label className={styles.formField}>Availability<select value={editing.availability} onChange={(event) => setEditing({ ...editing, availability: event.target.value as VolunteerRecord["availability"] })}><option>Available</option><option>Busy</option><option>Inactive</option></select></label>
              <div className={styles.modalFooter}><div className={styles.modalFooterRight}><button className={styles.modalCancelBtn} type="button" onClick={() => setEditing(null)}>Cancel</button><button className={styles.modalPrimaryBtn} type="submit" disabled={busyId === editing.id}>{busyId === editing.id ? "Saving…" : "Save changes"}</button></div></div>
            </form>
          </section>
        </div>
      )}

      {confirmingDelete && (
        <div className={styles.modalBackdrop} onClick={() => setConfirmingDelete(null)}>
          <section className={styles.modalDialog} role="alertdialog" aria-modal="true" aria-labelledby="delete-volunteer-title" onClick={(event) => event.stopPropagation()}>
            <header className={styles.modalHeader}><div className={styles.modalTitleGroup}><h2 id="delete-volunteer-title">Delete volunteer?</h2><p className={styles.modalSubtitle}>Remove {confirmingDelete.fullName} from volunteer management?</p></div></header>
            {error && <p className={styles.errorMessage} role="alert">{error}</p>}
            <div className={styles.modalFooter}><div className={styles.modalFooterRight}><button className={styles.modalCancelBtn} type="button" onClick={() => { setConfirmingDelete(null); setError(""); }}>Cancel</button><button className={styles.modalDeleteBtn} type="button" disabled={busyId === confirmingDelete.id} onClick={() => void deleteVolunteer()}>{busyId === confirmingDelete.id ? "Deleting…" : "Delete volunteer"}</button></div></div>
          </section>
        </div>
      )}
    </div>
  );
}
