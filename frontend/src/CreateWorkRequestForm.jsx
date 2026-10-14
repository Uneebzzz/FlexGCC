import { useState } from "react";
import { api } from "./api";

const PRIORITIES = ["Low", "Medium", "High"];
const STATUSES = ["New", "InProgress", "Blocked", "Completed"];
const empty = { title: "", clientName: "", description: "", priority: "Medium", status: "New", dueDate: "" };

export default function CreateWorkRequestForm({ onCreated, onCancel }) {
  const [form, setForm] = useState(empty);
  const [errors, setErrors] = useState({});
  const [submitting, setSubmitting] = useState(false);
  const [serverError, setServerError] = useState("");

  const set = (field) => (e) => setForm({ ...form, [field]: e.target.value });

  // Client-side check for fast feedback; the API remains the source of truth.
  const validate = () => {
    const e = {};
    if (!form.title.trim()) e.title = "Title is required.";
    if (!form.clientName.trim()) e.clientName = "Client name is required.";
    if (!form.dueDate) e.dueDate = "Due date is required.";
    return e;
  };

  const submit = async (ev) => {
    ev.preventDefault();
    const local = validate();
    setErrors(local);
    if (Object.keys(local).length) return;

    setSubmitting(true);
    setServerError("");
    try {
      await api.create({ ...form, dueDate: new Date(form.dueDate).toISOString() });
      onCreated();
    } catch (err) {
      // Show field errors from the API next to the inputs.
      const fieldErrors = Object.fromEntries(
        Object.entries(err.details ?? {}).map(([k, v]) => [k, v[0]])
      );
      setErrors(fieldErrors);
      setServerError(err.message);
    } finally {
      setSubmitting(false);
    }
  };

  const field = (label, name, input) => (
    <label style={{ display: "block", marginBottom: 12 }}>
      <div>{label}</div>
      {input}
      {errors[name] && <div role="alert" style={{ color: "#b91c1c", fontSize: 13 }}>{errors[name]}</div>}
    </label>
  );

  return (
    <form onSubmit={submit} noValidate style={{ border: "1px solid #ddd", padding: 16, borderRadius: 8, marginBottom: 16 }}>
      <h3>New work request</h3>
      {serverError && <div role="alert" style={{ color: "#b91c1c", marginBottom: 8 }}>{serverError}</div>}
      {field("Title *", "title", <input value={form.title} onChange={set("title")} maxLength={200} />)}
      {field("Client name *", "clientName", <input value={form.clientName} onChange={set("clientName")} maxLength={200} />)}
      {field("Description", "description", <textarea value={form.description} onChange={set("description")} rows={3} />)}
      {field("Priority *", "priority",
        <select value={form.priority} onChange={set("priority")}>{PRIORITIES.map((p) => <option key={p}>{p}</option>)}</select>)}
      {field("Status *", "status",
        <select value={form.status} onChange={set("status")}>{STATUSES.map((s) => <option key={s}>{s}</option>)}</select>)}
      {field("Due date *", "dueDate", <input type="date" value={form.dueDate} onChange={set("dueDate")} />)}
      <button type="submit" disabled={submitting}>{submitting ? "Saving…" : "Create"}</button>{" "}
      <button type="button" onClick={onCancel} disabled={submitting}>Cancel</button>
    </form>
  );
}
