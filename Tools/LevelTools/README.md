# Level tools (Python 3, standard library only)

- `levels.py OUT_DIR PREVIEW_DIR` — regenerates the ASCII layouts in `Assets/_Game/Levels/` from code
  and writes colour previews. Optional: the `.txt` layouts can also be edited by hand.
- `reach.py LEVELS_DIR` — conservative reachability check (jump ≤ 3 tiles up / ≤ 5 across, ladders,
  one-way ledges, gates opened in key order). Run it after editing a layout.
- `png.py` — tiny PNG reader/writer used by the above (also `zoom` / `runs` / `grid` helpers for slicing art).

Example (from the project root):

    python Tools/LevelTools/reach.py Assets/_Game/Levels

After changing layouts, run **Tools > TileVania > Rebuild Game** in Unity.
