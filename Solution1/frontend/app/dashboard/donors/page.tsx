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
  DonorRecord,
  DonationRecord,
  PendingApplication,
  errorMessage,
  formatCurrency,
  formatDate,
  isDonorRecord,
  isDonationRecord,
  isPendingApplication,
  requireRows,
} from "../management";
import styles from "./page.module.css";

const DONOR_ROLES = new Set(["Admin", "DonorOfficer"]);

export default function DonorManagementPage() {
  const router = useRouter();
  const token = useSessionToken();
  const role = useSessionRole();
  const [donors, setDonors] = useState<DonorRecord[]>([]);
  const [donations, setDonations] = useState<DonationRecord[]>([]);
  const [applications, setApplications] = useState<PendingApplication[]>([]);
  const [search, setSearch] = useState("");
  const [filter, setFilter] = useState<"All" | "Active" | "Inactive">("All");
  const [selected, setSelected] = useState<DonorRecord | null>(null);
  const [editing, setEditing] = useState<DonorRecord | null>(null);
  const [confirmingDelete, setConfirmingDelete] = useState<DonorRecord | null>(null);
  const [error, setError] = useState("");
  const [notice, setNotice] = useState("");
  const [busyId, setBusyId] = useState("");
  const [isLoading, setIsLoading] = useState(true);

  const refresh = useCallback(async () => {
    setError("");
    setIsLoading(true);
    try {
      const [donorResult, donationResult, applicationResult] = await Promise.all([
        apiFetch<unknown>("/api/donors"),
        apiFetch<unknown>("/api/donations"),
        role === "Admin"
          ? apiFetch<unknown>("/api/admin/users/pending-applications")
          : Promise.resolve([]),
      ]);
      const pendingApplications = requireRows(
        applicationResult,
        isPendingApplication,
        "pending applications",
      ).filter((application) => application.accountType === "Donor");
      setDonors(
        requireRows(donorResult, isDonorRecord, "donors").filter(
          (donor) =>
            !pendingApplications.some(
              (application) =>
                application.email.toLowerCase() === donor.email.toLowerCase(),
            ),
        ),
      );
      setDonations(requireRows(donationResult, isDonationRecord, "donations"));
      setApplications(pendingApplications);
    } catch (loadError) {
      setError(errorMessage(loadError, "Unable to load donor data."));
    } finally {
      setIsLoading(false);
    }
  }, [role]);

  useEffect(() => {
    if (token === AUTH_SESSION_LOADING || role === AUTH_SESSION_LOADING) return;
    if (!token || !DONOR_ROLES.has(role)) {
      router.replace(role === "VolunteerCoordinator" ? "/dashboard/volunteers" : role ? "/portal" : "/login");
      return;
    }

    void refresh();
  }, [refresh, role, router, token]);

  const filteredDonors = useMemo(() => {
    const query = search.trim().toLowerCase();
    return donors.filter((donor) => {
      const matchesSearch =
        !query ||
        `${donor.fullName} ${donor.email} ${donor.phone ?? ""} ${donor.type}`
          .toLowerCase()
          .includes(query);
      const matchesFilter =
        filter === "All" || (filter === "Active" ? donor.isActive : !donor.isActive);
      return matchesSearch && matchesFilter;
    });
  }, [donors, filter, search]);

  const totalDonations = donations.reduce((total, donation) => total + donation.amount, 0);

  async function saveDonor(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!editing) return;
    setBusyId(editing.id);
    setError("");
    setNotice("");
    try {
      const saved = await apiFetch<unknown>(`/api/donors/${encodeURIComponent(editing.id)}`, {
        method: "PUT",
        body: {
          fullName: editing.fullName.trim(),
          email: editing.email.trim(),
          phone: editing.phone?.trim() || null,
          type: editing.type,
          isActive: editing.isActive,
        },
      });
      if (!isDonorRecord(saved)) throw new Error("The server returned an invalid donor record.");
      setDonors((current) => current.map((donor) => donor.id === saved.id ? saved : donor));
      setSelected(saved);
      setEditing(null);
      setNotice("Donor details updated.");
    } catch (saveError) {
      setError(errorMessage(saveError, "Unable to update this donor."));
    } finally {
      setBusyId("");
    }
  }

  async function setDonorActive(donor: DonorRecord, isActive: boolean) {
    setBusyId(donor.id);
    setError("");
    setNotice("");
    try {
      const saved = await apiFetch<unknown>(`/api/donors/${encodeURIComponent(donor.id)}`, {
        method: "PUT",
        body: {
          fullName: donor.fullName,
          email: donor.email,
          phone: donor.phone,
          type: donor.type,
          isActive,
        },
      });
      if (!isDonorRecord(saved)) throw new Error("The server returned an invalid donor record.");
      setDonors((current) => current.map((item) => item.id === saved.id ? saved : item));
      if (selected?.id === saved.id) setSelected(saved);
      setNotice(`Donor ${isActive ? "activated" : "deactivated"}.`);
    } catch (actionError) {
      setError(errorMessage(actionError, "Unable to change donor status."));
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

  async function deleteDonor() {
    if (!confirmingDelete) return;
    const donor = confirmingDelete;
    setBusyId(donor.id);
    setError("");
    setNotice("");
    try {
      await apiFetch<void>(`/api/donors/${encodeURIComponent(donor.id)}`, { method: "DELETE" });
      setDonors((current) => current.filter((item) => item.id !== donor.id));
      if (selected?.id === donor.id) setSelected(null);
      setConfirmingDelete(null);
      setNotice(`${donor.fullName} was removed.`);
    } catch (deleteError) {
      setError(errorMessage(deleteError, "Unable to delete this donor. Try deactivating the account instead."));
    } finally {
      setBusyId("");
    }
  }

  function signOut() {
    clearSessionCredentials();
    router.replace("/login");
  }

  if (token === AUTH_SESSION_LOADING || role === AUTH_SESSION_LOADING || !token || !DONOR_ROLES.has(role)) {
    return <main className={styles.statusPage} role="status">Checking donor dashboard access…</main>;
  }

  const donationForSelected = selected
    ? donations.filter((donation) => donation.donorId === selected.id)
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
              <span className={styles.brandSubheading}>DONOR MANAGEMENT</span>
            </span>
          </Link>
          <span className={styles.menuSectionLabel}>MANAGEMENT</span>
          <nav className={styles.navMenu} aria-label="Management">
            <Link href="/dashboard/donors" className={`${styles.navItem} ${styles.activeNav}`}>Donors</Link>
            {role === "Admin" && <Link href="/dashboard/volunteers" className={styles.navItem}>Volunteers</Link>}
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
            <Link href="/dashboard/donors" className={`${styles.tabButton} ${styles.activeTabButton}`}>Donors</Link>
            {role === "Admin" && <Link href="/dashboard/volunteers" className={styles.tabButton}>Volunteers</Link>}
          </div>
          <div className={styles.topBarRight}>
            <div className={styles.userProfile}>
              <span className={styles.userAvatar}>{role === "Admin" ? "A" : "D"}</span>
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
                <h1>Donor Management</h1>
                <span className={styles.secBadge}>{role}</span>
              </div>
              <p className={styles.pageSubtitle}>Review donor applications and manage donor accounts and giving history.</p>
            </div>
          </div>

          {error && <p className={styles.errorMessage} role="alert">{error}</p>}
          {notice && <p className={styles.noticeMessage} role="status">{notice}</p>}

          <section className={styles.statsGrid} aria-label="Donor summary">
            <div className={styles.statCard}><span className={styles.statLabel}>DONORS</span><strong className={styles.statNumber}>{donors.length}</strong></div>
            <div className={styles.statCard}><span className={styles.statLabel}>ACTIVE</span><strong className={styles.statNumber}>{donors.filter((donor) => donor.isActive).length}</strong></div>
            <div className={styles.statCard}><span className={styles.statLabel}>PENDING APPLICATIONS</span><strong className={styles.statNumber}>{applications.length}</strong></div>
            <div className={styles.statCard}><span className={styles.statLabel}>RECORDED DONATIONS</span><strong className={styles.statNumber}>{formatCurrency(totalDonations)}</strong></div>
          </section>

          {role === "Admin" ? (
          <section className={styles.dataCardShell}>
            <div className={styles.pipelineHeader}>
              <div className={styles.pipelineTitle}>Pending donor applications <span className={styles.realtimeBadge}>{applications.length}</span></div>
              <button className={styles.footerSecondaryBtn} type="button" onClick={() => void refresh()} disabled={isLoading}>Refresh</button>
            </div>
            {isLoading ? <p className={styles.emptyState}>Loading applications…</p> : applications.length === 0 ? (
              <p className={styles.emptyState}>There are no pending donor applications.</p>
            ) : (
              <div className={styles.tableResponsive}>
                <table className={styles.dataTable}>
                  <thead><tr><th>Name</th><th>Email</th><th>Phone</th><th>Donor type</th><th>Applied</th><th>Actions</th></tr></thead>
                  <tbody>
                    {applications.map((application) => (
                      <tr key={application.id}>
                        <td>{application.fullName}</td><td>{application.email}</td><td>{application.phone || "—"}</td>
                        <td>{application.donorType || "—"}</td><td>{formatDate(application.createdAt)}</td>
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
              <input className={styles.tableSearchInput} type="search" aria-label="Search donors" placeholder="Search donors by name, email or phone" value={search} onChange={(event) => setSearch(event.target.value)} />
              <div className={styles.filterPillsGroup}>
                {(["All", "Active", "Inactive"] as const).map((value) => (
                  <button key={value} type="button" className={`${styles.filterPill} ${filter === value ? styles.activeFilterPill : ""}`} onClick={() => setFilter(value)}>{value}</button>
                ))}
              </div>
            </div>
            <div className={styles.pipelineHeader}><div className={styles.pipelineTitle}>Donor accounts</div><span className={styles.sortSelector}>{filteredDonors.length} records</span></div>
            {isLoading ? <p className={styles.emptyState}>Loading donor accounts…</p> : filteredDonors.length === 0 ? (
              <p className={styles.emptyState}>No donor accounts match your search.</p>
            ) : (
              <div className={styles.tableResponsive}>
                <table className={styles.dataTable}>
                  <thead><tr><th>Name</th><th>Email</th><th>Phone</th><th>Type</th><th>Status</th><th>Donations</th><th>Actions</th></tr></thead>
                  <tbody>
                    {filteredDonors.map((donor) => (
                      <tr key={donor.id} onClick={() => setSelected(donor)}>
                        <td><div className={styles.identityCell}><span className={`${styles.avatarBadge} ${donor.type === "Corporate" ? styles.avatarCorporate : styles.avatarIndividual}`}>{donor.type === "Corporate" ? "C" : "D"}</span><span className={styles.identityText}><span className={styles.identityName}>{donor.fullName}</span><span className={styles.identityCode}>{formatDate(donor.createdAt)}</span></span></div></td>
                        <td>{donor.email}</td><td>{donor.phone || "—"}</td><td>{donor.type}</td>
                        <td><span className={`${styles.statusBadge} ${donor.isActive ? styles.statusActive : styles.statusInactive}`}><span className={styles.dot} />{donor.isActive ? "Active" : "Inactive"}</span></td>
                        <td>{donations.filter((item) => item.donorId === donor.id).length}</td>
                        <td><div className={styles.actionLinks} onClick={(event) => event.stopPropagation()}>
                          <button className={styles.editActionBtn} type="button" onClick={() => setEditing({ ...donor })}>Edit</button>
                          <button className={styles.editActionBtn} type="button" disabled={busyId === donor.id} onClick={() => void setDonorActive(donor, !donor.isActive)}>{donor.isActive ? "Deactivate" : "Activate"}</button>
                          <button className={styles.deleteActionBtn} type="button" onClick={() => setConfirmingDelete(donor)}>Delete</button>
                        </div></td>
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
          <section className={styles.modalDialog} role="dialog" aria-modal="true" aria-labelledby="donor-details-title" onClick={(event) => event.stopPropagation()}>
            <header className={styles.modalHeader}><div className={styles.modalTitleGroup}><h2 id="donor-details-title">{selected.fullName}</h2><p className={styles.modalSubtitle}>{selected.email} · {selected.phone || "No phone"} · {selected.type}</p></div><button className={styles.modalCloseBtn} type="button" aria-label="Close donor details" onClick={() => setSelected(null)}>×</button></header>
            <p>Account status: <strong>{selected.isActive ? "Active" : "Inactive"}</strong></p>
            <h3>Donations</h3>
            {donationForSelected.length ? donationForSelected.map((donation) => <p key={donation.id}>{formatDate(donation.donatedAt)} · {donation.campaignName || donation.type} · {formatCurrency(donation.amount, donation.currency)}</p>) : <p>No donations recorded.</p>}
          </section>
        </div>
      )}

      {editing && (
        <div className={styles.modalBackdrop} onClick={() => setEditing(null)}>
          <section className={styles.modalDialog} role="dialog" aria-modal="true" aria-labelledby="edit-donor-title" onClick={(event) => event.stopPropagation()}>
            <header className={styles.modalHeader}><div className={styles.modalTitleGroup}><h2 id="edit-donor-title">Edit donor</h2><p className={styles.modalSubtitle}>Update the account details and active status.</p></div><button className={styles.modalCloseBtn} type="button" aria-label="Close editor" onClick={() => setEditing(null)}>×</button></header>
            <form className={styles.modalForm} onSubmit={saveDonor}>
              <label className={styles.formField}>Full name<input required value={editing.fullName} onChange={(event) => setEditing({ ...editing, fullName: event.target.value })} /></label>
              <label className={styles.formField}>Email<input required type="email" value={editing.email} onChange={(event) => setEditing({ ...editing, email: event.target.value })} /></label>
              <label className={styles.formField}>Phone<input type="tel" value={editing.phone ?? ""} onChange={(event) => setEditing({ ...editing, phone: event.target.value || null })} /></label>
              <label className={styles.formField}>Donor type<select value={editing.type} onChange={(event) => setEditing({ ...editing, type: event.target.value as DonorRecord["type"] })}><option>Individual</option><option>Corporate</option></select></label>
              <label className={styles.formField}>Account status<select value={editing.isActive ? "Active" : "Inactive"} onChange={(event) => setEditing({ ...editing, isActive: event.target.value === "Active" })}><option>Active</option><option>Inactive</option></select></label>
              <div className={styles.modalFooter}><div className={styles.modalFooterRight}><button className={styles.modalCancelBtn} type="button" onClick={() => setEditing(null)}>Cancel</button><button className={styles.modalPrimaryBtn} type="submit" disabled={busyId === editing.id}>{busyId === editing.id ? "Saving…" : "Save changes"}</button></div></div>
            </form>
          </section>
        </div>
      )}

      {confirmingDelete && (
        <div className={styles.modalBackdrop} onClick={() => setConfirmingDelete(null)}>
          <section className={styles.modalDialog} role="alertdialog" aria-modal="true" aria-labelledby="delete-donor-title" onClick={(event) => event.stopPropagation()}>
            <header className={styles.modalHeader}><div className={styles.modalTitleGroup}><h2 id="delete-donor-title">Delete donor?</h2><p className={styles.modalSubtitle}>Remove {confirmingDelete.fullName} from donor management? Donors with donations may need to be deactivated instead.</p></div></header>
            {error && <p className={styles.errorMessage} role="alert">{error}</p>}
            <div className={styles.modalFooter}><div className={styles.modalFooterRight}><button className={styles.modalCancelBtn} type="button" onClick={() => { setConfirmingDelete(null); setError(""); }}>Cancel</button><button className={styles.modalDeleteBtn} type="button" disabled={busyId === confirmingDelete.id} onClick={() => void deleteDonor()}>{busyId === confirmingDelete.id ? "Deleting…" : "Delete donor"}</button></div></div>
          </section>
        </div>
      )}
    </div>
  );
}
