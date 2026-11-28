"use client";

import Link from "next/link";
import { FormEvent, useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import {
  AUTH_SESSION_LOADING,
  clearSessionCredentials,
  useSessionRole,
  useSessionToken,
} from "../lib/auth-session";

const API_BASE_URL =
  (process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5001").replace(/\/+$/, "");

type Campaign = {
  id: string;
  name: string;
  targetAmount: number;
  isActive: boolean;
};

type Donation = {
  id: string;
  donorName: string;
  campaignName: string | null;
  type: string;
  amount: number;
  currency: string;
};

type Donor = {
  id: string;
  fullName: string;
  email: string;
  type: string;
  isActive: boolean;
};

type Volunteer = {
  id: string;
  fullName: string;
  email: string;
  availability: string;
  skills: string | null;
};

type VolunteerTask = {
  id: string;
  volunteerId: string | null;
  volunteerName: string | null;
  title: string;
  location: string | null;
  status: string;
  dueDate: string | null;
};

type Shelter = {
  id: string;
  name: string;
  capacity: number;
  currentOccupancy: number;
  latitude: number | null;
  longitude: number | null;
};

type Resource = {
  id: string;
  name: string;
  category: string;
  quantityAvailable: number;
  description: string | null;
  latitude: number | null;
  longitude: number | null;
};

export default function PortalPage() {
  const router = useRouter();
  const role = useSessionRole();
  const token = useSessionToken();
  const [error, setError] = useState("");
  const [notice, setNotice] = useState("");
  const [campaigns, setCampaigns] = useState<Campaign[]>([]);
  const [donations, setDonations] = useState<Donation[]>([]);
  const [donors, setDonors] = useState<Donor[]>([]);
  const [volunteers, setVolunteers] = useState<Volunteer[]>([]);
  const [tasks, setTasks] = useState<VolunteerTask[]>([]);
  const [shelters, setShelters] = useState<Shelter[]>([]);
  const [resources, setResources] = useState<Resource[]>([]);

  useEffect(() => {
    if (role === AUTH_SESSION_LOADING || token === AUTH_SESSION_LOADING) return;
    if (!token || !role) {
      router.replace("/login");
      return;
    }
    if (role === "Admin") {
      router.replace("/admin");
      return;
    }

    let active = true;
    async function load() {
      setError("");
      try {
        const authHeaders = { Authorization: `Bearer ${token}` };
        const requests: Promise<unknown>[] = [];

        if (role === "DonorOfficer") {
          requests.push(
            fetch(`${API_BASE_URL}/api/campaigns`, { headers: authHeaders }).then((r) => r.json()),
            fetch(`${API_BASE_URL}/api/donations`, { headers: authHeaders }).then((r) => r.json()),
            fetch(`${API_BASE_URL}/api/donors`, { headers: authHeaders }).then((r) => r.json()),
          );
        } else if (role === "VolunteerCoordinator") {
          requests.push(
            fetch(`${API_BASE_URL}/api/volunteers`, { headers: authHeaders }).then((r) => r.json()),
            fetch(`${API_BASE_URL}/api/volunteer-tasks`, { headers: authHeaders }).then((r) => r.json()),
          );
        } else if (role === "ShelterAdmin") {
          requests.push(fetch(`${API_BASE_URL}/api/shelters`).then((r) => r.json()));
        } else if (role === "ResourceManager" || role === "Coordinator" || role === "CaseManager") {
          requests.push(fetch(`${API_BASE_URL}/api/resources`).then((r) => r.json()));
        }

        const results = await Promise.all(requests);
        if (!active) return;
        if (role === "DonorOfficer") {
          setCampaigns(Array.isArray(results[0]) ? results[0] as Campaign[] : []);
          setDonations(Array.isArray(results[1]) ? results[1] as Donation[] : []);
          setDonors(Array.isArray(results[2]) ? results[2] as Donor[] : []);
        } else if (role === "VolunteerCoordinator") {
          setVolunteers(Array.isArray(results[0]) ? results[0] as Volunteer[] : []);
          setTasks(Array.isArray(results[1]) ? results[1] as VolunteerTask[] : []);
        } else if (role === "ShelterAdmin") {
          setShelters(Array.isArray(results[0]) ? results[0] as Shelter[] : []);
        } else if (role === "ResourceManager" || role === "Coordinator" || role === "CaseManager") {
          setResources(Array.isArray(results[0]) ? results[0] as Resource[] : []);
        }
      } catch (loadError) {
        if (active) {
          setError(loadError instanceof Error ? loadError.message : "Unable to load portal data.");
        }
      }
    }
    void load();
    return () => {
      active = false;
    };
  }, [role, router, token]);

  async function postJson(path: string, body: unknown) {
    const response = await fetch(`${API_BASE_URL}${path}`, {
      method: "POST",
      headers: {
        Authorization: `Bearer ${token}`,
        "Content-Type": "application/json",
      },
      body: JSON.stringify(body),
    });
    const responseBody: unknown = await response.json().catch(() => undefined);
    if (!response.ok) {
      const message =
        responseBody && typeof responseBody === "object"
          ? (responseBody as { detail?: unknown; error?: unknown; message?: unknown }).detail ??
            (responseBody as { error?: unknown }).error ??
            (responseBody as { message?: unknown }).message
          : undefined;
      throw new Error(typeof message === "string" ? message : `Request failed (${response.status}).`);
    }
    return responseBody;
  }

  async function createCampaign(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const form = new FormData(event.currentTarget);
    setError("");
    setNotice("");
    try {
      const created = await postJson("/api/campaigns", {
        name: String(form.get("name")),
        description: String(form.get("description") || ""),
        targetAmount: Number(form.get("targetAmount")),
        startDate: String(form.get("startDate")),
      }) as Campaign;
      setCampaigns((current) => [created, ...current]);
      setNotice("Campaign created.");
      event.currentTarget.reset();
    } catch (actionError) {
      setError(actionError instanceof Error ? actionError.message : "Unable to create campaign.");
    }
  }

  async function createTask(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const form = new FormData(event.currentTarget);
    setError("");
    setNotice("");
    try {
      const created = await postJson("/api/volunteer-tasks", {
        title: String(form.get("title")),
        description: String(form.get("description") || ""),
        location: String(form.get("location") || ""),
        dueDate: String(form.get("dueDate") || "") || null,
      }) as VolunteerTask;
      setTasks((current) => [created, ...current]);
      setNotice("Volunteer task created.");
      event.currentTarget.reset();
    } catch (actionError) {
      setError(actionError instanceof Error ? actionError.message : "Unable to create task.");
    }
  }

  async function createResource(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const form = new FormData(event.currentTarget);
    setError("");
    setNotice("");
    try {
      const created = await postJson("/api/resources", {
        id: "00000000-0000-0000-0000-000000000000",
        name: String(form.get("name")),
        category: String(form.get("category")),
        quantityAvailable: Number(form.get("quantity")),
        description: String(form.get("description") || ""),
        latitude: null,
        longitude: null,
      }) as Resource;
      setResources((current) => [created, ...current]);
      setNotice("Resource created.");
      event.currentTarget.reset();
    } catch (actionError) {
      setError(actionError instanceof Error ? actionError.message : "Unable to create resource.");
    }
  }

  function signOut() {
    clearSessionCredentials();
    router.replace("/login");
  }

  if (role === AUTH_SESSION_LOADING || token === AUTH_SESSION_LOADING) {
    return <main className="min-h-screen grid place-items-center bg-slate-50 text-slate-600">Checking account…</main>;
  }
  if (!token || !role) {
    return <main className="min-h-screen grid place-items-center bg-slate-50 text-slate-600">Redirecting…</main>;
  }

  return (
    <main className="min-h-screen bg-slate-50 px-4 py-8 text-slate-900 md:px-8">
      <div className="mx-auto max-w-6xl">
        <header className="mb-6 flex items-center justify-between gap-4">
          <div>
            <p className="text-sm font-semibold uppercase tracking-wide text-emerald-800">Account portal</p>
            <h1 className="mt-2 text-3xl font-bold">Welcome, {role}</h1>
          </div>
          <button className="rounded-xl border border-slate-300 px-4 py-2 font-semibold hover:bg-white" type="button" onClick={signOut}>Sign out</button>
        </header>

        {error && <p className="mb-4 rounded-xl bg-red-50 p-4 text-red-800" role="alert">{error}</p>}
        {notice && <p className="mb-4 rounded-xl bg-emerald-50 p-4 text-emerald-900" role="status">{notice}</p>}

        {role === "Citizen" && (
          <section className="rounded-2xl bg-white p-6 shadow-sm">
            <h2 className="text-xl font-semibold">Citizen services</h2>
            <p className="mt-2 text-slate-600">Submit and track humanitarian aid requests.</p>
            <Link className="mt-5 inline-block rounded-xl bg-emerald-800 px-5 py-3 font-semibold text-white" href="/dashboard">Open dashboard</Link>
          </section>
        )}

        {role === "Volunteer" && (
          <section className="rounded-2xl bg-white p-6 shadow-sm">
            <h2 className="text-xl font-semibold">Volunteer account</h2>
            <p className="mt-2 text-slate-600">Your approved account is active. Volunteer task assignment is managed by a Volunteer Coordinator.</p>
            <p className="mt-3 text-sm text-slate-500">Real-time personal task delivery is not yet exposed by the current API.</p>
          </section>
        )}

        {role === "Donor" && (
          <section className="rounded-2xl bg-white p-6 shadow-sm">
            <h2 className="text-xl font-semibold">Donor account</h2>
            <p className="mt-2 text-slate-600">Your approved account is active. Donation management is handled by a Donor Officer.</p>
          </section>
        )}

        {role === "DonorOfficer" && (
          <div className="grid gap-6">
            <section className="rounded-2xl bg-white p-6 shadow-sm">
              <h2 className="text-xl font-semibold">Create campaign</h2>
              <form className="mt-4 grid gap-3 md:grid-cols-2" onSubmit={createCampaign}>
                <input className="rounded-lg border border-slate-300 px-3 py-2" name="name" required placeholder="Campaign name" />
                <input className="rounded-lg border border-slate-300 px-3 py-2" name="targetAmount" required type="number" min="0.01" step="0.01" placeholder="Target amount" />
                <input className="rounded-lg border border-slate-300 px-3 py-2" name="startDate" required type="date" />
                <input className="rounded-lg border border-slate-300 px-3 py-2" name="description" placeholder="Description" />
                <button className="w-fit rounded-lg bg-emerald-800 px-4 py-2 font-semibold text-white" type="submit">Create campaign</button>
              </form>
            </section>
            <section className="grid gap-6 md:grid-cols-3">
              <article className="rounded-2xl bg-white p-5 shadow-sm"><h3 className="font-semibold">Campaigns</h3>{campaigns.map((item) => <p className="mt-3 text-sm" key={item.id}>{item.name} · {item.targetAmount}</p>)}</article>
              <article className="rounded-2xl bg-white p-5 shadow-sm"><h3 className="font-semibold">Donations</h3>{donations.map((item) => <p className="mt-3 text-sm" key={item.id}>{item.donorName}: {item.amount} {item.currency}</p>)}</article>
              <article className="rounded-2xl bg-white p-5 shadow-sm"><h3 className="font-semibold">Donors</h3>{donors.map((item) => <p className="mt-3 text-sm" key={item.id}>{item.fullName} · {item.type}</p>)}</article>
            </section>
          </div>
        )}

        {role === "VolunteerCoordinator" && (
          <div className="grid gap-6">
            <section className="rounded-2xl bg-white p-6 shadow-sm">
              <h2 className="text-xl font-semibold">Create volunteer task</h2>
              <form className="mt-4 grid gap-3 md:grid-cols-2" onSubmit={createTask}>
                <input className="rounded-lg border border-slate-300 px-3 py-2" name="title" required placeholder="Task title" />
                <input className="rounded-lg border border-slate-300 px-3 py-2" name="location" placeholder="Location" />
                <input className="rounded-lg border border-slate-300 px-3 py-2" name="dueDate" type="datetime-local" />
                <input className="rounded-lg border border-slate-300 px-3 py-2" name="description" placeholder="Description" />
                <button className="w-fit rounded-lg bg-emerald-800 px-4 py-2 font-semibold text-white" type="submit">Create task</button>
              </form>
            </section>
            <section className="grid gap-6 md:grid-cols-2">
              <article className="rounded-2xl bg-white p-5 shadow-sm"><h3 className="font-semibold">Volunteers</h3>{volunteers.map((item) => <p className="mt-3 text-sm" key={item.id}>{item.fullName} · {item.availability}</p>)}</article>
              <article className="rounded-2xl bg-white p-5 shadow-sm"><h3 className="font-semibold">Tasks</h3>{tasks.map((item) => <p className="mt-3 text-sm" key={item.id}>{item.title} · {item.status}</p>)}</article>
            </section>
          </div>
        )}

        {role === "ShelterAdmin" && (
          <div className="grid gap-6">
            <section
              id="shelter-history"
              className="rounded-2xl bg-white p-6 shadow-sm"
            >
              <h2 className="text-xl font-semibold">Shelter History</h2>
              {shelters.length === 0 ? (
                <p className="mt-3 text-slate-600">No shelters are available.</p>
              ) : (
                shelters.map((item) => (
                  <p className="mt-3 text-sm" key={item.id}>
                    {item.name}: {item.currentOccupancy}/{item.capacity}
                  </p>
                ))
              )}
            </section>
          </div>
        )}

        {(role === "ResourceManager" || role === "Coordinator" || role === "CaseManager") && (
          <div className="grid gap-6">
            {role === "ResourceManager" && (
              <section className="rounded-2xl bg-white p-6 shadow-sm">
                <h2 className="text-xl font-semibold">Create resource</h2>
                <form className="mt-4 grid gap-3 md:grid-cols-2" onSubmit={createResource}>
                  <input className="rounded-lg border border-slate-300 px-3 py-2" name="name" required placeholder="Resource name" />
                  <input className="rounded-lg border border-slate-300 px-3 py-2" name="category" required placeholder="Category" />
                  <input className="rounded-lg border border-slate-300 px-3 py-2" name="quantity" required type="number" min="0" placeholder="Available quantity" />
                  <input className="rounded-lg border border-slate-300 px-3 py-2" name="description" placeholder="Description" />
                  <button className="w-fit rounded-lg bg-emerald-800 px-4 py-2 font-semibold text-white" type="submit">Create resource</button>
                </form>
              </section>
            )}
            <section className="rounded-2xl bg-white p-6 shadow-sm">
              <h2 className="text-xl font-semibold">Resources</h2>
              {resources.length === 0 ? <p className="mt-3 text-slate-600">No resources are available.</p> : resources.map((item) => (
                <p className="mt-3 text-sm" key={item.id}>{item.name}: {item.quantityAvailable} available</p>
              ))}
              <Link className="mt-5 inline-block rounded-xl bg-emerald-800 px-5 py-3 font-semibold text-white" href="/dashboard">Open staff dashboard</Link>
            </section>
          </div>
        )}

        {role !== "Citizen" && role !== "Volunteer" && role !== "Donor" && role !== "DonorOfficer" && role !== "VolunteerCoordinator" && role !== "ShelterAdmin" && role !== "ResourceManager" && role !== "Coordinator" && role !== "CaseManager" && (
          <section className="rounded-2xl bg-white p-6 shadow-sm">
            <h2 className="text-xl font-semibold">Account active</h2>
            <p className="mt-2 text-slate-600">Your role has no dedicated management screen yet.</p>
          </section>
        )}
      </div>
    </main>
  );
}
