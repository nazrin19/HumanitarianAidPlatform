import Link from "next/link";
import Image from "next/image";
import styles from "./page.module.css";

const accountTypes = [
  {
    title: "Citizen",
    description: "Create an account to request and track humanitarian aid.",
    href: "/signup/citizen",
  },
  {
    title: "Volunteer",
    description: "Apply to support relief efforts in your community.",
    href: "/apply/volunteer",
  },
  {
    title: "Donor",
    description: "Apply to contribute resources to relief efforts.",
    href: "/apply/donor",
  },
];

export default function SignupPage() {
  return (
    <main className={`${styles.page} min-h-screen flex items-center justify-center p-4 md:p-6`}>
      <div className={`${styles.shell} w-full max-w-270`}>
        <section className={styles.left}>
          <div className={styles.brand}>
            <Image
              className={styles.logo}
              src="/humanitarian-aid-platform-logo.svg"
              alt="Humanitarian Aid Platform logo"
              width={50}
              height={50}
            />
            <span>Humanitarian Aid Platform</span>
          </div>

          <div className={styles.art}>
            <Image
              src="/signup-community.jpg"
              alt="Volunteers carrying a supply box, planting a young tree together, and reaching out to help"
              width={940}
              height={640}
              className={styles.artImage}
              sizes="(max-width: 900px) 0px, 45vw"
            />
          </div>
          <h2>Together, we can make a difference</h2>
          <p>
            Join our community to help distribute aid, manage shelters, and
            support those in need.
          </p>
          <div className={styles.dots} aria-hidden="true">
            <i />
            <i />
            <i />
          </div>
        </section>

        <section className={styles.right}>
          <p className={styles.have}>
            Already have an account? <Link href="/login">Login</Link>
          </p>

          <div className="mx-auto my-auto w-full max-w-125 pt-3">
            <h1 className="text-center text-3xl font-bold md:text-4xl">
              Choose an account type
            </h1>
            <p className="mt-3 text-center text-slate-600">
              Select the account that matches how you want to use the platform.
            </p>
            <div className="mt-8 grid gap-4 md:grid-cols-3">
              {accountTypes.map((account) => (
                <Link
                  key={account.href}
                  href={account.href}
                  className="rounded-2xl border border-slate-200 p-6 transition hover:border-emerald-700 hover:bg-emerald-50 focus-visible:outline-2 focus-visible:outline-emerald-700"
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
          </div>
        </section>
      </div>
    </main>
  );
}
