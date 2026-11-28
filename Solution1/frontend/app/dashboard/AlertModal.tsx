"use client";

import styles from "./page.module.css";

export type AlertModalProps = {
  title: string;
  message: string;
  confirmLabel?: string;
  dismissLabel?: string;
  isConfirming?: boolean;
  onConfirm: () => void;
  onDismiss: () => void;
};

export default function AlertModal({
  title,
  message,
  confirmLabel = "Confirm",
  dismissLabel = "Dismiss",
  isConfirming = false,
  onConfirm,
  onDismiss,
}: AlertModalProps) {
  return (
    <div className={styles.alertBackdrop}>
      <section
        className={styles.alertModal}
        role="dialog"
        aria-modal="true"
        aria-labelledby="notification-alert-title"
        aria-describedby="notification-alert-message"
      >
        <h2 id="notification-alert-title">{title}</h2>
        <p id="notification-alert-message">{message}</p>
        <div className={styles.alertActions}>
          <button
            className={styles.button}
            type="button"
            onClick={onDismiss}
            disabled={isConfirming}
          >
            {dismissLabel}
          </button>
          <button
            className={`${styles.button} ${styles.primaryButton}`}
            type="button"
            onClick={onConfirm}
            disabled={isConfirming}
          >
            {isConfirming ? "Saving…" : confirmLabel}
          </button>
        </div>
      </section>
    </div>
  );
}
