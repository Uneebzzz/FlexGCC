import { useCallback, useEffect, useState } from "react";
import { api } from "./api";
import { useDebounce } from "./useDebounce";
import CreateWorkRequestForm from "./CreateWorkRequestForm";

const STATUSES = ["New", "InProgress", "Blocked", "Completed"];
const PAGE_SIZE = 10;

export default function WorkRequestsPage() {
  const [data, setData] = useState({ items: [], totalCount: 0 });
  const [status, setStatus] = useState("");
  const [search, setSearch] = useState("");
  const [page, setPage] = useState(1);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [showForm, setShowForm] = useState(false);
  const [busyId, setBusyId] = useState(null);

  const debouncedSearch = useDebounce(search);

  const load = useCallback(async () => {
    setLoading(true);
    setError("");
    try {
      setData(await api.list({ status, search: debouncedSearch, page, pageSize: PAGE_SIZE }));
    } catch (e) {
      setError(e.message);
    } finally {
      setLoading(false);
    }
  }, [status, debouncedSearch, page]);

  useEffect(() => { load(); }, [load]);

  // Go back to page 1 whenever the filter or search changes.
  useEffect(() => { setPage(1); }, [status, debouncedSearch]);

  const changeStatus = async (id, newStatus) => {
    setBusyId(id);
    try {
      await api.updateStatus(id, newStatus);
      await load();
    } catch (e) {
      setError(e.message);
    } finally {
      setBusyId(null);
    }
  };

  const totalPages = Math.max(1, Math.ceil(data.totalCount / PAGE_SIZE));

  return (
    <main style={{ maxWidth: 960, margin: "24px auto", fontFamily: "system-ui, sans-serif" }}>
      <header style={{ display: "flex", justifyContent: "space-between", alignItems: "center" }}>
        <h1>Work Requests</h1>
        <button onClick={() => setShowForm(true)}>+ New request</button>
      </header>

      {showForm && (
        <CreateWorkRequestForm
          onCancel={() => setShowForm(false)}
          onCreated={() => { setShowForm(false); setPage(1); load(); }}
        />
      )}

      <div style={{ display: "flex", gap: 8, margin: "12px 0" }}>
        <input
          placeholder="Search title or client…"
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          style={{ flex: 1 }}
        />
        <select value={status} onChange={(e) => setStatus(e.target.value)}>
          <option value="">All statuses</option>
          {STATUSES.map((s) => <option key={s} value={s}>{s}</option>)}
        </select>
      </div>

      {error && (
        <div role="alert" style={{ background: "#fee2e2", padding: 12, borderRadius: 6, marginBottom: 12 }}>
          {error} <button onClick={load}>Retry</button>
        </div>
      )}

      {loading ? (
        <p>Loading…</p>
      ) : data.items.length === 0 && !error ? (
        <p>No work requests found.</p>
      ) : (
        <table width="100%" cellPadding={8} style={{ borderCollapse: "collapse" }}>
          <thead>
            <tr style={{ textAlign: "left", borderBottom: "2px solid #ddd" }}>
              <th>Title</th><th>Client</th><th>Priority</th><th>Due</th><th>Notes</th><th>Status</th>
            </tr>
          </thead>
          <tbody>
            {data.items.map((w) => (
              <tr key={w.id} style={{ borderBottom: "1px solid #eee" }}>
                <td>{w.title}</td>
                <td>{w.clientName}</td>
                <td>{w.priority}</td>
                <td>{new Date(w.dueDate).toLocaleDateString()}</td>
                <td>{w.noteCount}</td>
                <td>
                  <select
                    value={w.status}
                    disabled={busyId === w.id}
                    onChange={(e) => changeStatus(w.id, e.target.value)}
                  >
                    {STATUSES.map((s) => <option key={s}>{s}</option>)}
                  </select>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}

      <footer style={{ display: "flex", gap: 12, alignItems: "center", marginTop: 12 }}>
        <button disabled={page <= 1 || loading} onClick={() => setPage(page - 1)}>Previous</button>
        <span>Page {page} of {totalPages} ({data.totalCount} total)</span>
        <button disabled={page >= totalPages || loading} onClick={() => setPage(page + 1)}>Next</button>
      </footer>
    </main>
  );
}
