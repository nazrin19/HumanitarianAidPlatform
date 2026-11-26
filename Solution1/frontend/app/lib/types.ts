export type DonorType = "Individual" | "Corporate";
export type DonorStatus = "Active" | "Inactive";
export type VolunteerAvailability = "Available" | "Busy" | "Inactive";

export interface DonationRecord {
  id: string;
  title: string;
  amount: string;
  date: string;
}

export interface Donor {
  id: string;
  code: string;
  name: string;
  email: string;
  phone: string;
  type: DonorType;
  status: DonorStatus;
  dateCreated: string;
  totalGiven?: string;
  recentDonations?: DonationRecord[];
}

export interface Volunteer {
  id: string;
  code: string;
  name: string;
  email: string;
  phone: string;
  skills: string[];
  availability: VolunteerAvailability;
  dateCreated: string;
  joinedDate?: string;
}

export type ActiveTab = "donors" | "volunteers" | "management";
export type ModalType = "addDonor" | "editDonor" | "addVolunteer" | null;