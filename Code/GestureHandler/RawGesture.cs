using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// Represents a raw gesture input event, capturing presses, releases, and drags.
/// </summary>
public partial class RawGesture : InputEventAction
{
    #region Nested Classes

    /// <summary>
    /// Base class for different types of events within a gesture.
    /// </summary>
    public class Event
    {
        /// <summary>
        /// The timestamp of the event in seconds.
        /// </summary>
        public float Time { get; set; } = -1f; // (secs)

        /// <summary>
        /// The index identifier for the event (e.g., finger index).
        /// </summary>
        public int Index { get; set; } = -1;

        /// <summary>
        /// Returns a string representation of the event.
        /// </summary>
        /// <returns>Formatted string with index and time.</returns>
        public virtual string AsString()
        {
            return $"ind: {Index} | time: {Time}";
        }
    }

    /// <summary>
    /// Represents a touch event within a gesture.
    /// </summary>
    public class Touch : Event
    {
        /// <summary>
        /// The position of the touch on the screen.
        /// </summary>
        public Vector2 Position { get; set; } = Vector2.Zero;

        /// <summary>
        /// Indicates whether the touch is pressed or released.
        /// </summary>
        public bool Pressed { get; set; }

        /// <summary>
        /// Returns a string representation of the touch event.
        /// </summary>
        /// <returns>Formatted string with position and pressed state.</returns>
        public override string AsString()
        {
            return $"{base.AsString()} | pos: {Position} | pressed: {Pressed}";
        }
    }

    /// <summary>
    /// Represents a drag event within a gesture.
    /// </summary>
    public class Drag : Event
    {
        /// <summary>
        /// The current position of the drag.
        /// </summary>
        public Vector2 Position { get; set; } = Vector2.Zero;

        /// <summary>
        /// The relative movement since the last update.
        /// </summary>
        public Vector2 Relative { get; set; } = Vector2.Zero;

        /// <summary>
        /// The velocity of the drag.
        /// </summary>
        public Vector2 Velocity { get; set; } = Vector2.Zero;

        /// <summary>
        /// Returns a string representation of the drag event.
        /// </summary>
        /// <returns>Formatted string with position and relative movement.</returns>
        public override string AsString()
        {
            return $"{base.AsString()} | pos: {Position} | relative: {Relative}";
        }
    }

    #endregion

    #region Constants

    /// <summary>
    /// Number of microseconds in a second.
    /// </summary>
    private const int SEC_IN_USEC = 1000000;

    #endregion

    #region Private Variables

    /// <summary>
    /// Dictionary mapping touch indices to their press events.
    /// </summary>
    private Dictionary<int, Touch> presses = new Dictionary<int, Touch>();

    /// <summary>
    /// Dictionary mapping touch indices to their release events.
    /// </summary>
    private Dictionary<int, Touch> releases = new Dictionary<int, Touch>();

    /// <summary>
    /// Dictionary mapping drag indices to their drag events.
    /// </summary>
    private Dictionary<int, Drag> drags = new Dictionary<int, Drag>();

    /// <summary>
    /// Nested dictionary storing the history of events categorized by type.
    /// Key: Touch Index
    /// Value: Dictionary where Key is Event Type ("presses", "releases", "drags") and Value is a List of Events.
    /// </summary>
    private Dictionary<int, Dictionary<string, List<Event>>> history = new Dictionary<int, Dictionary<string, List<Event>>>();

    /// <summary>
    /// The number of active touches currently detected.
    /// </summary>
    private int activeTouches = 0;

    /// <summary>
    /// The start time of the current gesture in seconds.
    /// </summary>
    private float startTime = -1f; // (secs)

    /// <summary>
    /// The elapsed time since the start of the current gesture in seconds.
    /// </summary>
    private float elapsedTime = -1f; // (secs)

    #endregion

    #region Public Properties

    /// <summary>
    /// Gets the dictionary of press events.
    /// </summary>
    public Dictionary<int, Touch> Presses => presses;

    /// <summary>
    /// Gets the dictionary of release events.
    /// </summary>
    public Dictionary<int, Touch> Releases => releases;

    /// <summary>
    /// Gets the dictionary of drag events.
    /// </summary>
    public Dictionary<int, Drag> Drags => drags;

    /// <summary>
    /// Gets the number of active touches currently detected.
    /// </summary>
    public int ActiveTouches => activeTouches;

    /// <summary>
    /// Gets the elapsed time since the start of the current gesture in seconds.
    /// </summary>
    public float ElapsedTime => elapsedTime;

    #endregion

    #region Public Methods

    /// <summary>
    /// Returns the number of current presses.
    /// </summary>
    /// <returns>Count of current presses.</returns>
    public int Size()
    {
        return presses.Count;
    }

    /// <summary>
    /// Calculates the centroid of specified events and property.
    /// </summary>
    /// <param name="eventsName">The name of the events collection ("presses", "releases", "drags").</param>
    /// <param name="propertyName">The property name to extract from each event ("position", "relative").</param>
    /// <returns>The centroid as a Vector2.</returns>
    public Vector2 Centroid(string eventsName, string propertyName)
    {
        List<Event> eventsList = GetEventsIndex(eventsName);
        List<Vector2> vectors = new List<Vector2>();

        foreach (var evt in eventsList)
        {
            switch (propertyName.ToLower())
            {
                case "position":
                    if (evt is Touch touch)
                        vectors.Add(touch.Position);
                    else if (evt is Drag drag)
                        vectors.Add(drag.Position);
                    break;
                case "relative":
                    if (evt is Drag dragRelative)
                        vectors.Add(dragRelative.Relative);
                    break;
                // Add additional properties if necessary
                default:
                    GD.PrintErr($"Unknown propertyName: {propertyName}");
                    break;
            }
        }

        if (vectors.Count == 0)
            return Vector2.Zero;

        return CalculateCentroid(vectors);
    }

    /// <summary>
    /// Calculates the centroid (average) of a list of Vector2 positions.
    /// </summary>
    /// <param name="vectors">List of Vector2 positions.</param>
    /// <returns>The centroid as a Vector2.</returns>
    private Vector2 CalculateCentroid(List<Vector2> vectors)
    {
        Vector2 sum = Vector2.Zero;
        foreach (var v in vectors)
        {
            sum += v;
        }
        return sum / vectors.Count;
    }

    /// <summary>
    /// Retrieves the end positions from presses, drags, and releases.
    /// </summary>
    /// <returns>A dictionary mapping indices to their end positions.</returns>
    public Dictionary<int, Vector2> GetEnds()
    {
        Dictionary<int, Vector2> ends = new Dictionary<int, Vector2>();

        foreach (var kvp in presses)
        {
            ends[kvp.Key] = kvp.Value.Position;
        }

        foreach (var kvp in drags)
        {
            ends[kvp.Key] = kvp.Value.Position;
        }

        foreach (var kvp in releases)
        {
            ends[kvp.Key] = kvp.Value.Position;
        }

        return ends;
    }

    /// <summary>
    /// Checks for gesture consistency based on difference and length limits.
    /// </summary>
    /// <param name="diffLimit">The maximum allowed difference between start and end positions.</param>
    /// <param name="lengthLimit">The maximum allowed length for relative positions. Defaults to -1 (no limit).</param>
    /// <returns>True if consistent; otherwise, false.</returns>
    public bool IsConsistent(float diffLimit, float lengthLimit = -1f)
    {
        if (lengthLimit == -1f)
        {
            // Assign a default value if needed
            // For example:
            lengthLimit = 100f; // Example default value
        }

        var ends = GetEnds();

        Vector2 endsCentroid = Centroid("drags", "position");
        Vector2 startsCentroid = Centroid("presses", "position");

        foreach (var kvp in ends)
        {
            int i = kvp.Key;
            Vector2 endPos = kvp.Value;

            if (!presses.ContainsKey(i))
                continue;

            Vector2 startRelative = presses[i].Position - startsCentroid;
            Vector2 endRelative = endPos - endsCentroid;

            bool valid = startRelative.Length() < lengthLimit &&
                         endRelative.Length() < lengthLimit &&
                         (endRelative - startRelative).Length() < diffLimit;

            if (!valid)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Rolls back the gesture history relative to a specified time.
    /// </summary>
    /// <param name="threshold">The relative time to roll back to.</param>
    /// <returns>The updated RawGesture after rollback.</returns>
    public RawGesture RollbackRelative(float threshold)
    {
        // Implement rollback logic based on threshold
        // Placeholder implementation: Reset internal state or manipulate data as needed
        return new RawGesture();
    }

    /// <summary>
    /// Rolls back the gesture history to an absolute time.
    /// </summary>
    /// <param name="time">The absolute time to roll back to.</param>
    /// <returns>The updated RawGesture after rollback.</returns>
    public RawGesture RollbackAbsolute(float time)
    {
        // Implement rollback logic based on absolute time
        // Placeholder implementation: Reset internal state or manipulate data as needed
        return new RawGesture();
    }

    /// <summary>
    /// Retrieves the latest event ID based on the specified time.
    /// </summary>
    /// <param name="latestTime">The time to compare against. Defaults to -1.</param>
    /// <returns>A tuple containing the latest index and type, or null if none found.</returns>
    public Tuple<int, string> LatestEventId(float latestTime = -1f)
    {
        Tuple<int, string> res = null;
        float maxTime = latestTime;

        foreach (var index in history.Keys)
        {
            foreach (var type in history[index].Keys)
            {
                var eventTime = history[index][type][history[index][type].Count - 1].Time;
                if (eventTime >= maxTime)
                {
                    res = Tuple.Create(index, type);
                    maxTime = eventTime;
                }
            }
        }

        return res;
    }

    /// <summary>
    /// Provides a string representation of the RawGesture.
    /// </summary>
    /// <returns>A string detailing presses, drags, and releases.</returns>
    public override string ToString()
    {
        string txt = "presses:";
        foreach (var e in presses.Values)
        {
            txt += "\n" + e.AsString();
        }
        txt += "\ndrags:";
        foreach (var e in drags.Values)
        {
            txt += "\n" + e.AsString();
        }
        txt += "\nreleases:";
        foreach (var e in releases.Values)
        {
            txt += "\n" + e.AsString();
        }
        return txt;
    }

    /// <summary>
    /// Updates the gesture state based on a screen drag event.
    /// </summary>
    /// <param name="event">The screen drag event.</param>
    /// <param name="time">The time of the event. Defaults to -1 (current time).</param>
    public void UpdateScreenDrag(InputEventScreenDrag @event, float time = -1f)
    {
        if (time < 0f)
        {
            time = GestureHelpers.Now();
        }

        Drag drag = new Drag
        {
            Position = @event.Position,
            Relative = @event.Relative,
            Velocity = @event.Velocity,
            Index = @event.Index,
            Time = time
        };

        AddHistory(@event.Index, "drags", drag);
        drags[@event.Index] = drag;
        elapsedTime = time - startTime;
    }

    /// <summary>
    /// Updates the gesture state based on a screen touch event.
    /// </summary>
    /// <param name="event">The screen touch event.</param>
    /// <param name="time">The time of the event. Defaults to -1 (current time).</param>
    public void UpdateScreenTouch(InputEventScreenTouch @event, float time = -1f)
    {
        if (time < 0f)
        {
            time = GestureHelpers.Now();
        }

        Touch touch = new Touch
        {
            Position = @event.Position,
            Pressed = @event.Pressed,
            Index = @event.Index,
            Time = time
        };

        if (@event.Pressed)
        {
            AddHistory(@event.Index, "presses", touch);
            presses[@event.Index] = touch;
            activeTouches += 1;
            releases.Remove(@event.Index);
            drags.Remove(@event.Index);
            if (activeTouches == 1)
            {
                startTime = time;
            }
        }
        else
        {
            AddHistory(@event.Index, "releases", touch);
            releases[@event.Index] = touch;
            activeTouches -= 1;
            drags.Remove(@event.Index);
        }

        elapsedTime = time - startTime;
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Maps the event name to its corresponding list of events.
    /// </summary>
    /// <param name="eventsName">The name of the events collection ("presses", "releases", "drags").</param>
    /// <returns>A list of events corresponding to the specified event name.</returns>
    private List<Event> GetEventsIndex(string eventsName)
    {
        List<Event> events = new List<Event>();
        switch (eventsName.ToLower())
        {
            case "presses":
                foreach (var touch in presses.Values)
                    events.Add(touch);
                break;
            case "releases":
                foreach (var touch in releases.Values)
                    events.Add(touch);
                break;
            case "drags":
                foreach (var drag in drags.Values)
                    events.Add(drag);
                break;
            default:
                GD.PrintErr($"Unknown eventsName: {eventsName}");
                break;
        }
        return events;
    }

    /// <summary>
    /// Adds an event to the history.
    /// </summary>
    /// <param name="index">The index of the event.</param>
    /// <param name="type">The type of the event ("presses", "releases", "drags").</param>
    /// <param name="value">The event object.</param>
    private void AddHistory(int index, string type, Event value)
    {
        if (!history.ContainsKey(index))
        {
            history[index] = new Dictionary<string, List<Event>>();
        }

        if (!history[index].ContainsKey(type))
        {
            history[index][type] = new List<Event>();
        }

        history[index][type].Add(value);
    }

    #endregion
}
