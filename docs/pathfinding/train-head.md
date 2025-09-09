# 🚆 Train Occupancy & Braking Windows

To keep trains safe, we track two **sliding windows** along the track:

1. **Occupancy Window**

   * From the **tail** of the train to the **nose**.
   * Represents the space the train physically occupies.
   * Used to check for collisions (no two trains may overlap).

2. **Braking Window**

   * From the **nose** of the train to the **brake head** (the farthest point the train could reach before stopping).
   * Length depends on speed, braking force, and safety buffers.
   * Used to check signals, stations, and other trains.

---

### How it Works

* The **Occupancy Window** moves forward as the train advances.
* The **Braking Window** stretches or shrinks with speed (longer at high speed, shorter at low speed).
* Every update:

  * If the **Braking Window** overlaps a station stop → begin braking.
  * If the **Braking Window** overlaps another train’s **Occupancy Window** → begin braking.
  * Otherwise, continue normally.

---

### Visual Sketch

``` 
                                (Clear track)
[ Tail ------------------ Nose ]============|========================= 
       (Occupied by train)             Brake Head (Stopping limit)

Other trains / stations must lie *beyond* the Brake Head.
```

---

This way the simulation always knows:

* **Where the train is** (Occupancy Window).
* **How far ahead it’s allowed to go safely** (Braking Window).

---

Would you like me to also make a **diagram (PNG/SVG)** version of this ASCII sketch so your docs can include a polished figure?
