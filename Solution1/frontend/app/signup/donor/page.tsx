"use client";

import React, { useState, useMemo } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { clearSessionCredentials } from "../../lib/auth-session";
import { Donor, DonorType, DonorStatus } from "../../lib/types";
import styles from "./page.module.css";

const INITIAL_DONORS: Donor[] = [
  {
    id: "1",
    code: "#CR-2023-0104",
    name: "Apex Logistics Lanka",
    email: "relief@apexlogistics.lk",
    phone: "+94 11 234 8800",
    type: "Corporate",
    status: "Active",
    dateCreated: "Oct 12, 2023",
    totalGiven: "Rs. 8,500,000",
    recentDonations: [
      { id: "d1", title: "Emergency Medical Supplies Fleet", amount: "Rs. 4,500,000", date: "Nov 02, 2024" },
      { id: "d2", title: "Heavy Transport Fuel Grant", amount: "Rs. 4,000,000", date: "Sep 14, 2024" },
    ],
  },
  {
    id: "2",
    code: "#CR-2023-0188",
    name: "Dr. Kusal Perera",
    email: "kusal.perera@health.org",
    phone: "+94 77 459 2109",
    type: "Individual",
    status: "Active",
    dateCreated: "Nov 04, 2023",
    totalGiven: "Rs. 1,200,000",
  },
  {
    id: "3",
    code: "#CR-2023-0245",
    name: "Crestline Marine Supplies",
    email: "info@crestlinemarine.com",
    phone: "+94 31 891 0021",
    type: "Corporate",
    status: "Inactive",
    dateCreated: "Dec 19, 2023",
    totalGiven: "Rs. 3,400,000",
  },
  {
    id: "4",
    code: "#CR-2024-0012",
    name: "Nalaka Wickramasinghe",
    email: "n.wickram@civicnet.org",
    phone: "+94 71 883 4521",
    type: "Individual",
    status: "Active",
    dateCreated: "Jan 08, 2024",
    totalGiven: "Rs. 750,000",
  },
  {
    id: "5",
    code: "#CR-2023-0089",
    name: "Dilmah Humanitarian Trust",
    email: "grants@dilmahtrust.org",
    phone: "+94 11 289 8000",
    type: "Corporate",
    status: "Active",
    dateCreated: "Jan 12, 2023",
    totalGiven: "Rs. 14,200,000",
    recentDonations: [
      { id: "d4", title: "Western Province Flood Relief", amount: "Rs. 5,000,000", date: "Nov 15, 2024" },
      { id: "d5", title: "Emergency Medical Kits Phase 2", amount: "Rs. 4,200,000", date: "Oct 02, 2024" },
      { id: "d6", title: "Dry Ration Pack Mobilization", amount: "Rs. 5,000,000", date: "Aug 28, 2024" },
    ],
  },
];

export default function DonorsPage() {
  const router = useRouter();
  const [donors, setDonors] = useState<Donor[]>(INITIAL_DONORS);
  const [selectedDonor, setSelectedDonor] = useState<Donor | null>(donors[0]);
  const [editingDonor, setEditingDonor] = useState<Donor | null>(null);
  const [isAddModalOpen, setIsAddModalOpen] = useState(false);
  const [searchTerm, setSearchTerm] = useState("");
  const [filterType, setFilterType] = useState<"All" | "Active" | "Individual" | "Corporate">("All");

  const filteredDonors = useMemo(() => {
    return donors.filter((d) => {
      const matchSearch =
        d.name.toLowerCase().includes(searchTerm.toLowerCase()) ||
        d.email.toLowerCase().includes(searchTerm.toLowerCase()) ||
        d.phone.includes(searchTerm) ||
        d.code.toLowerCase().includes(searchTerm.toLowerCase());
      if (!matchSearch) return false;

      if (filterType === "Active") return d.status === "Active";
      if (filterType === "Individual") return d.type === "Individual";
      if (filterType === "Corporate") return d.type === "Corporate";
      return true;
    });
  }, [donors, searchTerm, filterType]);

  const handleSignOut = () => {
    clearSessionCredentials();
    router.replace("/login");
  };

  const handleDelete = (id: string, e?: React.MouseEvent) => {
    e?.stopPropagation();
    if (confirm("Are you sure you want to delete this donor?")) {
      setDonors((prev) => prev.filter((d) => d.id !== id));
      if (selectedDonor?.id === id) setSelectedDonor(null);
      if (editingDonor?.id === id) setEditingDonor(null);
    }
  };

  return (
    <div className={styles.pageContainer}>
      {/* Left Sidebar */}
      <aside className={styles.sidebar}>
        <div className={styles.sidebarTop}>
          <Link href="/dashboard" className={styles.brandLogoBox}>
            <div className={styles.brandIconWrapper}>
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.2">
                <path d="M12 2L2 7l10 5 10-5-10-5zM2 17l10 5 10-5M2 12l10 5 10-5" />
              </svg>
            </div>
            <div className={styles.brandTextGroup}>
              <span className={styles.brandHeading}>HUMANITARIAN</span>
              <span className={styles.brandSubheading}>SHELTER & RESOURCE HUB</span>
            </div>
          </Link>

          <span className={styles.menuSectionLabel}>MAIN MENU</span>
          <nav className={styles.navMenu}>
            <Link href="/dashboard" className={styles.navItem}>
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2"><rect x="3" y="3" width="7" height="7"/><rect x="14" y="3" width="7" height="7"/><rect x="14" y="14" width="7" height="7"/><rect x="3" y="14" width="7" height="7"/></svg>
              <span>Overview</span>
            </Link>
            <Link href="/dashboard#map-card" className={styles.navItem}>
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2"><path d="M3 9l9-7 9 7v11a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z"/><polyline points="9 22 9 12 15 12 15 22"/></svg>
              <span>Shelter Management</span>
            </Link>
            <Link href="/dashboard#map-card" className={styles.navItem}>
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2"><path d="M21 16V8a2 2 0 0 0-1-1.73l-7-4a2 2 0 0 0-2 0l-7 4A2 2 0 0 0 3 8v8a2 2 0 0 0 1 1.73l7 4a2 2 0 0 0 2 0l7-4A2 2 0 0 0 21 16z"/></svg>
              <span>Resource Management</span>
            </Link>
            <Link href="/dashboard/donors" className={`${styles.navItem} ${styles.activeNav}`}>
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2"><path d="M19 14c1.49-1.46 3-3.21 3-5.5A5.5 5.5 0 0 0 16.5 3c-1.76 0-3 .5-4.5 2-1.5-1.5-2.74-2-4.5-2A5.5 5.5 0 0 0 2 8.5c0 2.3 1.5 4.05 3 5.5l7 7Z"/></svg>
              <span>Donors</span>
            </Link>
            <Link href="/dashboard/volunteers" className={styles.navItem}>
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2"><path d="M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2"/><circle cx="9" cy="7" r="4"/></svg>
              <span>Volunteers</span>
            </Link>
            <Link href="/dashboard#queue" className={styles.navItem}>
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2"><path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"/><polyline points="14 2 14 8 20 8"/><line x1="16" y1="13" x2="8" y2="13"/><line x1="16" y1="17" x2="8" y2="17"/></svg>
              <span>Request Tracker</span>
            </Link>
          </nav>
        </div>

        <div className={styles.sidebarBottom}>
          <div className={styles.sectorStatus}>
            <span className={styles.statusDot} />
            <span>SECTOR-04 ONLINE</span>
          </div>
          <span className={styles.versionBadge}>v2.4</span>
        </div>
      </aside>

      {/* Main Content */}
      <main className={styles.mainContent}>
        {/* Top Navbar */}
        <header className={styles.topBar}>
          <div className={styles.topNavTabs}>
            <Link href="/dashboard" className={styles.tabButton}>Coordinator Dashboard</Link>
            <Link href="/dashboard/donors" className={`${styles.tabButton} ${styles.activeTabButton}`}>Donors</Link>
            <Link href="/dashboard/volunteers" className={styles.tabButton}>Volunteers</Link>
            <Link href="/portal" className={styles.tabButton}>Citizen Portal</Link>
            <Link href="/dashboard#queue" className={styles.tabButton}>Requests</Link>
          </div>

          <div className={styles.topBarRight}>
            <div className={styles.searchBox}>
              <svg className={styles.searchIcon} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.2"><circle cx="11" cy="11" r="8"/><line x1="21" y1="21" x2="16.65" y2="16.65"/></svg>
              <input
                type="text"
                placeholder="Search relief records..."
                value={searchTerm}
                onChange={(e) => setSearchTerm(e.target.value)}
              />
            </div>

            <div className={styles.userProfile}>
              <div className={styles.userAvatar}>AS</div>
              <div className={styles.userInfo}>
                <span className={styles.userName}>Anura Senanayake</span>
                <span className={styles.userRole}>Staff Officer</span>
              </div>
              <button className={styles.logoutBtn} onClick={handleSignOut} title="Sign out" type="button">
                <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2"><path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4"/><polyline points="16 17 21 12 16 7"/><line x1="21" y1="12" x2="9" y2="12"/></svg>
              </button>
            </div>
          </div>
        </header>

        {/* Page Body */}
        <div className={styles.pageBody}>
          <div className={styles.pageHeader}>
            <div className={styles.pageTitleGroup}>
              <div className={styles.titleWithBadge}>
                <h1>Donors • Institutional & Public Registry</h1>
                <span className={styles.secBadge}>SEC-04</span>
              </div>
              <p className={styles.pageSubtitle}>
                Institutional emergency intake registry and donor accounts for rapid relief operations.
              </p>
            </div>

            <button className={styles.actionButton} onClick={() => setIsAddModalOpen(true)} type="button">
              + Add donor
            </button>
          </div>

          {/* 4 Stat Cards (Image 5) */}
          <div className={styles.statsGrid}>
            <div className={styles.statCard}>
              <div className={styles.statCardTop}>
                <span className={styles.statLabel}>TOTAL DONORS</span>
                <div className={styles.statIconBox} style={{ background: "#fee2e2", color: "#dc2626" }}>!</div>
              </div>
              <div className={styles.statValueRow}>
                <span className={styles.statNumber}>1,420</span>
                <span className={`${styles.statPill} ${styles.pillRed}`}>REGISTERED</span>
              </div>
              <div className={styles.statCardBottom}>
                <span>+12 in last 24h</span>
                <span className={styles.highlightRed}>↗ 94% intake rate</span>
              </div>
            </div>

            <div className={styles.statCard}>
              <div className={styles.statCardTop}>
                <span className={styles.statLabel}>ACTIVE DONORS</span>
                <div className={styles.statIconBox} style={{ background: "#dcfce7", color: "#16a34a" }}>↻</div>
              </div>
              <div className={styles.statValueRow}>
                <span className={styles.statNumber}>890</span>
                <span className={`${styles.statPill} ${styles.pillGreen}`}>ACTIVE</span>
              </div>
              <div className={styles.statCardBottom}>
                <span>Contributed recently</span>
                <span className={styles.highlightGreen}>✓ Validated</span>
              </div>
            </div>

            <div className={styles.statCard}>
              <div className={styles.statCardTop}>
                <span className={styles.statLabel}>CORPORATE PARTNERS</span>
                <div className={styles.statIconBox} style={{ background: "#ede9fe", color: "#6366f1" }}>⚙</div>
              </div>
              <div className={styles.statValueRow}>
                <span className={styles.statNumber}>42</span>
                <span className={`${styles.statPill} ${styles.pillPurple}`}>VERIFIED</span>
              </div>
              <div className={styles.statCardBottom}>
                <span>Logistics & grants</span>
                <span>Policy verified</span>
              </div>
            </div>

            <div className={styles.statCard}>
              <div className={styles.statCardTop}>
                <span className={styles.statLabel}>ALLOCATED FUNDS</span>
                <div className={styles.statIconBox} style={{ background: "#dcfce7", color: "#15803d" }}>Rs.</div>
              </div>
              <div className={styles.statValueRow}>
                <span className={styles.statNumber}>Rs. 24.5M</span>
              </div>
              <div className={styles.statCardBottom}>
                <span>Target: 26M/phase</span>
                <span className={styles.highlightGreen}>✓ 92% disbursed</span>
              </div>
            </div>
          </div>

          {/* Table Shell */}
          <div className={styles.dataCardShell}>
            <div className={styles.filterBar}>
              <div style={{ position: "relative", flex: 1, maxWidth: 360 }}>
                <svg className={styles.searchIcon} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.2"><circle cx="11" cy="11" r="8"/><line x1="21" y1="21" x2="16.65" y2="16.65"/></svg>
                <input
                  className={styles.tableSearchInput}
                  type="text"
                  placeholder="Search by name or email (#CR-...), or phone..."
                  value={searchTerm}
                  onChange={(e) => setSearchTerm(e.target.value)}
                />
              </div>

              <div className={styles.filterPillsGroup}>
                {(["All", "Active", "Individual", "Corporate"] as const).map((t) => (
                  <button
                    key={t}
                    className={`${styles.filterPill} ${filterType === t ? styles.activeFilterPill : ""}`}
                    onClick={() => setFilterType(t)}
                    type="button"
                  >
                    {t} {t === "All" && "1,420"} {t === "Active" && "890"}
                  </button>
                ))}
              </div>
            </div>

            <div className={styles.pipelineHeader}>
              <div className={styles.pipelineTitle}>
                <span>Donor Registry & Allocation Pipeline</span>
                <span className={styles.realtimeBadge}>Feed: Real-time</span>
              </div>
              <span className={styles.sortSelector}>Sort: Registration Date (Newest)</span>
            </div>

            <div className={styles.tableResponsive}>
              <table className={styles.dataTable}>
                <thead>
                  <tr>
                    <th>FULL NAME</th>
                    <th>EMAIL</th>
                    <th>PHONE</th>
                    <th>TYPE</th>
                    <th>STATUS</th>
                    <th>DATE CREATED</th>
                    <th>ACTIONS</th>
                  </tr>
                </thead>
                <tbody>
                  {filteredDonors.map((donor) => (
                    <tr key={donor.id} onClick={() => setSelectedDonor(donor)}>
                      <td>
                        <div className={styles.identityCell}>
                          <div className={`${styles.avatarBadge} ${donor.type === "Corporate" ? styles.avatarCorporate : styles.avatarIndividual}`}>
                            {donor.type === "Corporate" ? "🏢" : "👤"}
                          </div>
                          <div className={styles.identityText}>
                            <span className={styles.identityName}>{donor.name}</span>
                            <span className={styles.identityCode}>{donor.code}</span>
                          </div>
                        </div>
                      </td>
                      <td>{donor.email}</td>
                      <td>{donor.phone}</td>
                      <td>
                        <span className={`${styles.typeBadge} ${donor.type === "Corporate" ? styles.corporate : styles.individual}`}>
                          {donor.type === "Corporate" ? "🏢 Corporate" : "👤 Individual"}
                        </span>
                      </td>
                      <td>
                        <span className={`${styles.statusBadge} ${donor.status === "Active" ? styles.statusActive : styles.statusInactive}`}>
                          <span className={styles.dot} />
                          {donor.status}
                        </span>
                      </td>
                      <td>{donor.dateCreated}</td>
                      <td>
                        <div className={styles.actionLinks}>
                          <button className={styles.editActionBtn} onClick={(e) => { e.stopPropagation(); setEditingDonor(donor); }} type="button">Edit</button>
                          <button className={styles.deleteActionBtn} onClick={(e) => handleDelete(donor.id, e)} type="button">Delete</button>
                        </div>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>
        </div>
      </main>

      {/* Edit Modal (Image 1) */}
      {editingDonor && (
        <div className={styles.modalBackdrop} onClick={() => setEditingDonor(null)}>
          <div className={styles.modalDialog} onClick={(e) => e.stopPropagation()}>
            <div className={styles.modalHeader}>
              <div className={styles.modalHeaderLeft}>
                <div className={styles.modalIconBadge}>🏢</div>
                <div className={styles.modalTitleGroup}>
                  <h2>
                    <span>Edit Donor Details</span>
                    <span className={styles.modalIdPill}>{editingDonor.code}</span>
                  </h2>
                  <p className={styles.modalSubtitle}>Update donor contact details, classification, and registry status.</p>
                </div>
              </div>
              <button className={styles.modalCloseBtn} onClick={() => setEditingDonor(null)} type="button">✕</button>
            </div>

            <form
              className={styles.modalForm}
              onSubmit={(e) => {
                e.preventDefault();
                setDonors((prev) => prev.map((d) => (d.id === editingDonor.id ? editingDonor : d)));
                setEditingDonor(null);
              }}
            >
              <div className={styles.formGridTwoCol}>
                <div className={styles.formField}>
                  <label>Full Name *</label>
                  <input
                    type="text"
                    required
                    value={editingDonor.name}
                    onChange={(e) => setEditingDonor({ ...editingDonor, name: e.target.value })}
                  />
                </div>
                <div className={styles.formField}>
                  <label>Email Address *</label>
                  <input
                    type="email"
                    required
                    value={editingDonor.email}
                    onChange={(e) => setEditingDonor({ ...editingDonor, email: e.target.value })}
                  />
                </div>
              </div>

              <div className={styles.formGridTwoCol}>
                <div className={styles.formField}>
                  <label>Phone Number *</label>
                  <input
                    type="text"
                    required
                    value={editingDonor.phone}
                    onChange={(e) => setEditingDonor({ ...editingDonor, phone: e.target.value })}
                  />
                </div>
                <div className={styles.formField}>
                  <label>Date Created</label>
                  <input disabled value={editingDonor.dateCreated} />
                </div>
              </div>

              <div className={styles.formField}>
                <label>Donor Type *</label>
                <div className={styles.segmentedControl}>
                  <button
                    type="button"
                    className={`${styles.segmentButton} ${editingDonor.type === "Individual" ? styles.segmentActive : ""}`}
                    onClick={() => setEditingDonor({ ...editingDonor, type: "Individual" })}
                  >
                    👤 Individual {editingDonor.type === "Individual" && "✓"}
                  </button>
                  <button
                    type="button"
                    className={`${styles.segmentButton} ${editingDonor.type === "Corporate" ? styles.segmentActive : ""}`}
                    onClick={() => setEditingDonor({ ...editingDonor, type: "Corporate" })}
                  >
                    🏢 Corporate {editingDonor.type === "Corporate" && "✓"}
                  </button>
                </div>
              </div>

              <div className={styles.formField}>
                <label>Status *</label>
                <select
                  value={editingDonor.status}
                  onChange={(e) => setEditingDonor({ ...editingDonor, status: e.target.value as DonorStatus })}
                >
                  <option value="Active">● Active</option>
                  <option value="Inactive">● Inactive</option>
                </select>
              </div>

              <div className={styles.modalFooter}>
                <button type="button" className={styles.modalDeleteBtn} onClick={() => handleDelete(editingDonor.id)}>
                  🗑 Delete
                </button>
                <div className={styles.modalFooterRight}>
                  <button type="button" className={styles.modalCancelBtn} onClick={() => setEditingDonor(null)}>Cancel</button>
                  <button type="submit" className={styles.modalPrimaryBtn}>✓ Save Changes</button>
                </div>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* Add Modal (Image 2) */}
      {isAddModalOpen && (
        <div className={styles.modalBackdrop} onClick={() => setIsAddModalOpen(false)}>
          <div className={styles.modalDialog} onClick={(e) => e.stopPropagation()}>
            <div className={styles.modalHeader}>
              <div className={styles.modalHeaderLeft}>
                <div className={styles.modalIconBadge}>+👤</div>
                <div className={styles.modalTitleGroup}>
                  <h2>Add New Donor</h2>
                  <p className={styles.modalSubtitle}>Register a new individual contributor or corporate donor partner</p>
                </div>
              </div>
              <button className={styles.modalCloseBtn} onClick={() => setIsAddModalOpen(false)} type="button">✕</button>
            </div>

            <form
              className={styles.modalForm}
              onSubmit={(e) => {
                e.preventDefault();
                const fd = new FormData(e.currentTarget);
                const created: Donor = {
                  id: String(Date.now()),
                  code: `#CR-2024-${Math.floor(1000 + Math.random() * 9000)}`,
                  name: fd.get("name") as string,
                  email: fd.get("email") as string,
                  phone: fd.get("phone") as string,
                  type: fd.get("type") as DonorType,
                  status: fd.get("status") as DonorStatus,
                  dateCreated: "Today (Auto-generated)",
                };
                setDonors((prev) => [created, ...prev]);
                setIsAddModalOpen(false);
              }}
            >
              <div className={styles.formGridTwoCol}>
                <div className={styles.formField}>
                  <label>Full Name *</label>
                  <input name="name" required placeholder="e.g., Dilmah Humanitarian Trust" />
                </div>
                <div className={styles.formField}>
                  <label>Email Address *</label>
                  <input name="email" type="email" required placeholder="e.g., donor@organization.org" />
                </div>
              </div>

              <div className={styles.formGridTwoCol}>
                <div className={styles.formField}>
                  <label>Phone Number *</label>
                  <input name="phone" required placeholder="e.g., +94 11 234 5678" />
                </div>
                <div className={styles.formField}>
                  <label>Date Registered</label>
                  <input disabled value="Today (Auto-generated)" />
                </div>
              </div>

              <div className={styles.formGridTwoCol}>
                <div className={styles.formField}>
                  <label>Donor Type *</label>
                  <select name="type" defaultValue="Individual">
                    <option value="Individual">Individual</option>
                    <option value="Corporate">Corporate</option>
                  </select>
                </div>
                <div className={styles.formField}>
                  <label>Status *</label>
                  <select name="status" defaultValue="Active">
                    <option value="Active">● Active</option>
                    <option value="Inactive">● Inactive</option>
                  </select>
                </div>
              </div>

              <div className={styles.infoBanner}>
                <span>Newly registered donors can immediately be linked to active shelter consignments and emergency distribution batches.</span>
              </div>

              <div className={styles.modalFooter}>
                <div className={styles.modalFooterRight}>
                  <button type="button" className={styles.modalCancelBtn} onClick={() => setIsAddModalOpen(false)}>Cancel</button>
                  <button type="submit" className={styles.modalPrimaryBtn}>+ Add Donor</button>
                </div>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}