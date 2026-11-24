import Image from "next/image";
import Link from "next/link";
import RequestAidForm from "./components/RequestAidForm";
import styles from "./page.module.css";

export default function Home() {
  return (
    <div className={styles.page}>
      <title>HumanitarianAidPlatform · Home</title>
      <header className={styles.header}>
        <div className={`${styles.wrap} ${styles.nav}`}>
          <Link
            className={styles.brand}
            href="#top"
            aria-label="Humanitarian Aid Platform home"
          >
            <Image
              className={styles.logo}
              src="/humanitarian-aid-platform-logo.svg"
              alt="Humanitarian Aid Platform logo"
              width={50}
              height={50}
            />
            <span>Humanitarian Aid Platform</span>
          </Link>

          <nav className={styles.links} aria-label="Main">
            <Link href="#requests">Request help</Link>
            <Link href="#impact">How aid is coordinated</Link>
            <Link href="#about">About Us</Link>
            <Link href="#involved">Get Involved</Link>
          </nav>

          <div className={styles.right}>
            <Link className={`${styles.button} ${styles.solid} ${styles.small}`} href="#requests">
              Request help
            </Link>
            <Link className={styles.login} href="/login">
              Login
            </Link>
          </div>
        </div>
      </header>

      <main id="top" className={styles.main}>
        <section className={`${styles.wrap} ${styles.hero}`}>
          <div>
            <h1>Empowering Communities, Directing Support.</h1>
            <p className={styles.lead}>
              Join a global network of volunteers and organizations coordinating
              disaster relief, review assistance requests, and coordinate aid
              where it is needed most.
            </p>
            <div className={styles.row}>
              <Link className={`${styles.button} ${styles.solid}`} href="#requests">
                Ask for help
              </Link>
              <Link className={`${styles.button} ${styles.outline}`} href="#about">
                Learn More
              </Link>
            </div>
            <div className={styles.chip}>
              <Image
                className={styles.chipImage}
                src="/homepage/home-community.jpg"
                alt="Four smiling community members"
                width={390}
                height={138}
              />
              <span>Active Community</span>
            </div>
            <div className={styles.how}>
              <h3>How it Works</h3>
              <div className={styles.mini}>
                <Link href="#requests">
                  <Image
                    src="/homepage/home-request-help.jpg"
                    alt=""
                    width={240}
                    height={234}
                  />
                  Request Help
                </Link>
                <Link href="#impact">
                  <Image
                    src="/homepage/home-coordinate-aid.jpg"
                    alt=""
                    width={354}
                    height={240}
                  />
                  Coordinate Aid
                </Link>
                <Link href="#impact">
                  <Image
                    src="/homepage/home-impact-track.jpg"
                    alt=""
                    width={324}
                    height={240}
                  />
                  Impact Track
                </Link>
              </div>
            </div>
          </div>

          <div className={styles.art}>
            <Image
              className={styles.artImage}
              src="/homepage/home-hero.jpg"
              alt={
                "Volunteers unloading a supply truck, packing food and medical boxes" +
                " by a shelter tent, and handing over supplies"
              }
              width={1112}
              height={1076}
              sizes="(max-width: 1000px) 100vw, 50vw"
              priority
            />
          </div>
        </section>

        <section className={`${styles.layer} ${styles.tint}`} id="requests">
          <div className={styles.wrap}>
            <span className={styles.eyebrow}>REQUEST HELP</span>
            <h2>Tell us what you need</h2>
            <p className={styles.sub}>
              Choose a category, tell us where you are, and describe your
              situation. A Citizen account is required to submit a request.
            </p>
            <RequestAidForm embedded />
          </div>
        </section>

        <section className={styles.layer} id="impact">
          <div className={styles.wrap}>
            <span className={styles.eyebrow}>OUR APPROACH</span>
            <h2>Every request is reviewed by people</h2>
            <p className={styles.sub}>
              Your request is saved securely, then a coordinator reviews it.
              Any automated urgency recommendation is a guide for staff, not an
              automatic decision about who receives help.
            </p>

            <div className={styles.values}>
              <div>
                <i>1</i>
                <div>
                  <strong>Submit</strong>
                  <p>Share the kind of help you need and a location where the team can reach you.</p>
                </div>
              </div>
              <div>
                <i>2</i>
                <div>
                  <strong>Review</strong>
                  <p>A coordinator reviews the request and its urgency recommendation.</p>
                </div>
              </div>
              <div>
                <i>3</i>
                <div>
                  <strong>Coordinate</strong>
                  <p>The team follows up to plan next steps. Submitting a request does not guarantee immediate assistance.</p>
                </div>
              </div>
            </div>
          </div>
        </section>

        <section className={`${styles.layer} ${styles.tint}`} id="about">
          <div className={`${styles.wrap} ${styles.about}`}>
            <div className={styles.picture}>
              <Image
                src="/homepage/home-about.jpg"
                alt={
                  "Volunteers carrying a supply box, planting a young tree together," +
                  " and reaching out to help"
                }
                width={940}
                height={640}
                sizes="(max-width: 1000px) 100vw, 50vw"
              />
            </div>
            <div>
              <span className={styles.eyebrow}>ABOUT US</span>
              <h2>Built for the people on the ground</h2>
              <p className={`${styles.sub} ${styles.aboutSub}`}>
                HumanitarianAidPlatform brings requests, shelters, supplies and
                volunteers into one calm, clear workspace so response teams can
                spend less time chasing information and more time helping.
              </p>
              <div className={styles.values}>
                <div>
                  <i>1</i>
                  <div>
                    <strong>Prioritise</strong>
                    <p>
                      A rule-based urgency recommendation helps coordinators
                      review requests; a person makes the decision.
                    </p>
                  </div>
                </div>
                <div>
                  <i>2</i>
                  <div>
                    <strong>Coordinate</strong>
                    <p>Staff can review requests, shelters and available resources.</p>
                  </div>
                </div>
                <div>
                  <i>3</i>
                  <div>
                    <strong>Track</strong>
                    <p>Citizens can sign in to view the status of their submitted requests.</p>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </section>

        <section className={styles.layer} id="involved">
          <div className={styles.wrap}>
            <span className={styles.eyebrow}>GET INVOLVED</span>
            <h2>There’s a place for you</h2>
            <p className={styles.sub}>
              Whether you have time, resources or a whole organisation behind
              you, you can help.
            </p>
            <div className={styles.joins}>
              <article className={styles.join}>
                <div className={styles.tile}>
                  <svg viewBox="0 0 24 24" aria-hidden="true">
                    <path d="M20.8 5.6a5 5 0 0 0-7.100 0L12 7.300l-1.700-1.700a5 5 0 0 0-7.100 7.100L12 21.500l8.800-8.800a5 5 0 0 0 0-7.100z" />
                  </svg>
                </div>
                <h3>Volunteer</h3>
                <p>
                  Apply to join the response community. Applications are
                  reviewed before an account is activated.
                </p>
                <Link
                  className={`${styles.button} ${styles.solid} ${styles.small}`}
                  href="/signup/volunteer"
                >
                  Join Us
                </Link>
              </article>
              <article className={styles.join}>
                <div className={styles.tile}>
                  <svg viewBox="0 0 24 24" aria-hidden="true">
                    <path
                      d="M21 8V5a1 1 0 0 0-1-1H4a1 1 0 0 0-1 1v3M3 8h18v12a1 1 0 0 1-1 1H4a1 1 0 0 1-1-1zM12 8v13M8 4c0 2 4 4 4 4s4-2 4-4"
                    />
                  </svg>
                </div>
                <h3>Donate</h3>
                <p>Apply for a donor account and support relief campaigns.</p>
                <Link
                  className={`${styles.button} ${styles.solid} ${styles.small}`}
                  href="/signup/donor"
                >
                  Join Us
                </Link>
              </article>
              <article className={styles.join}>
                <div className={styles.tile}>
                  <svg viewBox="0 0 24 24" aria-hidden="true">
                    <path d="M3 21V8l9-5 9 5v13M9 21v-6h6v6M3 21h18" />
                  </svg>
                </div>
                <h3>Partner organisation</h3>
                <p>Contact the platform administrator to arrange a partner organisation account.</p>
              </article>
            </div>
          </div>
        </section>
      </main>

      <footer className={styles.footer}>
        <div className={styles.wrap}>
          <div className={styles.footerGrid}>
            <div>
              <div className={styles.footerBrand}>
                <Image
                  className={styles.footerLogo}
                  src="/humanitarian-aid-platform-logo.svg"
                  alt=""
                  width={48}
                  height={48}
                />
                <span>Humanitarian Aid Platform</span>
              </div>
              <p>
                Empowering communities and directing support where it is needed
                most.
              </p>
            </div>
            <div>
              <h5>Site</h5>
              <div className={styles.footerLinks}>
                <Link href="#requests">Request help</Link>
                <Link href="#impact">Our Impact</Link>
                <Link href="#about">About Us</Link>
                <Link href="#involved">Get Involved</Link>
              </div>
            </div>
            <div>
              <h5>Newsletter</h5>
              <p>Create a platform account to receive updates through your role-specific portal.</p>
              <Link className={styles.newsletterLink} href="/signup">Create an account</Link>
            </div>
          </div>
          <div className={styles.copyright}>
            © 2026 Humanitarian Aid Platform
          </div>
        </div>
      </footer>
    </div>
  );
}
