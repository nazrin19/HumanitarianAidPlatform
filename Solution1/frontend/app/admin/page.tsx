"use client";

import { FormEvent, useCallback, useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import {
  AUTH_SESSION_LOADING,
  clearSessionCredentials,
  useSessionRole,
  useSessionToken,
} from "../lib/auth-session";

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
];

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
    throw new Error(
      getErrorMessage(body, `Unable to load applications (${response.status}).`),
    );
  }
  if (!Array.isArray(body) || !body.every(isPendingApplication)) {
    throw new Error("The server returned an invalid applications response.");
  }
  return body;
}

export default function AdminPage() {
  const router = useRouter();
  const token = useSessionToken();
  const accountRole = useSessionRole();
  const [applications, setApplications] = useState<PendingApplication[]>([]);
  const [error, setError] = useState("");
  const [notice, setNotice] = useState("");
  const [busyId, setBusyId] = useState("");
  const [isLoading, setIsLoading] = useState(true);
  const [fullName, setFullName] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [role, setRole] = useState(STAFF_ROLES[0]);
  const [isCreating, setIsCreating] = useState(false);

  const refresh = useCallback(async (accessToken: string) => {
    setError("");
    setIsLoading(true);
    try {
      setApplications(await loadPendingApplications(accessToken));
    } catch (loadError) {
      setError(
        loadError instanceof Error
          ? loadError.message
          : "Unable to load applications.",
      );
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    if (token === AUTH_SESSION_LOADING || accountRole === AUTH_SESSION_LOADING) {
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
              : "Unable to load applications.",
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
        throw new Error(
          getErrorMessage(body, `Unable to ${decision} this application (${response.status}).`),
        );
      }
      setNotice(
        `${application.fullName}'s application was ${
          decision === "approve" ? "approved" : "rejected"
        }.`,
      );
      await refresh(token);
    } catch (actionError) {
      setError(
        actionError instanceof Error
          ? actionError.message
          : `Unable to ${decision} this application.`,
      );
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
        body: JSON.stringify({
          fullName: fullName.trim(),
          email: email.trim(),
          password,
          role,
        }),
      });
      const body: unknown = await response.json().catch(() => undefined);
      if (!response.ok) {
        throw new Error(
          getErrorMessage(body, `Unable to create staff account (${response.status}).`),
        );
      }

      setNotice(`Staff account created for ${email.trim()}.`);
      setFullName("");
      setEmail("");
      setPassword("");
    } catch (createError) {
      setError(
        createError instanceof Error
          ? createError.message
          : "Unable to create staff account.",
      );
    } finally {
      setIsCreating(false);
    }
  }

  function signOut() {
    clearSessionCredentials();
    router.replace("/login");
  }

  if (token === AUTH_SESSION_LOADING || accountRole === AUTH_SESSION_LOADING) {
    return (
      <main className="flex min-h-screen items-center justify-center bg-slate-50 text-slate-700" role="status">
        Checking administrator access…
      </main>
    );
  }

  if (!token || accountRole !== "Admin") {
    return (
      <main className="flex min-h-screen items-center justify-center bg-slate-50 text-slate-700" role="status">
        Redirecting…
      </main>
    );
  }

  return (
    <main className="min-h-screen bg-slate-50 px-4 py-8 text-slate-900 md:px-8">
      <div className="mx-auto max-w-6xl">
        <header className="mb-8 flex flex-wrap items-center justify-between gap-4">
          <div>
            <p className="text-sm font-semibold uppercase tracking-wide text-emerald-800">
              Humanitarian Aid Platform
            </p>
            <h1 className="mt-2 text-3xl font-bold">Administrator workspace</h1>
          </div>
          <button
            className="rounded-xl border border-slate-300 px-4 py-2 font-semibold hover:bg-white"
            type="button"
            onClick={signOut}
          >
            Sign out
          </button>
        </header>

        {error && <p className="mb-4 rounded-xl bg-red-50 p-4 text-red-800" role="alert">{error}</p>}
        {notice && <p className="mb-4 rounded-xl bg-emerald-50 p-4 text-emerald-900" role="status">{notice}</p>}

        <section className="rounded-2xl bg-white p-6 shadow-sm" aria-labelledby="applications-heading">
          <div className="mb-5 flex flex-wrap items-center justify-between gap-3">
            <div>
              <h2 className="text-xl font-semibold" id="applications-heading">Pending applications</h2>
              <p className="mt-1 text-sm text-slate-600">Approve or reject Volunteer and Donor applications.</p>
            </div>
            <button
              className="rounded-lg border border-slate-300 px-4 py-2 text-sm font-semibold hover:bg-slate-50"
              type="button"
              disabled={!token || isLoading}
              onClick={() => void refresh(token)}
            >
              Refresh
            </button>
          </div>
          {isLoading ? (
            <p role="status">Loading applications…</p>
          ) : applications.length === 0 ? (
            <p className="text-slate-600">There are no pending applications.</p>
          ) : (
            <ul className="divide-y divide-slate-200">
              {applications.map((application) => (
                <li className="flex flex-wrap items-center justify-between gap-4 py-5" key={application.id}>
                  <div>
                    <h3 className="font-semibold">{application.fullName} <span className="font-normal text-slate-600">({application.accountType})</span></h3>
                    <p className="mt-1 text-sm text-slate-600">{application.email}{application.phone ? ` · ${application.phone}` : ""}</p>
                    {application.skills && <p className="mt-1 text-sm text-slate-600">Skills: {application.skills}</p>}
                    {application.donorType && <p className="mt-1 text-sm text-slate-600">Donor type: {application.donorType}</p>}
                    <p className="mt-1 text-xs text-slate-500">Applied {new Date(application.createdAt).toLocaleString()}</p>
                  </div>
                  <div className="flex gap-2">
                    <button
                      className="rounded-lg bg-emerald-800 px-4 py-2 text-sm font-semibold text-white disabled:opacity-50"
                      type="button"
                      disabled={busyId === application.id}
                      onClick={() => void decide(application, "approve")}
                    >
                      Approve
                    </button>
                    <button
                      className="rounded-lg border border-red-300 px-4 py-2 text-sm font-semibold text-red-800 disabled:opacity-50"
                      type="button"
                      disabled={busyId === application.id}
                      onClick={() => void decide(application, "reject")}
                    >
                      Reject
                    </button>
                  </div>
                </li>
              ))}
            </ul>
          )}
        </section>

        <section className="mt-8 rounded-2xl bg-white p-6 shadow-sm" aria-labelledby="staff-heading">
          <h2 className="text-xl font-semibold" id="staff-heading">Create staff account</h2>
          <p className="mt-1 text-sm text-slate-600">Admin accounts are not created from this form.</p>
          <form className="mt-5 grid gap-4 md:grid-cols-2" onSubmit={createStaffAccount}>
            <label className="grid gap-1 text-sm font-medium">
              Full name
              <input className="rounded-lg border border-slate-300 px-3 py-2" required maxLength={100} value={fullName} onChange={(event) => setFullName(event.target.value)} />
            </label>
            <label className="grid gap-1 text-sm font-medium">
              Email
              <input className="rounded-lg border border-slate-300 px-3 py-2" required type="email" maxLength={256} value={email} onChange={(event) => setEmail(event.target.value)} />
            </label>
            <label className="grid gap-1 text-sm font-medium">
              Password (at least 12 characters)
              <input className="rounded-lg border border-slate-300 px-3 py-2" required type="password" minLength={12} maxLength={100} autoComplete="new-password" value={password} onChange={(event) => setPassword(event.target.value)} />
            </label>
            <label className="grid gap-1 text-sm font-medium">
              Staff role
              <select className="rounded-lg border border-slate-300 px-3 py-2" value={role} onChange={(event) => setRole(event.target.value)}>
                {STAFF_ROLES.map((staffRole) => <option key={staffRole}>{staffRole}</option>)}
              </select>
            </label>
            <button
              className="w-fit rounded-xl bg-emerald-800 px-5 py-3 font-semibold text-white disabled:opacity-50 md:col-span-2"
              type="submit"
              disabled={isCreating || !token}
            >
              {isCreating ? "Creating…" : "Create staff account"}
            </button>
          </form>
        </section>
      </div>
    </main>
  );
}
