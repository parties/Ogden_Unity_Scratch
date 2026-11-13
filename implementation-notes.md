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
- WaypointFollower.cs: new Is Enemy checkbox (on by default). TroopGold.cs says a troop can walk a path with WaypointFollower, but at the end of the path every walker lost the game. Unchecked, the troop just disappears at the end.
- Tower building demo: start gold 30. In a test run that bought four towers and never fired a cannon, the base ended at 1 HP; firing the cannons makes it comfortable.
- Clicker and gathering are two scenes: ClickerDemo (Reach 0, never runs out) and GatherDemo (walk up, Reach 2, breaks for good). ClickPickup and Mineable ignore clicks after the game is over.
- RoomDoor.cs + EnemySpawner.cs + Health.cs: the door stayed green (open) before a room spawner fired and between spawns of a wave, so a fast player could leave mid-wave. Spawners now count themselves in `Health.spawnersWaiting` while they still have enemies to send, and RoomDoor stays locked while it's above 0. Kept in Health so RoomDoor still needs only Health.cs. A room spawner with no Box Collider 2D now logs a warning, since it would keep the door locked forever.
- Dungeon demo balance: 3 slimes per wave, speed 1.4, 2 HP; player 6 HP. At 4 slimes and speed 1.8 the test player died in Room 1.
