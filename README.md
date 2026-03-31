# Space Raiders

**Space Raiders** is a small Unity space survival game built around movement, pressure, and staying in control. You pilot a ship through an asteroid field, avoid enemy ships, and try to survive as long as possible without getting boxed in or crashing out.

---

What makes the project interesting from a code perspective is that a lot of the feel comes from custom gameplay systems. The ship uses its own movement logic with thrust, boost, roll, banking, and damping, so flying has a little weight without becoming hard to control (Even if it isn't realistic to space travel). The asteroid field is generated around the player at runtime, with spacing rules, chunk-based density control, and regeneration logic so the space stays active as you move through it. Enemy ships also use their own simple state-driven behavior to patrol, chase, and pressure the player instead of just existing as static obstacles.

---

This project was originally a local hackathon project with some friends but I really wanted to expand it past what we managed in 8 hours into something playable and enjoyable! I really want to push this project to what us 4 envisioned at the beginning of the hackathon, hopefully living to all of our imaginations. Credit to my teammates are given from the repo I forked it from and credit to the assets are in the design document.
