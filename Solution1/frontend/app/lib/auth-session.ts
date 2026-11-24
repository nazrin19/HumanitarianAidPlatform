"use client";

import { useSyncExternalStore } from "react";

export const AUTH_SESSION_LOADING = "__auth_session_loading__";

const SESSION_CHANGED_EVENT = "auth-session-changed";

function subscribeToSession(onChange: () => void) {
  const notify = () => onChange();
  window.addEventListener("storage", notify);
  window.addEventListener(SESSION_CHANGED_EVENT, notify);
  return () => {
    window.removeEventListener("storage", notify);
    window.removeEventListener(SESSION_CHANGED_EVENT, notify);
  };
}

function getRoleSnapshot() {
  return window.sessionStorage.getItem("authRole") ?? "";
}

function getTokenSnapshot() {
  return window.sessionStorage.getItem("authToken") ?? "";
}

function getServerSnapshot() {
  return AUTH_SESSION_LOADING;
}

export function useSessionRole() {
  return useSyncExternalStore(subscribeToSession, getRoleSnapshot, getServerSnapshot);
}

export function useSessionToken() {
  return useSyncExternalStore(subscribeToSession, getTokenSnapshot, getServerSnapshot);
}

export function setSessionCredentials(token: string, role: string) {
  window.sessionStorage.setItem("authToken", token);
  window.sessionStorage.setItem("authRole", role);
  window.dispatchEvent(new Event(SESSION_CHANGED_EVENT));
}

export function clearSessionCredentials() {
  window.sessionStorage.removeItem("authToken");
  window.sessionStorage.removeItem("authRole");
  window.dispatchEvent(new Event(SESSION_CHANGED_EVENT));
}
