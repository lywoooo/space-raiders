# Space Raiders

Space Raiders is a small Unity space survival game built around movement, pressure, and staying in control. You pilot a ship through an asteroid field, avoid enemy ships, and try to survive as long as possible without getting boxed in or crashing out.

What makes the project interesting from a code perspective is that a lot of the feel comes from custom gameplay systems instead of just dropped-in assets. The ship uses its own movement logic with thrust, boost, roll, banking, damping, and speed clamping, so flying has a little weight without becoming hard to control. The asteroid field is generated around the player at runtime, with spacing rules, chunk-based density control, and regeneration logic so the space stays active as you move through it. Enemy ships also use their own simple state-driven behavior to patrol, chase, and pressure the player instead of just existing as static obstacles.

The project also pulls together strong visual building blocks from imported asset packs, but the main appeal is still in the gameplay code: responsive ship handling, procedural hazards, and constant movement pressure. It stays pretty grounded in scope, which makes it a nice example of using a few focused systems to create a fast-paced arcade loop.
