# Implementation notes

Working log for the PlatformChase damage demo. Delete it once the branch merges.

## 2026-11-12: PlatformChaseDamageDemo

- Built in the main checkout on branch `claude/platformchase-damage-demo`
  (cut from `claude/ogden-demo-scenes-c07f89`), not in a worktree, because
  Unity opened this folder.
- Health.cs and ContactDamage.cs are copies of the curriculum-2026 templates
  with fixes; PlatformChase.cs is edited in place. The curriculum templates are
  unchanged until Eric reviews:
  - PlatformChase stops when it touches the player, so it no longer shoves a
    standing player along (it pushed one 4 units into the spike pit).
  - Health gets Safe Time (default 0, so old scenes behave the same). ContactDamage
    uses the Stay callbacks when Safe Time is above 0, so two spike tiles take
    1 HP, not 2, and staying on spikes or against a slime hurts once a second.
- The player in this scene has a zero-friction material
  (`PlayerNoFriction.physicsMaterial2D`). Without it, holding D into a wall while
  jumping sticks the player to the wall (SideMover template issue, not fixed).
