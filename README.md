# Project-E.C.H.O.

Genesis AR: A Planet Reborn is a visionary concept that taps into several growing trends: the demand for immersive experiences, the rise of AR/MR hardware, and the increasing interest in educational/creative games. Its unique blend of genetic engineering, creature creation, and ecosystem management.

## Holographic Telemetry Demo

This repository now includes a lightweight FastAPI backend (`main.py`) and a browser frontend (`index.html`) for a holographic world-status telemetry demo.

### Run the backend

1. Install dependencies:
   - `pip install fastapi uvicorn`
2. Start the server:
   - `python main.py`

The API will be available at `http://127.0.0.1:8000/api/world/status`.

### Run the frontend

1. Keep the backend running.
2. Open `index.html` in any modern browser.
3. Use the page controls to fetch and view live telemetry.
4. Optional: set a different backend origin using `?apiBase=http://host:port` in the URL.
