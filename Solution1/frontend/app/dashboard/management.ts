export type PendingApplication = {
  id: string;
  fullName: string;
  email: string;
  accountType: string;
  phone: string | null;
  skills: string | null;
  donorType: string | null;
  createdAt: string;
};

export type DonorRecord = {
  id: string;
  fullName: string;
  email: string;
  phone: string | null;
  type: "Individual" | "Corporate";
  isActive: boolean;
  createdAt: string;
};

export type DonationRecord = {
  id: string;
  donorId: string;
  donorName: string;
  campaignId: string | null;
  campaignName: string | null;
  type: string;
  amount: number;
  currency: string;
  description: string | null;
  donatedAt: string;
};

export type VolunteerRecord = {
  id: string;
  fullName: string;
  email: string;
  phone: string | null;
  skills: string | null;
  availability: "Available" | "Busy" | "Inactive";
  createdAt: string;
};

export type VolunteerTaskRecord = {
  id: string;
  volunteerId: string | null;
  volunteerName: string | null;
  title: string;
  description: string | null;
  location: string | null;
  status: string;
  dueDate: string | null;
  createdAt: string;
};

export function isRecord(value: unknown): value is Record<string, unknown> {
  return value !== null && typeof value === "object" && !Array.isArray(value);
}

export function isPendingApplication(value: unknown): value is PendingApplication {
  return (
    isRecord(value) &&
    typeof value.id === "string" &&
    typeof value.fullName === "string" &&
    typeof value.email === "string" &&
    typeof value.accountType === "string" &&
    (value.phone === null || typeof value.phone === "string") &&
    (value.skills === null || typeof value.skills === "string") &&
    (value.donorType === null || typeof value.donorType === "string") &&
    typeof value.createdAt === "string"
  );
}

export function isDonorRecord(value: unknown): value is DonorRecord {
  return (
    isRecord(value) &&
    typeof value.id === "string" &&
    typeof value.fullName === "string" &&
    typeof value.email === "string" &&
    (value.phone === null || typeof value.phone === "string") &&
    (value.type === "Individual" || value.type === "Corporate") &&
    typeof value.isActive === "boolean" &&
    typeof value.createdAt === "string"
  );
}

export function isDonationRecord(value: unknown): value is DonationRecord {
  return (
    isRecord(value) &&
    typeof value.id === "string" &&
    typeof value.donorId === "string" &&
    typeof value.donorName === "string" &&
    (value.campaignId === null || typeof value.campaignId === "string") &&
    (value.campaignName === null || typeof value.campaignName === "string") &&
    typeof value.type === "string" &&
    typeof value.amount === "number" &&
    typeof value.currency === "string" &&
    (value.description === null || typeof value.description === "string") &&
    typeof value.donatedAt === "string"
  );
}

export function isVolunteerRecord(value: unknown): value is VolunteerRecord {
  return (
    isRecord(value) &&
    typeof value.id === "string" &&
    typeof value.fullName === "string" &&
    typeof value.email === "string" &&
    (value.phone === null || typeof value.phone === "string") &&
    (value.skills === null || typeof value.skills === "string") &&
    (value.availability === "Available" ||
      value.availability === "Busy" ||
      value.availability === "Inactive") &&
    typeof value.createdAt === "string"
  );
}

export function isVolunteerTaskRecord(value: unknown): value is VolunteerTaskRecord {
  return (
    isRecord(value) &&
    typeof value.id === "string" &&
    (value.volunteerId === null || typeof value.volunteerId === "string") &&
    (value.volunteerName === null || typeof value.volunteerName === "string") &&
    typeof value.title === "string" &&
    (value.description === null || typeof value.description === "string") &&
    (value.location === null || typeof value.location === "string") &&
    typeof value.status === "string" &&
    (value.dueDate === null || typeof value.dueDate === "string") &&
    typeof value.createdAt === "string"
  );
}

export function requireRows<T>(
  value: unknown,
  isItem: (item: unknown) => item is T,
  responseName: string,
): T[] {
  if (!Array.isArray(value) || !value.every(isItem)) {
    throw new Error(`The server returned an invalid ${responseName} response.`);
  }
  return value;
}

export function formatDate(value: string) {
  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? "Unknown" : date.toLocaleDateString();
}

export function formatCurrency(amount: number, currency = "LKR") {
  return new Intl.NumberFormat(undefined, {
    style: "currency",
    currency,
    maximumFractionDigits: 2,
  }).format(amount);
}

export function errorMessage(error: unknown, fallback: string) {
  return error instanceof Error ? error.message : fallback;
}
