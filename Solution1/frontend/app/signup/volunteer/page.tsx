"use client";

import React, { useState, useMemo } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { clearSessionCredentials } from "../../lib/auth-session";
import { Volunteer, VolunteerAvailability } from "../../lib/types";
import styles from "./page.module.css";

const INITIAL_VOLUNTEERS: Volunteer[] = [
  {
    id: "1",
    code: "#CR-2023-0104",
    name: "Sunil Dharmadasa",
    email: "sunil.d@civilsafety.lk",
    phone: "+94 77 450 1199",
    skills: ["Paramedic / First Aid", "Heavy Vehicle Driver"],
    availability: "Available",
    dateCreated: "Oct 12, 2023",
  },
  {
    id: "2",
    code: "#CR-2023-0188",
    name: "Nadeesha Fernando",
    email: "nadeesha.f@relief.lk",
    phone: "+94 71 678 2341",
    skills: ["Logistics & Warehousing", "Community Kitchen"],
    availability: "Busy",
    dateCreated: "Nov 04, 2023",
  },
  {
    id: "3",
    code: "#CR-2023-0245",
    name: "Chathura Wickramasinghe",
    email: "c.wickrama@support.org",
    phone: "+94 71 334 8920",
    skills: ["Water Purification Spec.", "Generator Technician"],
    availability: "Available",
    dateCreated: "Dec 19, 2023",
  },
  {
    id: "4",
    code: "#CR-2024-0012",
    name: "Malsha Jayawardena",
    email: "m.jaya@civilsafety.lk",
    phone: "+94 70 215 6789",
    skills: ["Search & Rescue", "Drone Surveillance"],
    availability: "Inactive",
    dateCreated: "Jan 08, 2024",
  },
  {
    id: "5",
    code: "#CR-2024-0045",
    name: "Kasun Bandara",
    email: "k.bandara@hospital.gov.lk",
    phone: "+94 75 882 1430",
    skills: ["Emergency Triage", "Field Medic"],
    availability: "Busy",
    dateCreated: "Feb 14, 2024",
  },
];

export default function VolunteersPage() {
  const router = useRouter();
  const [volunteers, setVolunteers] = useState<Volunteer[]>(INITIAL_VOLUNTEERS);
  const [selectedVolunteer, setSelectedVolunteer] = useState<Volunteer | null>(volunteers[0]);
  const [editingVolunteer, setEditingVolunteer] = useState<Volunteer | null>(null);
  const [isAddModalOpen, setIsAddModalOpen] = useState(false);
  const [searchTerm, setSearchTerm] = useState("");
  const [availabilityFilter, setAvailabilityFilter] = useState<"All" | "Available" | "Busy" | "Inactive">("All");

  const filteredVolunteers = useMemo(() => {
    return volunteers.filter((vol) => {
      const matchSearch =
        vol.name.toLowerCase().includes(searchTerm.toLowerCase()) ||
        vol.email.toLowerCase().includes(searchTerm.toLowerCase()) ||
        vol.phone.includes(searchTerm) ||
        vol.code.toLowerCase().includes(searchTerm.toLowerCase()) ||
        vol.skills.some((s) => s.toLowerCase().includes(searchTerm.toLowerCase()));
      if (!matchSearch) return false;

      if (availabilityFilter === "Available") return vol.availability === "Available";
      if (availabilityFilter === "Busy") return vol.availability === "Busy";
      if (availabilityFilter === "Inactive") return vol.availability === "Inactive";
      return true;
    });
  }, [volunteers, searchTerm, availabilityFilter]);

  const handleSignOut = () => {
    clearSessionCredentials();
    router.replace("/login");
  };

  const handleDelete = (id: string, e?: React.MouseEvent) => {
    e?.stopPropagation();
    if (confirm("Are you sure you want to remove this volunteer responder?")) {
      setVolunteers((prev) => prev.filter((v) => v.id !== id));
      if (selectedVolunteer?.id === id) setSelectedVolunteer(null);
      if (editingVolunteer?.id === id) setEditingVolunteer(null);
    }
  };

  return (
    <div className={styles.pageContainer}>
      {/* Sidebar */}
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
            <Link href="/dashboard/donors" className={styles.navItem}>
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2"><path d="M19 14c1.49-1.46 3-3.21 3-5.5A5.5 5.5 0 0 0 16.5 3c-1.76 0-3 .5-4.5 2-1.5-1.5-2.74-2-4.5-2A5.5 5.5 0 0 0 2 8.5c0 2.3 1.5 4.05 3 5.5l7 7Z"/></svg>
              <span>Donors</span>
            </Link>
            <Link href="/dashboard/volunteers" className={`${styles.navItem} ${styles.activeNav}`}>
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

      {/* Main Area */}
      <main className={styles.mainContent}>
        {/* Top Navbar */}
        <header className={styles.topBar}>
          <div className={styles.topNavTabs}>
            <Link href="/dashboard" className={styles.tabButton}>Coordinator Dashboard</Link>
            <Link href="/dashboard/donors" className={styles.tabButton}>Donors</Link>
            <Link href="/dashboard/volunteers" className={`${styles.tabButton} ${styles.activeTabButton}`}>Volunteers</Link>
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
                <h1>Volunteers • Institutional & Field Registry</h1>
                <span className={styles.secBadge}>SEC-04</span>
              </div>
              <p className={styles.pageSubtitle}>
                Institutional emergency intake registry and volunteer deployment for rapid relief operations.
              </p>
            </div>

            <button className={styles.actionButton} onClick={() => setIsAddModalOpen(true)} type="button">
              + Add volunteer
            </button>
          </div>

          {/* 4 Stat Cards (Image 4) */}
          <div className={styles.statsGrid}>
            <div className={styles.statCard}>
              <div className={styles.statCardTop}>
                <span className={styles.statLabel}>TOTAL VOLUNTEERS</span>
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
                <span className={styles.statLabel}>ACTIVE VOLUNTEERS</span>
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
                <span className={styles.statLabel}>SKILLED RESPONDERS</span>
                <div className={styles.statIconBox} style={{ background: "#ede9fe", color: "#6366f1" }}>⚡</div>
              </div>
              <div className={styles.statValueRow}>
                <span className={styles.statNumber}>342</span>
                <span className={`${styles.statPill} ${styles.pillPurple}`}>VERIFIED</span>
              </div>
              <div className={styles.statCardBottom}>
                <span>Emergency & medical</span>
                <span>Policy verified</span>
              </div>
            </div>

            <div className={styles.statCard}>
              <div className={styles.statCardTop}>
                <span className={styles.statLabel}>AVAILABLE VOLUNTEERS</span>
                <div className={styles.statIconBox} style={{ background: "#dcfce7", color: "#15803d" }}>✓</div>
              </div>
              <div className={styles.statValueRow}>
                <span className={styles.statNumber}>342</span>
              </div>
              <div className={styles.statCardBottom}>
                <span>Ready on stand-by</span>
                <span className={styles.highlightGreen}>● 92% deployed</span>
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
                  placeholder="Search by name or email (#VOL-...), or phone..."
                  value={searchTerm}
                  onChange={(e) => setSearchTerm(e.target.value)}
                />
              </div>

              <div className={styles.filterPillsGroup}>
                {(["All", "Available", "Busy", "Inactive"] as const).map((status) => (
                  <button
                    key={status}
                    className={`${styles.filterPill} ${availabilityFilter === status ? styles.activeFilterPill : ""}`}
                    onClick={() => setAvailabilityFilter(status)}
                    type="button"
                  >
                    {status} {status === "All" && "1,420"} {status === "Available" && "890"} {status === "Busy" && "342"} {status === "Inactive" && "188"}
                  </button>
                ))}
              </div>
            </div>

            <div className={styles.pipelineHeader}>
              <div className={styles.pipelineTitle}>
                <span>Volunteer Registry & Deployment Pipeline</span>
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
                    <th>SKILLS</th>
                    <th>AVAILABILITY</th>
                    <th>DATE CREATED</th>
                    <th>ACTIONS</th>
                  </tr>
                </thead>
                <tbody>
                  {filteredVolunteers.map((vol) => (
                    <tr key={vol.id} onClick={() => setSelectedVolunteer(vol)}>
                      <td>
                        <div className={styles.identityCell}>
                          <div className={styles.avatarBadge}>👤</div>
                          <div className={styles.identityText}>
                            <span className={styles.identityName}>{vol.name}</span>
                            <span className={styles.identityCode}>{vol.code}</span>
                          </div>
                        </div>
                      </td>
                      <td>{vol.email}</td>
                      <td>{vol.phone}</td>
                      <td>
                        <div className={styles.skillBadgesWrap}>
                          {vol.skills.map((skill, idx) => (
                            <span key={idx} className={styles.skillPill}>
                              🛠 {skill}
                            </span>
                          ))}
                        </div>
                      </td>
                      <td>
                        <span
                          className={`${styles.statusBadge} ${
                            vol.availability === "Available"
                              ? styles.statusAvailable
                              : vol.availability === "Busy"
                              ? styles.statusBusy
                              : styles.statusInactive
                          }`}
                        >
                          <span className={styles.dot} />
                          {vol.availability}
                        </span>
                      </td>
                      <td>{vol.dateCreated}</td>
                      <td>
                        <div className={styles.actionLinks}>
                          <button
                            className={styles.editActionBtn}
                            onClick={(e) => {
                              e.stopPropagation();
                              setEditingVolunteer({ ...vol });
                            }}
                            type="button"
                          >
                            Edit
                          </button>
                          <button
                            className={styles.deleteActionBtn}
                            onClick={(e) => handleDelete(vol.id, e)}
                            type="button"
                          >
                            Delete
                          </button>
                        </div>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>

            <div className={styles.tableFooter}>
              <span>● Showing {filteredVolunteers.length} of 1,420 volunteer accounts</span>
              <div className={styles.footerActions}>
                <button className={styles.footerSecondaryBtn} type="button">˅ Load More Volunteers</button>
                <button className={styles.footerSecondaryBtn} type="button">⤓ Export CSV</button>
              </div>
            </div>
          </div>
        </div>
      </main>

      {/* Edit Volunteer Modal */}
      {editingVolunteer && (
        <div className={styles.modalBackdrop} onClick={() => setEditingVolunteer(null)}>
          <div className={styles.modalDialog} onClick={(e) => e.stopPropagation()}>
            <div className={styles.modalHeader}>
              <div className={styles.modalHeaderLeft}>
                <div className={styles.modalIconBadge}>👤</div>
                <div className={styles.modalTitleGroup}>
                  <h2>
                    <span>Edit Volunteer Details</span>
                    <span className={styles.modalIdPill}>{editingVolunteer.code}</span>
                  </h2>
                  <p className={styles.modalSubtitle}>Update contact information, skills, and emergency deployment availability.</p>
                </div>
              </div>
              <button className={styles.modalCloseBtn} onClick={() => setEditingVolunteer(null)} type="button">✕</button>
            </div>

            <form
              className={styles.modalForm}
              onSubmit={(e) => {
                e.preventDefault();
                setVolunteers((prev) => prev.map((v) => (v.id === editingVolunteer.id ? editingVolunteer : v)));
                setEditingVolunteer(null);
              }}
            >
              <div className={styles.formGridTwoCol}>
                <div className={styles.formField}>
                  <label>Full Name *</label>
                  <input
                    type="text"
                    required
                    value={editingVolunteer.name}
                    onChange={(e) => setEditingVolunteer({ ...editingVolunteer, name: e.target.value })}
                  />
                </div>
                <div className={styles.formField}>
                  <label>Email Address *</label>
                  <input
                    type="email"
                    required
                    value={editingVolunteer.email}
                    onChange={(e) => setEditingVolunteer({ ...editingVolunteer, email: e.target.value })}
                  />
                </div>
              </div>

              <div className={styles.formGridTwoCol}>
                <div className={styles.formField}>
                  <label>Phone Number *</label>
                  <input
                    type="text"
                    required
                    value={editingVolunteer.phone}
                    onChange={(e) => setEditingVolunteer({ ...editingVolunteer, phone: e.target.value })}
                  />
                </div>
                <div className={styles.formField}>
                  <label>Date Registered</label>
                  <input disabled value={editingVolunteer.dateCreated} />
                </div>
              </div>

              <div className={styles.formField}>
                <label>Skills & Capabilities (comma separated) *</label>
                <input
                  type="text"
                  required
                  value={editingVolunteer.skills.join(", ")}
                  onChange={(e) =>
                    setEditingVolunteer({
                      ...editingVolunteer,
                      skills: e.target.value.split(",").map((s) => s.trim()).filter(Boolean),
                    })
                  }
                />
              </div>

              <div className={styles.formField}>
                <label>Deployment Availability *</label>
                <select
                  value={editingVolunteer.availability}
                  onChange={(e) =>
                    setEditingVolunteer({ ...editingVolunteer, availability: e.target.value as VolunteerAvailability })
                  }
                >
                  <option value="Available">Available (Stand-by)</option>
                  <option value="Busy">Busy (Deployed)</option>
                  <option value="Inactive">Inactive</option>
                </select>
              </div>

              <div className={styles.modalFooter}>
                <button type="button" className={styles.modalDeleteBtn} onClick={() => handleDelete(editingVolunteer.id)}>
                  🗑 Delete
                </button>
                <div className={styles.modalFooterRight}>
                  <button type="button" className={styles.modalCancelBtn} onClick={() => setEditingVolunteer(null)}>Cancel</button>
                  <button type="submit" className={styles.modalPrimaryBtn}>✓ Save Changes</button>
                </div>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* Add New Volunteer Modal (Image 3) */}
      {isAddModalOpen && (
        <div className={styles.modalBackdrop} onClick={() => setIsAddModalOpen(false)}>
          <div className={styles.modalDialog} onClick={(e) => e.stopPropagation()}>
            <div className={styles.modalHeader}>
              <div className={styles.modalHeaderLeft}>
                <div className={styles.modalIconBadge}>+👥</div>
                <div className={styles.modalTitleGroup}>
                  <h2>Add New Volunteer</h2>
                  <p className={styles.modalSubtitle}>Register a new volunteer responder to the emergency relief pipeline</p>
                </div>
              </div>
              <button className={styles.modalCloseBtn} onClick={() => setIsAddModalOpen(false)} type="button">✕</button>
            </div>

            <form
              className={styles.modalForm}
              onSubmit={(e) => {
                e.preventDefault();
                const fd = new FormData(e.currentTarget);
                const skillsRaw = (fd.get("skills") as string) || "";
                const created: Volunteer = {
                  id: String(Date.now()),
                  code: `#VOL-2024-${Math.floor(1000 + Math.random() * 9000)}`,
                  name: fd.get("name") as string,
                  email: fd.get("email") as string,
                  phone: fd.get("phone") as string,
                  skills: skillsRaw.split(",").map((s) => s.trim()).filter(Boolean),
                  availability: fd.get("availability") as VolunteerAvailability,
                  dateCreated: "Today (Auto-generated)",
                };
                setVolunteers((prev) => [created, ...prev]);
                setIsAddModalOpen(false);
              }}
            >
              <div className={styles.formGridTwoCol}>
                <div className={styles.formField}>
                  <label>Full Name *</label>
                  <input name="name" required placeholder="e.g., Sarah Fernando" />
                </div>
                <div className={styles.formField}>
                  <label>Email Address *</label>
                  <input name="email" type="email" required placeholder="e.g., sarah.f@relief.lk" />
                </div>
              </div>

              <div className={styles.formGridTwoCol}>
                <div className={styles.formField}>
                  <label>Phone Number *</label>
                  <input name="phone" required placeholder="e.g., +94 77 000 0000" />
                </div>
                <div className={styles.formField}>
                  <label>Date Registered</label>
                  <input disabled value="Today (Auto-generated)" />
                </div>
              </div>

              <div className={styles.formField}>
                <label>Skills & Capabilities *</label>
                <input
                  name="skills"
                  required
                  placeholder="e.g., Paramedic, First Aid, Logistics, Heavy Vehicle Driver"
                />
                <span className={styles.fieldHelperText}>
                  Separate skills with commas (e.g. Triage, Water Sanitation, Heavy Transport)
                </span>
              </div>

              <div className={styles.formField}>
                <label>Deployment Availability *</label>
                <select name="availability" defaultValue="Available">
                  <option value="Available">Available (Stand-by)</option>
                  <option value="Busy">Busy (Deployed)</option>
                  <option value="Inactive">Inactive</option>
                </select>
              </div>

              <div className={styles.modalFooter}>
                <div className={styles.modalFooterRight}>
                  <button type="button" className={styles.modalCancelBtn} onClick={() => setIsAddModalOpen(false)}>Cancel</button>
                  <button type="submit" className={styles.modalPrimaryBtn}>+ Add Volunteer</button>
                </div>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}