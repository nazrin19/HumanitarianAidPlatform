"use client";

import Image from "next/image";
import Link from "next/link";
import { FormEvent, useRef, useState } from "react";
import styles from "./page.module.css";

type NeedType = "sh" | "su";

export default function Home() {
  const [visibleNeeds, setVisibleNeeds] = useState({
    sh: true,
    su: true,
  });
  const [email, setEmail] = useState("");
  const [newsletterMessage, setNewsletterMessage] = useState("");
  const [isNewsletterSuccess, setIsNewsletterSuccess] = useState(false);
  const emailRef = useRef<HTMLInputElement>(null);

  function toggleNeed(type: NeedType) {
    setVisibleNeeds((visible) => ({ ...visible, [type]: !visible[type] }));
  }

  function handleNewsletterSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setNewsletterMessage("");
    setIsNewsletterSuccess(false);

    if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email.trim())) {
      setNewsletterMessage("Enter a valid email address.");
      emailRef.current?.focus();
      return;
    }

    setIsNewsletterSuccess(true);
    setNewsletterMessage("Thanks for signing up!");
    setEmail("");
  }

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
            <Link href="#requests">Explore Requests</Link>
            <Link href="#impact">Our Impact</Link>
            <Link href="#about">About Us</Link>
            <Link href="#involved">Get Involved</Link>
          </nav>

          <div className={styles.right}>
            <Link className={`${styles.button} ${styles.solid} ${styles.small}`} href="/request-aid">
              Join Us
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
              disaster relief, managing shelters, and dispatching aid where it
              is needed most.
            </p>
            <div className={styles.row}>
              <Link className={`${styles.button} ${styles.solid}`} href="/request-aid">
                Get Started
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
                <Link href="/request-aid">
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

        <section
          className={`${styles.layer} ${styles.tint}`}
          id="requests"
        >
          <div className={styles.wrap}>
            <span className={styles.eyebrow}>EXPLORE REQUESTS</span>
            <h2>Active needs, mapped in real time</h2>
            <p className={styles.sub}>
              See where shelter and supplies are needed most, and which requests
              are most urgent.
            </p>

            <div className={styles.needs}>
              <div className={styles.panel}>
                <h4>Active Needs &amp; Disaster Map</h4>
                <div className={styles.chips}>
                  <button
                    className={styles.filterChip}
                    type="button"
                    aria-pressed={visibleNeeds.sh}
                    onClick={() => toggleNeed("sh")}
                  >
                    &#8962; Shelter
                  </button>
                  <button
                    className={`${styles.filterChip} ${styles.suppliesChip}`}
                    type="button"
                    aria-pressed={visibleNeeds.su}
                    onClick={() => toggleNeed("su")}
                  >
                    &#9638; Supplies
                  </button>
                </div>

                <svg
                  className={styles.map}
                  viewBox="0 0 560 280"
                  role="img"
                  aria-label="Map showing shelter and supply needs"
                >
                  <rect width="560" height="280" fill="#EAF6EE" />
                  <path
                    d="M0 210c80-30 120 30 220 0s140-60 340-20v90H0z"
                    fill="#D5EEDF"
                  />
                  <path
                    d="M-10 70C90 40 150 120 250 90S420 20 570 70"
                    fill="none"
                    stroke="#BFE3EE"
                    strokeWidth="16"
                    strokeLinecap="round"
                  />
                  <g
                    fill="none"
                    stroke="#fff"
                    strokeWidth="7"
                    strokeLinecap="round"
                  >
                    <path d="M40 0c20 90 80 120 150 160s120 80 130 120" />
                    <path d="M0 150c120 20 200-30 330-10s170 60 230 40" />
                    <path d="M420 0c-10 60 20 110 70 150" />
                  </g>
                  <g fill="none" stroke="#D3DCE0" strokeWidth="1.5">
                    <path d="M0 100h560M0 200h560M140 0v280M300 0v280M440 0v280" opacity=".45" />
                  </g>
                  <g
                    style={{ display: visibleNeeds.sh ? undefined : "none" }}
                  >
                    <g transform="translate(130 118)">
                      <path
                        d="M0-22a14 14 0 0 1 14 14c0 10-14 26-14 26s-14-16-14-26a14 14 0 0 1 14-14z"
                        fill="#1B633E"
                      />
                      <circle cy="-8" r="5" fill="#fff" />
                    </g>
                    <g transform="translate(310 200)">
                      <path
                        d="M0-22a14 14 0 0 1 14 14c0 10-14 26-14 26s-14-16-14-26a14 14 0 0 1 14-14z"
                        fill="#1B633E"
                      />
                      <circle cy="-8" r="5" fill="#fff" />
                    </g>
                  </g>
                  <g
                    style={{ display: visibleNeeds.su ? undefined : "none" }}
                  >
                    <g transform="translate(230 70)">
                      <path
                        d="M0-22a14 14 0 0 1 14 14c0 10-14 26-14 26s-14-16-14-26a14 14 0 0 1 14-14z"
                        fill="#E8794A"
                      />
                      <circle cy="-8" r="5" fill="#fff" />
                    </g>
                    <g transform="translate(430 150)">
                      <path
                        d="M0-22a14 14 0 0 1 14 14c0 10-14 26-14 26s-14-16-14-26a14 14 0 0 1 14-14z"
                        fill="#E8794A"
                      />
                      <circle cy="-8" r="5" fill="#fff" />
                    </g>
                  </g>
                  <g
                    fontFamily="Inter, sans-serif"
                    fontSize="12"
                    fontWeight="600"
                    fill="#111A22"
                  >
                    <text x="150" y="140">
                      City Center
                    </text>
                    <text x="250" y="76">
                      Riverside
                    </text>
                    <text x="330" y="222">
                      Temple Grounds
                    </text>
                    <text x="450" y="172">
                      Harbour
                    </text>
                  </g>
                </svg>

                <div className={styles.mapStats}>
                  <div>
                    <b>2,400+</b>
                    <span>Volunteers Active</span>
                  </div>
                  <div>
                    <b>120+</b>
                    <span>Active Projects</span>
                  </div>
                </div>
              </div>

              <div>
                <div className={styles.requests}>
                  <article className={styles.request}>
                    <div>
                      <strong>Temporary shelter</strong>
                      <span className={`${styles.tag} ${styles.high}`}>
                        High · 92%
                      </span>
                    </div>
                    <small>City Center · Request #1042</small>
                    <div className={styles.bar}>
                      <i style={{ width: "85%" }} />
                    </div>
                  </article>
                  <article className={styles.request}>
                    <div>
                      <strong>Medicine</strong>
                      <span className={`${styles.tag} ${styles.high}`}>
                        High · 84%
                      </span>
                    </div>
                    <small>Riverside · Request #1041</small>
                    <div className={styles.bar}>
                      <i style={{ width: "60%" }} />
                    </div>
                  </article>
                  <article className={styles.request}>
                    <div>
                      <strong>Food packs</strong>
                      <span className={`${styles.tag} ${styles.medium}`}>
                        Medium · 67%
                      </span>
                    </div>
                    <small>Temple Grounds · Request #1040</small>
                    <div className={styles.bar}>
                      <i style={{ width: "40%" }} />
                    </div>
                  </article>
                  <article className={styles.request}>
                    <div>
                      <strong>Transport</strong>
                      <span className={`${styles.tag} ${styles.low}`}>
                        Low · 71%
                      </span>
                    </div>
                    <small>Harbour · Request #1038</small>
                    <div className={styles.bar}>
                      <i style={{ width: "25%" }} />
                    </div>
                  </article>
                </div>
                <p className={styles.note}>Sample data shown for illustration.</p>
              </div>
            </div>
          </div>
        </section>

        <section className={styles.layer} id="impact">
          <div className={styles.wrap}>
            <span className={styles.eyebrow}>OUR IMPACT</span>
            <h2>Aid that reaches people, measured</h2>
            <p className={styles.sub}>
              Every request, shelter and delivery is tracked so teams can see
              what is working.
            </p>

            <div className={styles.kpis}>
              <div className={styles.kpi}>
                <b>2,400+</b>
                <span>Volunteers active</span>
              </div>
              <div className={styles.kpi}>
                <b>120+</b>
                <span>Active projects</span>
              </div>
              <div className={styles.kpi}>
                <b>17</b>
                <span>Shelters connected</span>
              </div>
              <div className={styles.kpi}>
                <b>98.4%</b>
                <span>Alerts delivered</span>
              </div>
            </div>

            <div className={styles.impactDetails}>
              <div className={styles.panel}>
                <h4>Shelter occupancy</h4>
                <div className={styles.occupancy}>
                  <div>
                    <label>
                      <span>City Center Shelter</span>
                      <span className={styles.redText}>85% full</span>
                    </label>
                    <div className={`${styles.bar} ${styles.redBar}`}>
                      <i style={{ width: "85%" }} />
                    </div>
                  </div>
                  <div>
                    <label>
                      <span>Riverside Hall</span>
                      <span className={styles.greenText}>54% full</span>
                    </label>
                    <div className={styles.bar}>
                      <i style={{ width: "54%" }} />
                    </div>
                  </div>
                  <div>
                    <label>
                      <span>Temple Grounds</span>
                      <span className={styles.greenText}>39% full</span>
                    </label>
                    <div className={styles.bar}>
                      <i style={{ width: "39%" }} />
                    </div>
                  </div>
                </div>
              </div>
              <div className={`${styles.panel} ${styles.ringPanel}`}>
                <div className={styles.ring}>
                  <i>
                    <span>
                      92%
                      <small>Active Community</small>
                    </span>
                  </i>
                </div>
                <div>
                  <h4>Community engagement</h4>
                  <p>
                    Most registered volunteers and partners took part in at
                    least one response this month.
                  </p>
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
                      Requests are scored by urgency so the most critical cases
                      come first.
                    </p>
                  </div>
                </div>
                <div>
                  <i>2</i>
                  <div>
                    <strong>Coordinate</strong>
                    <p>Match shelters, stock and volunteers with a single click.</p>
                  </div>
                </div>
                <div>
                  <i>3</i>
                  <div>
                    <strong>Track</strong>
                    <p>Follow every dispatch and notify everyone involved.</p>
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
                  Pick up dispatches, help at shelters and update delivery
                  status.
                </p>
                <Link
                  className={`${styles.button} ${styles.solid} ${styles.small}`}
                  href="/signup"
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
                <p>Fund and stock the supplies that shelters need most.</p>
                <Link
                  className={`${styles.button} ${styles.solid} ${styles.small}`}
                  href="/signup"
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
                <p>Connect your team, shelter or charity to the network.</p>
                <Link
                  className={`${styles.button} ${styles.solid} ${styles.small}`}
                  href="/signup"
                >
                  Join Us
                </Link>
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
                <Link href="#requests">Explore Requests</Link>
                <Link href="#impact">Our Impact</Link>
                <Link href="#about">About Us</Link>
                <Link href="#involved">Get Involved</Link>
                <Link href="#top">Careers</Link>
                <Link href="#top">Contact</Link>
              </div>
            </div>
            <div>
              <h5>Newsletter</h5>
              <form
                className={styles.newsletter}
                noValidate
                onSubmit={handleNewsletterSubmit}
              >
                <input
                  ref={emailRef}
                  id="newsletter-email"
                  className={styles.newsletterInput}
                  type="email"
                  autoComplete="email"
                  placeholder="Email address"
                  aria-label="Email address"
                  aria-invalid={
                    Boolean(newsletterMessage) && !isNewsletterSuccess
                  }
                  aria-describedby="newsletter-message"
                  value={email}
                  onChange={(event) => setEmail(event.target.value)}
                />
                <button type="submit">Sign up</button>
              </form>
              <div
                className={`${styles.message} ${isNewsletterSuccess ? styles.success : ""}`}
                id="newsletter-message"
                role="status"
                aria-live="polite"
              >
                {newsletterMessage}
              </div>
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
