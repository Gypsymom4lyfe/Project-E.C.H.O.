import unittest

from fastapi.testclient import TestClient

from main import app


class WorldStatusApiTests(unittest.TestCase):
    def setUp(self):
        self.client = TestClient(app)

    def test_world_status_shape_and_ranges(self):
        response = self.client.get("/api/world/status")

        self.assertEqual(response.status_code, 200)
        data = response.json()

        self.assertIn("timestamp", data)
        self.assertIsInstance(data["timestamp"], (int, float))
        self.assertEqual(data.get("environment_status"), "stable")
        self.assertIsInstance(data.get("nodes"), list)
        self.assertGreater(len(data["nodes"]), 0)

        for node in data["nodes"]:
            self.assertIn("id", node)
            self.assertIn("lat", node)
            self.assertIn("lon", node)
            self.assertIn("status", node)
            self.assertIn("biomass_index", node)
            self.assertIn("active_load_watts", node)

            biomass = node["biomass_index"]
            self.assertGreaterEqual(biomass, 0.0)
            self.assertLessEqual(biomass, 1.0)

            decimal_places = len(str(biomass).split(".")[1]) if "." in str(biomass) else 0
            self.assertLessEqual(decimal_places, 4)

            self.assertGreaterEqual(node["active_load_watts"], 525)
            self.assertLessEqual(node["active_load_watts"], 700)


if __name__ == "__main__":
    unittest.main()
