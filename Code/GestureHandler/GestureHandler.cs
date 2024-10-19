using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Handles gesture inputs and emits high-level gesture signals such as taps, swipes, pinches, and twists.
/// </summary>
public partial class GestureHandler : Node
{

    public static GestureHandler Instance { get; private set; }

    #region Config

    // Configuration constants for gesture detection thresholds and limits.

    private const bool DEFAULT_BINDINGS = true;
    private const bool DEBUG = false;

    private const float DRAG_STARTUP_TIME = 0.02f;

    private const float FINGER_SIZE = 100.0f;

    private const float MULTI_FINGER_RELEASE_THRESHOLD = 0.1f;

    private const float TAP_TIME_LIMIT = 0.2f;
    private const float TAP_DISTANCE_LIMIT = 25.0f;

    private const float LONG_PRESS_TIME_THRESHOLD = 0.75f;
    private const float LONG_PRESS_DISTANCE_LIMIT = 25.0f;

    private const float SWIPE_TIME_LIMIT = 0.5f;
    private const float SWIPE_DISTANCE_THRESHOLD = 200.0f;

    #endregion

    #region Constants

    /// <summary>
    /// Maps swipe gesture names to their corresponding direction vectors.
    /// </summary>
    private static readonly Dictionary<string, Vector2> Swipe2Dir = new Dictionary<string, Vector2>
    {
        { "swipe_up", Vector2.Up },
        { "swipe_up_right", (Vector2.Up + Vector2.Right).Normalized() },
        { "swipe_right", Vector2.Right },
        { "swipe_down_right", (Vector2.Down + Vector2.Right).Normalized() },
        { "swipe_down", Vector2.Down },
        { "swipe_down_left", (Vector2.Down + Vector2.Left).Normalized() },
        { "swipe_left", Vector2.Left },
        { "swipe_up_left", (Vector2.Up + Vector2.Left).Normalized() }
    };

    #endregion

    #region Signals

    // Signal declarations for various gesture events.

    [Signal]
    public delegate void TouchEventHandler(InputEventScreenTouch touchEvent);

    [Signal]
    public delegate void DragEventHandler(InputEventScreenDrag dragEvent);

    [Signal]
    public delegate void SingleTapEventHandler(InputEventSingleScreenTap tapEvent);

    [Signal]
    public delegate void SingleTouchEventHandler(InputEventSingleScreenTouch touchEvent);

    [Signal]
    public delegate void SingleDragEventHandler(InputEventSingleScreenDrag dragEvent);

    [Signal]
    public delegate void SingleSwipeEventHandler(InputEventSingleScreenSwipe swipeEvent);

    [Signal]
    public delegate void SingleLongPressEventHandler(InputEventSingleScreenLongPress longPressEvent);

    [Signal]
    public delegate void MultiDragEventHandler(InputEventMultiScreenDrag dragEvent);

    [Signal]
    public delegate void MultiTapEventHandler(InputEventMultiScreenTap tapEvent);

    [Signal]
    public delegate void MultiSwipeEventHandler(InputEventMultiScreenSwipe swipeEvent);

    [Signal]
    public delegate void MultiLongPressEventHandler(InputEventMultiScreenLongPress longPressEvent);

    [Signal]
    public delegate void PinchEventHandler(InputEventScreenPinch pinchEvent);

    [Signal]
    public delegate void TwistEventHandler(InputEventScreenTwist twistEvent);

    [Signal]
    public delegate void RawGestureEventHandler(RawGesture rawGesture);

    [Signal]
    public delegate void CancelEventHandler(InputEventScreenCancel cancelEvent);

    [Signal]
    public delegate void AnyGestureEventHandler(string gestureName, InputEvent inputEvent);

    #endregion

    #region Enum

    /// <summary>
    /// Enumeration of gesture types for internal use.
    /// </summary>
    private enum Gesture
    {
        PINCH,
        MULTI_DRAG,
        TWIST,
        SINGLE_DRAG,
        NONE
    }

    #endregion

    #region Variables

    /// <summary>
    /// Stores the raw gesture data.
    /// </summary>
    private RawGesture rawGestureData = new RawGesture();

    /// <summary>
    /// Stores the position where the mouse event was pressed.
    /// </summary>
    private Vector2 mouseEventPressPosition;

    /// <summary>
    /// Stores the current mouse gesture being performed.
    /// </summary>
    private Gesture mouseEvent = Gesture.NONE;

    /// <summary>
    /// Timer for starting up single drag gestures.
    /// </summary>
    private Timer dragStartupTimer = new Timer();

    /// <summary>
    /// Timer for detecting long press gestures.
    /// </summary>
    private Timer longPressTimer = new Timer();

    /// <summary>
    /// Indicates whether a single touch has been canceled.
    /// </summary>
    private bool singleTouchCancelled = false;

    /// <summary>
    /// Indicates whether single drag gestures are enabled.
    /// </summary>
    private bool singleDragEnabled = false;

    #endregion

    #region Lifecycle Methods

    /// <summary>
    /// Called when the node is added to the scene tree for the first time.
    /// Initializes timers and sets default input bindings if enabled.
    /// </summary>
    public override void _Ready()
    {
        AddTimer(dragStartupTimer, OnDragStartupTimerTimeout);
        AddTimer(longPressTimer, OnLongPressTimerTimeout);

        if (DEFAULT_BINDINGS)
        {
            // Multi Swipes
            SetDefaultAction("multi_swipe_up", NativeKeyEvent((int)Key.I));
            SetDefaultAction("multi_swipe_up_right", NativeKeyEvent((int)Key.O));
            SetDefaultAction("multi_swipe_right", NativeKeyEvent((int)Key.L));
            SetDefaultAction("multi_swipe_down_right", NativeKeyEvent((int)Key.Period));
            SetDefaultAction("multi_swipe_down", NativeKeyEvent((int)Key.Comma));
            SetDefaultAction("multi_swipe_down_left", NativeKeyEvent((int)Key.M));
            SetDefaultAction("multi_swipe_left", NativeKeyEvent((int)Key.J));
            SetDefaultAction("multi_swipe_up_left", NativeKeyEvent((int)Key.Q));

            // Single Swipes
            SetDefaultAction("single_swipe_up", NativeKeyEvent((int)Key.W));
            SetDefaultAction("single_swipe_up_right", NativeKeyEvent((int)Key.E));
            SetDefaultAction("single_swipe_right", NativeKeyEvent((int)Key.D));
            SetDefaultAction("single_swipe_down_right", NativeKeyEvent((int)Key.C));
            SetDefaultAction("single_swipe_down", NativeKeyEvent((int)Key.X));
            SetDefaultAction("single_swipe_down_left", NativeKeyEvent((int)Key.Z));
            SetDefaultAction("single_swipe_left", NativeKeyEvent((int)Key.A));
            SetDefaultAction("single_swipe_up_left", NativeKeyEvent((int)Key.Q));

            SetDefaultAction("multi_touch", NativeMouseButtonEvent(MouseButton.Middle));
            SetDefaultAction("pinch_outward", NativeMouseButtonEvent(MouseButton.WheelUp));
            SetDefaultAction("pinch_inward", NativeMouseButtonEvent(MouseButton.WheelDown));
            SetDefaultAction("twist", NativeMouseButtonEvent(MouseButton.Right));
        }
    }

    public override void _EnterTree()
    {
        Instance = this;
    }

    public override void _ExitTree()
    {
        if (Instance == this)
            Instance = null;
    }

    /// <summary>
    /// Called when an input event hasn't been handled by any other node.
    /// Processes various input events to detect gestures.
    /// </summary>
    /// <param name="event">The input event.</param>
    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventScreenDrag screenDrag)
        {
            HandleScreenDrag(screenDrag);
        }
        else if (@event is InputEventScreenTouch screenTouch)
        {
            HandleScreenTouch(screenTouch);
        }
        else if (@event is InputEventMouseMotion mouseMotion)
        {
            HandleMouseMotion(mouseMotion);
        }
        else
        {
            HandleAction(@event);
        }
    }

    #endregion

    #region Event Handlers

    /// <summary>
    /// Handles mouse motion events for gesture detection.
    /// </summary>
    /// <param name="eventMotion">The mouse motion event.</param>
    private void HandleMouseMotion(InputEventMouseMotion eventMotion)
    {
        if (rawGestureData.Drags.Count == 1 && mouseEvent == Gesture.SINGLE_DRAG)
        {
            EmitSingleDrag(new InputEventSingleScreenDrag(rawGestureData));
        }
        else if (rawGestureData.Drags.Count == 2 && mouseEvent == Gesture.MULTI_DRAG)
        {
            Vector2 offset = new Vector2(5, 5);
            var e0 = NativeDragEvent(0, eventMotion.Position - offset, eventMotion.Relative, eventMotion.Velocity);
            rawGestureData.UpdateScreenDrag(e0);
            var e1 = NativeDragEvent(1, eventMotion.Position + offset, eventMotion.Relative, eventMotion.Velocity);
            rawGestureData.UpdateScreenDrag(e1);
            EmitMultiDrag(new InputEventMultiScreenDrag(rawGestureData, e0));
            EmitMultiDrag(new InputEventMultiScreenDrag(rawGestureData, e1));
        }
        else if (mouseEvent == Gesture.TWIST)
        {
            Vector2 rel1 = eventMotion.Position - mouseEventPressPosition;
            Vector2 rel2 = rel1 + eventMotion.Relative;
            var twistEvent = new InputEventScreenTwist
            {
                Position = mouseEventPressPosition,
                Relative = rel1.AngleTo(rel2),
                Fingers = 2
            };
            EmitTwist(twistEvent);
        }
    }

    /// <summary>
    /// Handles screen touch events for gesture detection.
    /// </summary>
    /// <param name="eventTouch">The screen touch event.</param>
    private void HandleScreenTouch(InputEventScreenTouch eventTouch)
    {
        if (eventTouch.Index < 0)
        {
            EmitCancel(new InputEventScreenCancel(rawGestureData, eventTouch));
            EndGesture();
            return;
        }

        // Ignore orphaned touch release events
        if (!eventTouch.Pressed && !rawGestureData.Presses.ContainsKey(eventTouch.Index))
        {
            return;
        }

        rawGestureData.UpdateScreenTouch(eventTouch);
        EmitRawGesture(rawGestureData);

        int index = eventTouch.Index;
        if (eventTouch.Pressed)
        {
            if (rawGestureData.Drags.Count == 1) // First and only touch
            {
                longPressTimer.Start(LONG_PRESS_TIME_THRESHOLD);
                singleTouchCancelled = false;
                EmitSingleTouch(new InputEventSingleScreenTouch(rawGestureData));
            }
            else if (!singleTouchCancelled)
            {
                singleTouchCancelled = true;
                CancelSingleDrag();
                EmitSingleTouch(new InputEventSingleScreenTouch(rawGestureData));
            }
        }
        else
        {
            int fingers = rawGestureData.ActiveTouches;
            if (index == 0)
            {
                EmitSingleTouch(new InputEventSingleScreenTouch(rawGestureData));
                if (!singleTouchCancelled)
                {
                    float distance = (rawGestureData.Releases[0].Position - rawGestureData.Presses[0].Position).Length();
                    if (rawGestureData.ElapsedTime < TAP_TIME_LIMIT && distance <= TAP_DISTANCE_LIMIT)
                    {
                        EmitSingleTap(new InputEventSingleScreenTap(rawGestureData));
                    }
                    if (rawGestureData.ElapsedTime < SWIPE_TIME_LIMIT && distance > SWIPE_DISTANCE_THRESHOLD)
                    {
                        EmitSingleSwipe(new InputEventSingleScreenSwipe(rawGestureData));
                    }
                }
            }

            if (rawGestureData.ActiveTouches == 0) // Last finger released
            {
                if (singleTouchCancelled)
                {
                    Vector2 endsCentroid = GestureHelpers.Centroid(rawGestureData.GetEnds().Values.ToList());
                    Vector2 startsCentroid = rawGestureData.Centroid("presses", "position");
                    float distance = (endsCentroid - startsCentroid).Length();
                    if (rawGestureData.ElapsedTime < TAP_TIME_LIMIT && distance <= TAP_DISTANCE_LIMIT &&
                        rawGestureData.IsConsistent(TAP_DISTANCE_LIMIT, FINGER_SIZE * rawGestureData.ActiveTouches) &&
                        ReleasedTogether(rawGestureData, MULTI_FINGER_RELEASE_THRESHOLD))
                    {
                        EmitMultiTap(new InputEventMultiScreenTap(rawGestureData));
                    }

                    if (rawGestureData.ElapsedTime < SWIPE_TIME_LIMIT && distance > SWIPE_DISTANCE_THRESHOLD &&
                        rawGestureData.IsConsistent(FINGER_SIZE, FINGER_SIZE * rawGestureData.ActiveTouches) &&
                        ReleasedTogether(rawGestureData, MULTI_FINGER_RELEASE_THRESHOLD))
                    {
                        EmitMultiSwipe(new InputEventMultiScreenSwipe(rawGestureData));
                    }
                }
                EndGesture();
            }
            CancelSingleDrag();
        }
    }

    /// <summary>
    /// Handles screen drag events for gesture detection.
    /// </summary>
    /// <param name="eventDrag">The screen drag event.</param>
    private void HandleScreenDrag(InputEventScreenDrag eventDrag)
    {
        if (eventDrag.Index < 0)
        {
            EmitCancel(new InputEventScreenCancel(rawGestureData, eventDrag));
            EndGesture();
            return;
        }

        rawGestureData.UpdateScreenDrag(eventDrag);
        EmitRawGesture(rawGestureData);

        if (rawGestureData.Drags.Count > 1)
        {
            CancelSingleDrag();
            int gesture = IdentifyGesture(rawGestureData);
            switch ((Gesture)gesture)
            {
                case Gesture.PINCH:
                    EmitPinch(new InputEventScreenPinch(rawGestureData, eventDrag));
                    break;
                case Gesture.MULTI_DRAG:
                    EmitMultiDrag(new InputEventMultiScreenDrag(rawGestureData, eventDrag));
                    break;
                case Gesture.TWIST:
                    EmitTwist(new InputEventScreenTwist(rawGestureData, eventDrag));
                    break;
            }
        }
        else
        {
            if (singleDragEnabled)
            {
                EmitSingleDrag(new InputEventSingleScreenDrag(rawGestureData));
            }
            else
            {
                if (dragStartupTimer.IsStopped())
                {
                    dragStartupTimer.Start(DRAG_STARTUP_TIME);
                }
            }
        }
    }

    /// <summary>
    /// Handles custom input actions for gesture emulation and detection.
    /// </summary>
    /// <param name="event">The input event.</param>
    private void HandleAction(InputEvent @event)
    {
        // Handle single_touch action
        if (InputMap.HasAction("single_touch") &&
            (@event.IsActionPressed("single_touch") || @event.IsActionReleased("single_touch")))
        {
            var touchEvent = NativeTouchEvent(0, GetViewport().GetMousePosition(), @event.IsActionPressed("single_touch"));
            EmitTouch(touchEvent);

            if (@event.IsActionPressed("single_touch"))
            {
                mouseEvent = Gesture.SINGLE_DRAG;
            }
            else
            {
                mouseEvent = Gesture.NONE;
            }
        }
        // Handle multi_touch action
        else if (InputMap.HasAction("multi_touch") &&
                 (@event.IsActionPressed("multi_touch") || @event.IsActionReleased("multi_touch")))
        {
            var touchEvent0 = NativeTouchEvent(0, GetViewport().GetMousePosition(), @event.IsActionPressed("multi_touch"));
            var touchEvent1 = NativeTouchEvent(1, GetViewport().GetMousePosition(), @event.IsActionPressed("multi_touch"));
            EmitTouch(touchEvent0);
            EmitTouch(touchEvent1);

            if (@event.IsActionPressed("multi_touch"))
            {
                mouseEvent = Gesture.MULTI_DRAG;
            }
            else
            {
                mouseEvent = Gesture.NONE;
            }
        }
        // Handle twist action
        else if (InputMap.HasAction("twist") &&
                 (@event.IsActionPressed("twist") || @event.IsActionReleased("twist")))
        {
            mouseEventPressPosition = GetViewport().GetMousePosition();
            if (@event.IsActionPressed("twist"))
            {
                mouseEvent = Gesture.TWIST;
            }
            else
            {
                mouseEvent = Gesture.NONE;
            }
        }
        // Handle pinch actions
        else if ((InputMap.HasAction("pinch_outward") && @event.IsActionPressed("pinch_outward")) ||
                 (InputMap.HasAction("pinch_inward") && @event.IsActionPressed("pinch_inward")))
        {
            var pinchEvent = new InputEventScreenPinch
            {
                Fingers = 2,
                Position = GetViewport().GetMousePosition(),
                Distance = 400,
                Relative = 40
            };

            if (@event.IsActionPressed("pinch_inward"))
            {
                pinchEvent.Relative *= -1;
            }

            EmitPinch(pinchEvent);
        }
        else
        {
            Vector2 swipeEmulationDir = Vector2.Zero;
            bool isSingleSwipe = false;

            foreach (var swipe in Swipe2Dir)
            {
                string singleSwipeAction = "single_" + swipe.Key;
                string multiSwipeAction = "multi_" + swipe.Key;

                if (InputMap.HasAction(singleSwipeAction) && @event.IsActionPressed(singleSwipeAction))
                {
                    swipeEmulationDir = swipe.Value;
                    isSingleSwipe = true;
                    break;
                }
                if (InputMap.HasAction(multiSwipeAction) && @event.IsActionPressed(multiSwipeAction))
                {
                    swipeEmulationDir = swipe.Value;
                    isSingleSwipe = false;
                    break;
                }
            }

            if (swipeEmulationDir != Vector2.Zero)
            {
                if (isSingleSwipe)
                {
                    var swipeEvent = new InputEventSingleScreenSwipe
                    {
                        Position = GetViewport().GetMousePosition(),
                        Relative = swipeEmulationDir * SWIPE_DISTANCE_THRESHOLD * 2
                    };
                    EmitSingleSwipe(swipeEvent);
                }
                else
                {
                    var swipeEvent = new InputEventMultiScreenSwipe
                    {
                        Fingers = 2,
                        Position = GetViewport().GetMousePosition(),
                        Relative = swipeEmulationDir * SWIPE_DISTANCE_THRESHOLD * 2
                    };
                    EmitMultiSwipe(swipeEvent);
                }
            }
        }
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Creates a native touch event.
    /// </summary>
    /// <param name="index">The touch index.</param>
    /// <param name="position">The touch position.</param>
    /// <param name="pressed">Whether the touch is pressed.</param>
    /// <returns>A new InputEventScreenTouch.</returns>
    private InputEventScreenTouch NativeTouchEvent(int index, Vector2 position, bool pressed)
    {
        return new InputEventScreenTouch
        {
            Index = index,
            Position = position,
            Pressed = pressed
        };
    }

    /// <summary>
    /// Creates a native drag event.
    /// </summary>
    /// <param name="index">The drag index.</param>
    /// <param name="position">The drag position.</param>
    /// <param name="relative">The relative movement.</param>
    /// <param name="velocity">The drag velocity.</param>
    /// <returns>A new InputEventScreenDrag.</returns>
    private InputEventScreenDrag NativeDragEvent(int index, Vector2 position, Vector2 relative, Vector2 velocity)
    {
        return new InputEventScreenDrag
        {
            Index = index,
            Position = position,
            Relative = relative,
            Velocity = velocity
        };
    }

    /// <summary>
    /// Creates a native mouse button event.
    /// </summary>
    /// <param name="button">The mouse button index.</param>
    /// <returns>A new InputEventMouseButton.</returns>
    private InputEventMouseButton NativeMouseButtonEvent(MouseButton button)
    {
        return new InputEventMouseButton
        {
            ButtonIndex = button
        };
    }

    /// <summary>
    /// Creates a native key event.
    /// </summary>
    /// <param name="key">The key index.</param>
    /// <returns>A new InputEventKey.</returns>
    private InputEventKey NativeKeyEvent(int key)
    {
        return new InputEventKey
        {
            Keycode = (Key)key
        };
    }

    /// <summary>
    /// Sets a default action in the InputMap.
    /// </summary>
    /// <param name="action">The name of the action.</param>
    /// <param name="event">The InputEvent to bind to the action.</param>
    private void SetDefaultAction(string action, InputEvent @event)
    {
        if (!InputMap.HasAction(action))
        {
            InputMap.AddAction(action);
            InputMap.ActionAddEvent(action, @event);
        }
    }

    /// <summary>
    /// Adds a Timer node and connects its timeout signal to a specified method.
    /// </summary>
    /// <param name="timer">The Timer node to add.</param>
    /// <param name="func">The method to connect to the timeout signal.</param>
    private void AddTimer(Timer timer, Action func)
    {
        timer.OneShot = true;
        if (func != null)
        {
            timer.Timeout += func;
        }
        AddChild(timer);
    }

    /// <summary>
    /// Cancels the single drag operation and stops the drag startup timer.
    /// </summary>
    private void CancelSingleDrag()
    {
        singleDragEnabled = false;
        dragStartupTimer.Stop();
    }

    /// <summary>
    /// Checks if the gestures were released together within a threshold.
    /// </summary>
    /// <param name="rawGestureData">The raw gesture data.</param>
    /// <param name="threshold">The time threshold.</param>
    /// <returns>True if released together; otherwise, false.</returns>
    private bool ReleasedTogether(RawGesture rawGestureData, float threshold)
    {
        var rolledBackGesture = rawGestureData.RollbackRelative(threshold);
        return rolledBackGesture != null && rolledBackGesture.ActiveTouches == rolledBackGesture.Drags.Count;
    }

    /// <summary>
    /// Identifies the gesture type based on the raw gesture data.
    /// </summary>
    /// <param name="rawGestureData">The raw gesture data.</param>
    /// <returns>An integer representing the gesture enum.</returns>
    private int IdentifyGesture(RawGesture rawGestureData)
    {
        Vector2 center = rawGestureData.Centroid("drags", "position");

        int sector = -1;
        foreach (var drag in rawGestureData.Drags.Values)
        {
            Vector2 adjustedPosition = center - drag.Position;
            float rawAngle = Mathf.PosMod(adjustedPosition.AngleTo(drag.Relative) + Mathf.Pi / 4, Mathf.Tau);
            float adjustedAngle = rawAngle >= 0 ? rawAngle : rawAngle + Mathf.Tau;
            int eSector = (int)Mathf.Floor(adjustedAngle / (Mathf.Pi / 2));

            if (sector == -1)
            {
                sector = eSector;
            }
            else if (sector != eSector)
            {
                return (int)Gesture.MULTI_DRAG;
            }
        }

        if (sector == 0 || sector == 2)
        {
            return (int)Gesture.PINCH;
        }
        else if (sector == 1 || sector == 3)
        {
            return (int)Gesture.TWIST;
        }
        else
        {
            // Default to multi-drag if gesture is unrecognized
            return (int)Gesture.MULTI_DRAG;
        }
    }

    /// <summary>
    /// Handles the drag startup timer timeout.
    /// </summary>
    private void OnDragStartupTimerTimeout()
    {
        singleDragEnabled = rawGestureData.Drags.Count == 1;
    }

    /// <summary>
    /// Handles the long press timer timeout.
    /// </summary>
    private void OnLongPressTimerTimeout()
    {
        Vector2 endsCentroid = GestureHelpers.Centroid(rawGestureData.GetEnds().Values.ToList());
        Vector2 startsCentroid = rawGestureData.Centroid("presses", "position");
        float distance = (endsCentroid - startsCentroid).Length();

        if (rawGestureData.Releases.Count == 0 &&
            distance <= LONG_PRESS_DISTANCE_LIMIT &&
            rawGestureData.IsConsistent(LONG_PRESS_DISTANCE_LIMIT, FINGER_SIZE * rawGestureData.ActiveTouches))
        {
            if (singleTouchCancelled)
            {
                EmitMultiLongPress(new InputEventMultiScreenLongPress(rawGestureData));
            }
            else
            {
                EmitSingleLongPress(new InputEventSingleScreenLongPress(rawGestureData));
            }
        }
    }

    /// <summary>
    /// Ends the current gesture by resetting variables and stopping timers.
    /// </summary>
    private void EndGesture()
    {
        singleDragEnabled = false;
        longPressTimer.Stop();
        rawGestureData = new RawGesture();
    }

    #endregion

    #region Gesture Emission Methods

    /// <summary>
    /// Emits the 'any_gesture' signal along with the specific gesture signal.
    /// </summary>
    /// <param name="sig">The name of the specific gesture signal.</param>
    /// <param name="val">The InputEvent associated with the gesture.</param>
    
    private void EmitAnyGesture(string sig, InputEvent val)
    {
        EmitSignal(nameof(AnyGesture), sig, val);
        EmitSignal(sig, val);
    }

    /// <summary>
    /// Emits a touch gesture signal.
    /// </summary>
    /// <param name="touchEvent">The touch event.</param>
    private void EmitTouch(InputEventScreenTouch touchEvent)
    {
        EmitAnyGesture(nameof(Touch), touchEvent);
    }

    /// <summary>
    /// Emits a drag gesture signal.
    /// </summary>
    /// <param name="dragEvent">The drag event.</param>
    private void EmitDrag(InputEventScreenDrag dragEvent)
    {
        EmitAnyGesture(nameof(Drag), dragEvent);
    }

    /// <summary>
    /// Emits a multi-finger drag gesture signal.
    /// </summary>
    /// <param name="multiDragEvent">The multi-drag event.</param>
    private void EmitMultiDrag(InputEventMultiScreenDrag multiDragEvent)
    {
        EmitAnyGesture(nameof(MultiDrag), multiDragEvent);
    }

    /// <summary>
    /// Emits a single tap gesture signal.
    /// </summary>
    /// <param name="tapEvent">The tap event.</param>
    private void EmitSingleTap(InputEventSingleScreenTap tapEvent)
    {
        EmitAnyGesture(nameof(SingleTap), tapEvent);
    }

    /// <summary>
    /// Emits a single touch gesture signal.
    /// </summary>
    /// <param name="touchEvent">The touch event.</param>
    private void EmitSingleTouch(InputEventSingleScreenTouch touchEvent)
    {
        EmitAnyGesture(nameof(SingleTouch), touchEvent);
    }

    /// <summary>
    /// Emits a single drag gesture signal.
    /// </summary>
    /// <param name="dragEvent">The drag event.</param>
    private void EmitSingleDrag(InputEventSingleScreenDrag dragEvent)
    {
        EmitAnyGesture(nameof(SingleDrag), dragEvent);
    }

    /// <summary>
    /// Emits a single swipe gesture signal.
    /// </summary>
    /// <param name="swipeEvent">The swipe event.</param>
    private void EmitSingleSwipe(InputEventSingleScreenSwipe swipeEvent)
    {
        EmitAnyGesture(nameof(SingleSwipe), swipeEvent);
    }

    /// <summary>
    /// Emits a single long press gesture signal.
    /// </summary>
    /// <param name="longPressEvent">The long press event.</param>
    private void EmitSingleLongPress(InputEventSingleScreenLongPress longPressEvent)
    {
        EmitAnyGesture(nameof(SingleLongPress), longPressEvent);
    }

    /// <summary>
    /// Emits a multi-finger tap gesture signal.
    /// </summary>
    /// <param name="tapEvent">The tap event.</param>
    private void EmitMultiTap(InputEventMultiScreenTap tapEvent)
    {
        EmitAnyGesture(nameof(MultiTap), tapEvent);
    }

    /// <summary>
    /// Emits a multi-finger swipe gesture signal.
    /// </summary>
    /// <param name="swipeEvent">The swipe event.</param>
    private void EmitMultiSwipe(InputEventMultiScreenSwipe swipeEvent)
    {
        EmitAnyGesture(nameof(MultiSwipe), swipeEvent);
    }


    /// <summary>
    /// Emits a multi-finger long press gesture signal.
    /// </summary>
    /// <param name="longPressEvent">The long press event.</param>
    private void EmitMultiLongPress(InputEventMultiScreenLongPress longPressEvent)
    {
        EmitAnyGesture(nameof(MultiLongPress), longPressEvent);
    }

    /// <summary>
    /// Emits a pinch gesture signal.
    /// </summary>
    /// <param name="pinchEvent">The pinch event.</param>
    private void EmitPinch(InputEventScreenPinch pinchEvent)
    {
        EmitAnyGesture(nameof(Pinch), pinchEvent);
    }

    /// <summary>
    /// Emits a twist gesture signal.
    /// </summary>
    /// <param name="twistEvent">The twist event.</param>
    private void EmitTwist(InputEventScreenTwist twistEvent)
    {
        EmitAnyGesture(nameof(Twist), twistEvent);
    }

    /// <summary>
    /// Emits the raw gesture data signal.
    /// </summary>
    /// <param name="rawGesture">The raw gesture data.</param>
    private void EmitRawGesture(RawGesture rawGesture)
    {
        EmitSignal(nameof(RawGesture), rawGesture);
        EmitSignal(nameof(AnyGesture), "raw_gesture", rawGesture);
    }

    /// <summary>
    /// Emits a gesture cancellation signal.
    /// </summary>
    /// <param name="cancelEvent">The cancel event.</param>
    private void EmitCancel(InputEventScreenCancel cancelEvent)
    {
        EmitAnyGesture(nameof(Cancel), cancelEvent);
    }

    #endregion
}
