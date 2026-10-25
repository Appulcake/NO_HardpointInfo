## Hardpoint Info
A minimal mod that allows you to add (configurable) extra entries that show extra stats above hardpoint selections.
Configure via either Bepinex Configuration Manager's F1 menu, or `BepInEx/config/Apul.NO_HardpointInfo.cfg`.

<img width="1280" alt="image" src="https://github.com/user-attachments/assets/844b7073-9a6f-4035-864f-de6fc9a926cd" />

<img width="1280" alt="image" src="https://github.com/user-attachments/assets/d5d8719e-c170-4ee6-a18d-7622ca617c34" />


<img width="350" alt="image" src="https://github.com/user-attachments/assets/8bd8a3ba-0566-4bc7-98f0-a315eac1d9d2" /> <img width="250" alt="image" src="https://github.com/user-attachments/assets/39ca10e0-e85d-4234-97c3-9ff4f5bc0317" /> <img width="250" alt="image" src="https://github.com/user-attachments/assets/3979d574-9607-4c9e-abc0-c14c0450cb8a" />

Supports showing (you can turn them each on/off):
- Mount RCS
- Mount Empty RCS
- Mount Mass (including the munitions and mount pylon itself)
- Mount Empty Mass (mass of just the mount, when all munitions on mount are expended)
- Mount Drag
- Mount Empty Drag
- Mount Empty Cost (just the cost of having the mount itself, without the munition value)
- Lase Targets (shows total plane laser designator count on laser guided weapons, and above Aryx Weaponry Pack targeting pods shows how many extra targets that pod hardpoint adds)
- Lase Ranges (shows the ranges of all the plane's laser designators on laser guided weapons, and above Aryx Weaponry Pack targeting pods shows that pod hardpoint's lase range) 

By default an option is enabled where it only shows an entry if it's non-zero, with exception for regular-empty pairs, so if RCS is non-zero, 
then a zero empty RCS will still show so it's easier to compare. If all entries are zero, the extra panel doesn't show.

Also adds an Empty RCS stat in the bottom right info panel, under RCS, that shows what your full plane's RCS will be when all mounts are expended. This
can also be turned off (is enabled by default).
