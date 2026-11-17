"use client";

import Link from "next/link";
import { useEffect } from "react";
import { useRouter } from "next/navigation";
import {
  AUTH_SESSION_LOADING,
  clearSessionCredentials,
  useSessionRole,
  useSessionToken,
} from "../lib/auth-session";

export default function PortalPage() {
  const router = useRouter();
  const role = useSessionRole();
  const token = useSessionToken();

  useEffect(() => {
    if (role === AUTH_SESSION_LOADING || token === AUTH_SESSION_LOADING) {
      return;
    }
    if (!token || !role) {
      router.replace("/login");
      return;
    }

    if (role === "Admin") {
      router.replace("/admin");
    }
  }, [role, router, token]);

  function signOut() {
    clearSessionCredentials();
    router.replace("/login");
  }

  const message = role === AUTH_SESSION_LOADING
    ? "Checking your account…"
    :
    role === "Volunteer"
      ? "Your approved volunteer account is active. Volunteer task and schedule management are not available in the frontend yet."
      : role === "Donor"
        ? "Your approved donor account is active. Donation and contribution management are not available in the frontend yet."
        : "Your account is active, but this role-specific management workspace has not been connected to the frontend yet.";

  return (
    <main className="flex min-h-screen items-center justify-center bg-slate-50 p-6 text-slate-900">
      <section className="w-full max-w-2xl rounded-3xl bg-white p-8 shadow-xl md:p-12">
        <p className="text-sm font-semibold uppercase tracking-wide text-emerald-800">
          Account portal
        </p>
        <h1 className="mt-3 text-3xl font-bold">Welcome{role && role !== AUTH_SESSION_LOADING ? `, ${role}` : ""}</h1>
        <p className="mt-4 leading-7 text-slate-600" role="status">
          {message}
        </p>
        <div className="mt-8 flex flex-wrap gap-4">
          {role === "Citizen" && (
            <Link
              className="rounded-xl bg-emerald-800 px-5 py-3 font-semibold text-white hover:bg-emerald-900"
              href="/dashboard"
            >
              Open dashboard
            </Link>
          )}
          <button
            className="rounded-xl border border-slate-300 px-5 py-3 font-semibold hover:bg-slate-50"
            type="button"
            onClick={signOut}
          >
            Sign out
          </button>
        </div>
      </section>
    </main>
  );
}
