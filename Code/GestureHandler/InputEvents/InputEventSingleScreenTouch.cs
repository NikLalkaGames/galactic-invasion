using Godot;
using System;
using System.Linq;

/// <summary>
/// Represents a single-screen touch input event, extending InputEventAction.
/// </summary>
public partial class InputEventSingleScreenTouch : InputEventAction
{
    /// <summary>
    /// The position of the touch event.
    /// </summary>
    public Vector2 Position { get; set; }

    /// <summary>
    /// Indicates whether the touch is pressed.
    /// </summary>
    public new bool Pressed { get; set; }

    /// <summary>
    /// Indicates whether the touch event was canceled.
    /// </summary>
    public bool Canceled { get; set; }

    /// <summary>
    /// The raw gesture data associated with this touch event.
    /// </summary>
    public RawGesture RawGesture { get; set; }

    /// <summary>
    /// Initializes a new instance of the InputEventSingleScreenTouch class.
    /// </summary>
    /// <param name="_raw_gesture">The raw gesture data. Optional.</param>
    public InputEventSingleScreenTouch(RawGesture _raw_gesture = null)
    {
        RawGesture = _raw_gesture;
        if (RawGesture != null)
        {
            Pressed = RawGesture.Releases.Count == 0;
            if (Pressed && RawGesture.Presses.Count > 0)
            {
                Position = RawGesture.Presses.Values.First().Position;
            }
            else if (!Pressed && RawGesture.Releases.Count > 0)
            {
                Position = RawGesture.Releases.Values.First().Position;
            }
            else
            {
                Position = Vector2.Zero;
            }

            Canceled = RawGesture.Size() > 1;
        }
        else
        {
            Pressed = false;
            Canceled = false;
            Position = Vector2.Zero;
        }
    }

    /// <summary>
    /// Returns a string representation of the single-screen touch event.
    /// </summary>
    /// <returns>A string detailing position, pressed state, and cancellation status.</returns>
    public string AsString()
    {
        return $"position={Position}|pressed={Pressed}|canceled={Canceled}";
    }

    /// <summary>
    /// Overrides the default ToString method to provide a string representation of the event.
    /// </summary>
    /// <returns>A string detailing position, pressed state, and cancellation status.</returns>
    public override string ToString()
    {
        return AsString();
    }
}
