"""
Intentionally vulnerable web application for ZAP integration testing.
DO NOT deploy this application in any environment other than isolated testing.
"""

import os
import sqlite3
from flask import Flask, request, redirect, make_response, session, g

app = Flask(__name__)
app.secret_key = "test-secret-key-do-not-use-in-production"

DATABASE = "/tmp/test.db"


def get_db():
    if "db" not in g:
        g.db = sqlite3.connect(DATABASE)
        g.db.row_factory = sqlite3.Row
    return g.db


@app.teardown_appcontext
def close_db(exception):
    db = g.pop("db", None)
    if db is not None:
        db.close()


def init_db():
    db = sqlite3.connect(DATABASE)
    db.execute(
        "CREATE TABLE IF NOT EXISTS users (id INTEGER PRIMARY KEY, name TEXT, email TEXT)"
    )
    db.execute(
        "INSERT OR IGNORE INTO users (id, name, email) VALUES (1, 'admin', 'admin@example.com')"
    )
    db.execute(
        "INSERT OR IGNORE INTO users (id, name, email) VALUES (2, 'user', 'user@example.com')"
    )
    db.commit()
    db.close()


@app.route("/health")
def health():
    return "OK", 200


@app.route("/")
def index():
    return """<!DOCTYPE html>
<html><head><title>Test App</title></head><body>
<h1>Vulnerable Test Application</h1>
<ul>
  <li><a href="/search?q=test">Search</a></li>
  <li><a href="/login">Login</a></li>
  <li><a href="/admin">Admin</a></li>
  <li><a href="/users?id=1">Users</a></li>
  <li><a href="/redirect?url=/">Redirect</a></li>
  <li><a href="/about">About</a></li>
  <li><a href="/contact">Contact</a></li>
</ul>
</body></html>"""


@app.route("/search")
def search():
    q = request.args.get("q", "")
    # Intentional XSS: output query without escaping
    return f"""<!DOCTYPE html>
<html><head><title>Search</title></head><body>
<h1>Search Results</h1>
<p>You searched for: {q}</p>
<form action="/search" method="get">
  <input type="text" name="q" value="{q}">
  <button type="submit">Search</button>
</form>
<a href="/">Home</a>
</body></html>"""


@app.route("/login", methods=["GET", "POST"])
def login():
    if request.method == "POST":
        username = request.form.get("username", "")
        password = request.form.get("password", "")
        if username == "admin" and password == "password":
            session["user"] = username
            return redirect("/admin")
        return """<!DOCTYPE html>
<html><head><title>Login</title></head><body>
<h1>Login Failed</h1>
<p>Invalid credentials</p>
<a href="/login">Try again</a>
</body></html>"""

    # No CSRF token intentionally
    return """<!DOCTYPE html>
<html><head><title>Login</title></head><body>
<h1>Login</h1>
<form action="/login" method="post">
  <label>Username: <input type="text" name="username"></label><br>
  <label>Password: <input type="password" name="password"></label><br>
  <button type="submit">Login</button>
</form>
<a href="/">Home</a>
</body></html>"""


@app.route("/admin")
def admin():
    if "user" not in session:
        return """<!DOCTYPE html>
<html><head><title>Admin</title></head><body>
<h1>Access Denied</h1>
<p>Please <a href="/login">login</a> first.</p>
</body></html>""", 403

    return """<!DOCTYPE html>
<html><head><title>Admin</title></head><body>
<h1>Admin Panel</h1>
<p>Welcome, admin! <a href="/logout">Logout</a></p>
<a href="/">Home</a>
</body></html>"""


@app.route("/logout")
def logout():
    session.pop("user", None)
    return redirect("/login")


@app.route("/users")
def users():
    user_id = request.args.get("id", "1")
    db = get_db()
    # Intentional SQL injection: string formatting instead of parameterized query
    query = f"SELECT * FROM users WHERE id = {user_id}"
    try:
        cursor = db.execute(query)
        rows = cursor.fetchall()
        result = "".join(
            f"<tr><td>{row['id']}</td><td>{row['name']}</td><td>{row['email']}</td></tr>"
            for row in rows
        )
    except Exception as e:
        result = f"<tr><td colspan='3'>Error: {e}</td></tr>"

    return f"""<!DOCTYPE html>
<html><head><title>Users</title></head><body>
<h1>Users</h1>
<table border="1">
<tr><th>ID</th><th>Name</th><th>Email</th></tr>
{result}
</table>
<a href="/">Home</a>
</body></html>"""


@app.route("/redirect")
def open_redirect():
    url = request.args.get("url", "/")
    # Intentional open redirect: no validation
    return redirect(url)


@app.route("/about")
def about():
    return """<!DOCTYPE html>
<html><head><title>About</title></head><body>
<h1>About</h1>
<p>This is a test application for ZAP scanning.</p>
<a href="/">Home</a>
</body></html>"""


@app.route("/contact")
def contact():
    return """<!DOCTYPE html>
<html><head><title>Contact</title></head><body>
<h1>Contact</h1>
<p>Email: contact@example.com</p>
<a href="/">Home</a>
</body></html>"""


if __name__ == "__main__":
    init_db()
    app.run(host="0.0.0.0", port=80)
