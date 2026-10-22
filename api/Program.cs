
import { useCallback, useEffect, useState } from "react";
import "./App.css";

const API_URL = import.meta.env.VITE_API_URL || "http://localhost:5001";

function todayString() {
  const now = new Date();
  const local = new Date(now.getTime() - now.getTimezoneOffset() * 60000);
  return local.toISOString().slice(0, 10);
}

async function apiRequest(path, options = {}) {
  const response = await fetch(`${API_URL}${path}`, {
    ...options,
    headers: {
      ...(options.body ? { "Content-Type": "application/json" } : {}),
      ...options.headers,
    },
  });

  if (!response.ok) {
    const result = await response.json().catch(() => ({}));
    throw new Error(
      result.message || `Request failed (${response.status})`
    );
  }

  if (response.status === 204) return null;
  return response.json();
}

export default function App() {
  const [students, setStudents] = useState([]);
  const [attendance, setAttendance] = useState([]);
  const [studentNumber, setStudentNumber] = useState("");
  const [fullName, setFullName] = useState("");
  const [editingId, setEditingId] = useState(null);
  const [attendanceDate, setAttendanceDate] = useState(todayString());
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");
  const [notice, setNotice] = useState("");

  const loadData = useCallback(async () => {
    setLoading(true);
    setError("");

    try {
      const [studentData, attendanceData] = await Promise.all([
        apiRequest("/api/students"),
        apiRequest(`/api/attendance?date=${attendanceDate}`),
      ]);

      setStudents(studentData);
      setAttendance(attendanceData);
    } catch (err) {
      setError(
        `${err.message}. Check that the API and PostgreSQL containers are running.`
      );
    } finally {
      setLoading(false);
    }
  }, [attendanceDate]);

  useEffect(() => {
    loadData();
  }, [loadData]);

  function resetForm() {
    setEditingId(null);
    setStudentNumber("");
    setFullName("");
  }

  async function handleStudentSubmit(event) {
    event.preventDefault();
    setSaving(true);
    setError("");
    setNotice("");

    try {
      const path = editingId
        ? `/api/students/${editingId}`
        : "/api/students";

      await apiRequest(path, {
        method: editingId ? "PUT" : "POST",
        body: JSON.stringify({
          studentNumber: studentNumber.trim(),
          fullName: fullName.trim(),
        }),
      });

      setNotice(editingId ? "Student updated successfully." : "Student added successfully.");
      resetForm();
      await loadData();
    } catch (err) {
      setError(err.message);
    } finally {
      setSaving(false);
    }
  }

  function editStudent(student) {
    setEditingId(student.id);
    setStudentNumber(student.studentNumber);
    setFullName(student.fullName);
    setNotice("");
    setError("");
    window.scrollTo({ top: 0, behavior: "smooth" });
  }

  async function deleteStudent(student) {
    if (!window.confirm(`Delete ${student.fullName}? Their attendance records will also be deleted.`)) {
      return;
    }

    setError("");
    setNotice("");

    try {
      await apiRequest(`/api/students/${student.id}`, {
        method: "DELETE",
      });

      if (editingId === student.id) resetForm();
      setNotice("Student deleted successfully.");
      await loadData();
    } catch (err) {
      setError(err.message);
    }
  }

  async function markAttendance(student, status) {
    setError("");
    setNotice("");

    try {
      await apiRequest("/api/attendance", {
        method: "POST",
        body: JSON.stringify({
          studentId: student.id,
          status,
          date: attendanceDate,
        }),
      });

      setNotice(`${student.fullName} marked ${status.toLowerCase()}.`);
      await loadData();
    } catch (err) {
      setError(err.message);
    }
  }

  const attendanceByStudent = Object.fromEntries(
    attendance.map((record) => [record.studentId, record.status])
  );

  const presentCount = attendance.filter(
    (record) => record.status === "Present"
  ).length;

  const absentCount = attendance.filter(
    (record) => record.status === "Absent"
  ).length;

  const unmarkedCount = Math.max(
    0,
    students.length - presentCount - absentCount
  );

  return (
    <main className="page">
      <section className="card dashboard">
        <header className="app-header">
          <div className="logo">SA</div>
          <div>
            <p className="eyebrow">STUDENT MANAGEMENT SYSTEM</p>
            <h1>Student Attendance</h1>
            <p className="description">
              Manage student records and daily attendance.
            </p>
          </div>
        </header>

        {error && <div className="alert error">{error}</div>}
        {notice && <div className="alert success">{notice}</div>}

        <section className="stats-grid">
          <article className="stat-card">
            <span>Total Students</span>
            <strong>{students.length}</strong>
          </article>
          <article className="stat-card">
            <span>Present</span>
            <strong>{presentCount}</strong>
          </article>
          <article className="stat-card">
            <span>Absent</span>
            <strong>{absentCount}</strong>
          </article>
          <article className="stat-card">
            <span>Not Marked</span>
            <strong>{unmarkedCount}</strong>
          </article>
        </section>

        <section className="panel">
          <h2>{editingId ? "Edit Student" : "Register Student"}</h2>

          <form className="student-form" onSubmit={handleStudentSubmit}>
            <div className="field">
              <label htmlFor="studentNumber">Student Number</label>
              <input
                id="studentNumber"
                value={studentNumber}
                onChange={(event) => setStudentNumber(event.target.value)}
                placeholder="e.g. 2026-004"
                required
                maxLength={50}
              />
            </div>

            <div className="field">
              <label htmlFor="fullName">Full Name</label>
              <input
                id="fullName"
                value={fullName}
                onChange={(event) => setFullName(event.target.value)}
                placeholder="Enter full name"
                required
                maxLength={150}
              />
            </div>

            <div className="actions">
              <button type="submit" disabled={saving}>
                {saving
                  ? "Saving..."
                  : editingId
                    ? "Update Student"
                    : "Add Student"}
              </button>

              {editingId && (
                <button
                  type="button"
                  className="secondary"
                  onClick={resetForm}
                >
                  Cancel
                </button>
              )}
            </div>
          </form>
        </section>

        <section className="panel">
          <div className="section-heading">
            <div>
              <h2>Daily Attendance</h2>
              <p className="description">
                Mark each student as present or absent for the selected date.
              </p>
            </div>

            <div className="field date-field">
              <label htmlFor="attendanceDate">Attendance Date</label>
              <input
                id="attendanceDate"
                type="date"
                value={attendanceDate}
                onChange={(event) => setAttendanceDate(event.target.value)}
                required
              />
            </div>
          </div>

          {loading ? (
            <p>Loading records...</p>
          ) : students.length === 0 ? (
            <p className="description">
              Register a student first to begin recording attendance.
            </p>
          ) : (
            <div className="table-wrap">
              <table>
                <thead>
                  <tr>
                    <th>Student Number</th>
                    <th>Full Name</th>
                    <th>Status</th>
                    <th>Mark Attendance</th>
                  </tr>
                </thead>
                <tbody>
                  {students.map((student) => {
                    const status = attendanceByStudent[student.id];

                    return (
                      <tr key={student.id}>
                        <td>{student.studentNumber}</td>
                        <td>{student.fullName}</td>
                        <td>
                          <span
                            className={`status ${
                              status ? status.toLowerCase() : "unmarked"
                            }`}
                          >
                            {status || "Not marked"}
                          </span>
                        </td>
                        <td>
                          <div className="actions attendance-actions">
                            <button
                              type="button"
                              className="present-button"
                              onClick={() =>
                                markAttendance(student, "Present")
                              }
                            >
                              Present
                            </button>
                            <button
                              type="button"
                              className="danger"
                              onClick={() => markAttendance(student, "Absent")}
                            >
                              Absent
                            </button>
                          </div>
                        </td>
                      </tr>
                    );
                  })}
                </tbody>
              </table>
            </div>
          )}
        </section>

        <section className="panel">
          <div className="section-heading">
            <h2>Student Records</h2>
            <button
              type="button"
              className="secondary"
              onClick={loadData}
              disabled={loading}
            >
              Refresh Records
            </button>
          </div>

          {loading ? (
            <p>Loading students...</p>
          ) : students.length === 0 ? (
            <p className="description">No student records found.</p>
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
                            onClick={() => editStudent(student)}
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

        <footer className="footer">
          React · .NET 10 Web API · PostgreSQL · Docker
        </footer>
      </section>
    </main>
  );
}
