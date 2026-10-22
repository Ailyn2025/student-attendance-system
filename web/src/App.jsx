
import { useEffect, useState } from "react";
import "./App.css";

const API_URL = import.meta.env.VITE_API_URL || "http://localhost:5001";

export default function App() {
  const [students, setStudents] = useState([]);
  const [studentNumber, setStudentNumber] = useState("");
  const [fullName, setFullName] = useState("");
  const [editingId, setEditingId] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [saving, setSaving] = useState(false);

  async function loadStudents() {
    setLoading(true);
    setError("");

    try {
      const response = await fetch(`${API_URL}/api/students`);
      if (!response.ok) throw new Error("Could not load students.");

      const data = await response.json();
      setStudents(data);
    } catch {
      setError("Cannot connect to the Attendance API on port 5001.");
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    loadStudents();
  }, []);

  async function handleSubmit(event) {
    event.preventDefault();

    if (!studentNumber.trim() || !fullName.trim()) {
      setError("Enter both a student number and full name.");
      return;
    }

    setSaving(true);
    setError("");

    try {
      const response = await fetch(
        editingId
          ? `${API_URL}/api/students/${editingId}`
          : `${API_URL}/api/students`,
        {
          method: editingId ? "PUT" : "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({
            studentNumber: studentNumber.trim(),
            fullName: fullName.trim(),
          }),
        }
      );

      if (!response.ok) {
        const result = await response.json().catch(() => ({}));
        throw new Error(result.message || "Unable to save student.");
      }

      setStudentNumber("");
      setFullName("");
      setEditingId(null);
      await loadStudents();
    } catch (err) {
      setError(err.message || "Unable to save student.");
    } finally {
      setSaving(false);
    }
  }

  function startEditing(student) {
    setEditingId(student.id);
    setStudentNumber(student.studentNumber);
    setFullName(student.fullName);
    setError("");
  }

  async function deleteStudent(student) {
    if (!window.confirm(`Delete ${student.fullName}?`)) return;

    setError("");

    try {
      const response = await fetch(
        `${API_URL}/api/students/${student.id}`,
        { method: "DELETE" }
      );

      if (!response.ok) throw new Error("Unable to delete student.");

      if (editingId === student.id) resetForm();
      await loadStudents();
    } catch (err) {
      setError(err.message || "Unable to delete student.");
    }
  }

  function resetForm() {
    setEditingId(null);
    setStudentNumber("");
    setFullName("");
    setError("");
  }

  return (
    <main className="page">
      <section className="card">
        <header className="app-header">
          <div className="logo">SA</div>
          <div>
            <p className="eyebrow">STUDENT MANAGEMENT</p>
            <h1>Student Attendance</h1>
            <p className="description">
              Manage student records stored in PostgreSQL.
            </p>
          </div>
        </header>

        <section className="message-box">
          <span>TOTAL STUDENTS</span>
          <h2>{students.length}</h2>
        </section>

        <form className="student-form" onSubmit={handleSubmit}>
          <h2>{editingId ? "Edit Student" : "Add Student"}</h2>

          <label htmlFor="studentNumber">Student Number</label>
          <input
            id="studentNumber"
            value={studentNumber}
            onChange={(event) => setStudentNumber(event.target.value)}
            placeholder="e.g. 2026-004"
            required
          />

          <label htmlFor="fullName">Full Name</label>
          <input
            id="fullName"
            value={fullName}
            onChange={(event) => setFullName(event.target.value)}
            placeholder="Enter student's full name"
            required
          />

          <div className="actions">
            <button type="submit" disabled={saving}>
              {saving ? "Saving..." : editingId ? "Update Student" : "Add Student"}
            </button>

            {editingId && (
              <button type="button" className="secondary" onClick={resetForm}>
                Cancel
              </button>
            )}
          </div>
        </form>

        {error && <p className="error">{error}</p>}

        <div className="list-heading">
          <h2>Student List</h2>
          <button type="button" className="secondary" onClick={loadStudents}>
            Refresh
          </button>
        </div>

        {loading ? (
          <p>Loading students...</p>
        ) : students.length === 0 ? (
          <p className="description">No students yet. Add your first student above.</p>
        ) : (
          <div className="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>Student Number</th>
                  <th>Full Name</th>
                  <th>Actions</th>
                </tr>
              </thead>
              <tbody>
                {students.map((student) => (
                  <tr key={student.id}>
                    <td>{student.studentNumber}</td>
                    <td>{student.fullName}</td>
                    <td>
                      <div className="actions">
                        <button
                          type="button"
                          className="secondary"
                          onClick={() => startEditing(student)}
                        >
                          Edit
                        </button>
                        <button
                          type="button"
                          className="danger"
                          onClick={() => deleteStudent(student)}
                        >
                          Delete
                        </button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </section>
    </main>
  );
}
