# Implementation notes (demo scenes batch)

Working log of deviations. Delete before this branch merges.

- Test clicks: MCP `simulate_mouse` moves the OS cursor but Unity's `Mouse.current` keeps reading the physical mouse, so playtests click through a virtual Input System mouse named TestMouse. If you see a "TestMouse" device in the Input Debugger, that's from testing; it goes away when the Editor restarts.
- Shot.cs: aimed shots (FlyToward) now check for a wall along each step with a linecast and disappear there. Physics triggers alone never fired against the Tilemap walls. Tower arrows (FlyToSpot / FlyAtTarget) still fly over walls. Also sets `useFullKinematicContacts` on the shot's body.
- EnemyChase.cs: stops moving once it touches the player, so it no longer shoves the player across the room.
- Added the Enemy tag to TagManager; Health's win check and Shot's default target need it.
- Added Assets/Scenes/Demos/Art/Square.png, a 4x4 white sprite for plain shapes (swing flash, blocks).
- Demo build scripts bake Tilemap collider shapes before saving. Built by script, the walls had saved with no collision at all.
- WaypointFollower.cs: when the last enemy reaches the base (and the base is still standing), the game now ends with "You win!". Before, the game kept running with nothing left.
- Tower defense demo balance: towers range 2.5 to 3, one hit or arrow every 1.2 s; slimes 5 HP, ladybugs 4 HP. In test runs about two enemies reach the base and you win with 3 of 5 HP. Spawns are random, so it varies.
