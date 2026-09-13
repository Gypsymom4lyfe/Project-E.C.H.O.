from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware
import random
import time

app = FastAPI(title="Project Echo Holographic Server")

app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=False,
    allow_methods=["*"],
    allow_headers=["*"],
)

NODES = [
    {"id": "node_us_east", "lat": 36.85, "lon": -76.28, "status": "active", "biomass_index": 0.82},
    {"id": "node_appalachia", "lat": 38.35, "lon": -81.63, "status": "active", "biomass_index": 0.65},
    {"id": "node_midwest", "lat": 41.88, "lon": -87.62, "status": "optimizing", "biomass_index": 0.74},
]


@app.get("/api/world/status")
def get_world_status():
    """Returns real-time telemetry and ecological seeding metrics for the globe."""
    timestamp = time.time()
    nodes = []

    for node in NODES:
        node_data = dict(node)
        fluctuation = random.uniform(-0.02, 0.03)
        node_data["biomass_index"] = round(min(max(node_data["biomass_index"] + fluctuation, 0.0), 1.0), 4)
        node_data["active_load_watts"] = random.randint(525, 700)
        nodes.append(node_data)

    return {"timestamp": timestamp, "environment_status": "stable", "nodes": nodes}


if __name__ == "__main__":
    import uvicorn

    uvicorn.run(app, host="127.0.0.1", port=8000)
