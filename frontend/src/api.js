const BASE = import.meta.env.VITE_API_URL ?? "http://localhost:5000/api";

// Throws an Error carrying the API's message + field details.
async function request(path, options = {}) {
  const res = await fetch(`${BASE}${path}`, {
    headers: { "Content-Type": "application/json" },
    ...options,
  });
  if (res.ok) return res.status === 204 ? null : res.json();

  let body = null;
  try { body = await res.json(); } catch { /* non-JSON error */ }
  const err = new Error(body?.message ?? `Request failed (${res.status})`);
  err.details = body?.details ?? {};
  err.status = res.status;
  throw err;
}

export const api = {
  list: ({ status, search, page, pageSize }) => {
    const q = new URLSearchParams({ page, pageSize });
    if (status) q.set("status", status);
    if (search) q.set("search", search);
    return request(`/work-requests?${q}`);
  },
  create: (data) => request("/work-requests", { method: "POST", body: JSON.stringify(data) }),
  updateStatus: (id, status) =>
    request(`/work-requests/${id}/status`, { method: "PATCH", body: JSON.stringify({ status }) }),
  addNote: (id, text) =>
    request(`/work-requests/${id}/notes`, { method: "POST", body: JSON.stringify({ text }) }),
};
