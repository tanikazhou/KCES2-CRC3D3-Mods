# CRC3D3 Debug Tools

Debug tools for CRC3D3.

- Adds a debug overlay at the top right of the screen (F3), below the follow character panel when it shows. The overlay hides while it has
  nothing to show.
- Shows the player's position (X, Y, Z) and facing as the first overlay line.
- Adds Shift+P to copy the position and facing as script tag values (`pos={...} rot={...}`) to the clipboard and the log, also while
  the overlay is hidden.
- Shows the field event points in range in the overlay: event id, start type, the game's last availability check, script
  file and label, whether the script file exists, prompt, time and character conditions.

Toggleable in Plugin settings (ConfigManager).
