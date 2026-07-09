# CombatManager

CombatManager adds a standalone AI movement sandbox for From The Depths.

Open it with `Ctrl+Shift+C`. The fullscreen editor shows an opaque tactical
2D/3D tactical graph between tabbed Blue/player controls on the left and tabbed
Red/enemy controls on the right. Both sides have mainframe-like behaviour and
manoeuvre settings, plan against each other from the same tick snapshot, then
move using shared ship/tank, hover, six-axis, or airplane movement-card request
approximations. Blue and Red are backed by editable AI blueprints that mirror
vanilla mainframe, behaviour, manoeuvre, and adjustment settings. Presets can
seed either side, `Import Blue AI` can copy supported settings from a selected
mainframe on the focused craft once, the Blue export preview shows future
vanilla mutations without writing, and Live Parity can read the focused craft's
actual AI requests for a read-only observed-vs-predicted comparison.

V2.2 adds a Unity-rendered 3D tactical graph mode. The graph starts top-down;
dragging the canvas switches to 3D/freecam so altitude pillars, climb/dive
trails, and aerial intent can be inspected. It also adds sandbox scaffolds for
Attack Run 1.0, Attack Run 2.0, and Attack Run 3.0 based on focused vanilla
decompilation, with PID/interception/path adjustment gaps labelled approximate.

Version: 0.2.2
