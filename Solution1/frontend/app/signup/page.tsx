import Link from "next/link";

const accountTypes = [
  {
    title: "Citizen",
    description: "Create an account to request and track humanitarian aid.",
    href: "/signup/citizen",
  },
  {
    title: "Volunteer",
    description: "Apply to support relief efforts in your community.",
    href: "/signup/volunteer",
  },
  {
    title: "Donor",
    description: "Apply to contribute resources to relief efforts.",
    href: "/signup/donor",
  },
];

export default function SignupPage() {
  return (
    <main className="flex min-h-screen items-center justify-center bg-slate-50 p-6 text-slate-900">
      <section className="w-full max-w-4xl rounded-3xl bg-white p-8 shadow-xl md:p-12">
        <p className="mb-3 text-sm font-semibold uppercase tracking-wide text-emerald-800">
          Humanitarian Aid Platform
        </p>
        <h1 className="text-3xl font-bold md:text-4xl">Choose an account type</h1>
        <p className="mt-3 text-slate-600">
          Select the account that matches how you want to use the platform.
        </p>
        <div className="mt-8 grid gap-4 md:grid-cols-3">
          {accountTypes.map((account) => (
            <Link
              key={account.href}
              href={account.href}
              className="rounded-2xl border border-slate-200 p-6 transition hover:border-emerald-700 hover:bg-emerald-50 focus-visible:outline focus-visible:outline-2 focus-visible:outline-emerald-700"
            >
              <h2 className="text-xl font-semibold">{account.title}</h2>
              <p className="mt-2 text-sm leading-6 text-slate-600">
                {account.description}
              </p>
              <span className="mt-5 inline-block font-semibold text-emerald-800">
                Continue →
              </span>
            </Link>
          ))}
        </div>
        <p className="mt-8 text-sm text-slate-600">
          Already have an account?{" "}
          <Link className="font-semibold text-emerald-800 hover:underline" href="/login">
            Log in
          </Link>
        </p>
      </section>
    </main>
  );
}
