Models
    Export with "Transform > +Y up".

Animations
    Select all keyframes and move back to start at frame 0.
    (Optional) Some animations don't loop correctly. If the first and last keyframe aren't identical, copy and paste the first keyframe to the end.
    Set animation Manual Frame Range to 0-<end>.
    Export without "Transform > +Y up" and without "Animation > Sampling Animations".

To fix scaling issues (Mixamo usually exports with mesh scaled to 100 but armature scaled to 0.01)
    1. Select mesh, Ctrl+A -> Scale
    2. Select mesh, Alt+P -> Clear Parent
    3. Select armature, Ctrl+A -> Scale
    4. Editor Type (top left corner) -> Graph Editor
    5. Filters (top right corner) -> Only Show Selected: disabled, Show Hidden: enabled
    6. Search (top left corner) -> enter "Location"
    7. Select point (0, 0) to put the 2D cursor there
    8. Pivot Point (top right corner) -> 2D Cursor
    9. A -> S -> Y -> 0.01
    10. Editor Type (top left corner) -> 3D Viewport
    11. Select mesh, Ctrl-select armature, Ctrl+P -> With Empty Groups
