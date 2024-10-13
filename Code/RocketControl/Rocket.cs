using galacticinvasion.Code.Common;
using Godot;
using System;

namespace galacticinvasion.Code.RocketControl;

public partial class Rocket : CharacterBody3D
{
	private enum InputDevice
	{
		Mouse,
		Touch
	}

	[Export]
	private float _mouseSmoothness = 5f;
    [Export]
    private float _touchSmoothness = 80f;
	[Export]
	private float _xBoundaryOffset = 8.5f;
	[Export]
	private float _yBoundaryOffset = 13f;
	[Export]
	private Camera3D _camera;
	[Export]
	private InputDevice _inputDevice = InputDevice.Touch;

	private event Action<InputEvent> InputHandler;
	private event Action<double> MovementHandler;

	private Vector2 _inputPosition;
	private Vector3 _worldPosition;
	private Vector3 _previousPosition;
	private Vector3 _targetPosition;
	private Vector3 _touchDelta;

	private Vector2 _minBounds;  // Минимальные границы по X и Y
	private Vector2 _maxBounds;  // Максимальные границы по X и Y

	public override void _Ready()
	{
		// Camera setup
		if (_camera is null)
		{
			GD.Print("Danger. No game camera assigned, using default 3d camera");
			_camera = GetViewport().GetCamera3D();
		}

		// Calculate screen bounds + change world bounds when screen size changed
		UpdateScreenBounds();
		GetTree().Root.SizeChanged += UpdateScreenBounds;

		// Save initial target position for movement
		_targetPosition = Position;

        // Determine input and movement callbacks based on selected input type
		InputHandler = _inputDevice == InputDevice.Touch ? TouchInput : MouseInput;
		MovementHandler = _inputDevice == InputDevice.Touch ? TouchMovement : MouseMovement;
	}

    // Rocket input, yeah babe!
    public override void _Input(InputEvent @event)
	{
		InputHandler.Invoke(@event);
	}

    // Rocket movement, baby!
    public override void _Process(double delta)
	{
		MovementHandler.Invoke(delta);
    }

    #region Touch Control

    private void TouchInput(InputEvent @event)
    {
        if (@event is InputEventScreenTouch touchEvent && touchEvent.Pressed)
        {
            _inputPosition = touchEvent.Position;
        }
        else if (@event is InputEventScreenDrag dragEvent)
        {
            _inputPosition = dragEvent.Position;
        }

        /* TESTING AREA - Emulate touch by mouse input */
        //if (@event is InputEventMouseMotion mouseMotion)
        //{
        //    InputEventScreenDrag touchDrag = new InputEventScreenDrag();
        //    touchDrag.Position = mouseMotion.Position;
        //    touchDrag.Index = 0;
        //    touchDrag.Relative = mouseMotion.Relative;
        //    Input.ParseInputEvent(touchDrag);
        //}

    }

    private void TouchMovement(double delta)
    {
        // test
        //if (!Input.IsMouseButtonPressed(MouseButton.Left))
        //{
        //    return;
        //}

        TranslateAndClampPosition();

        // controlling with lerp inside local radius 
        if (Position.DistanceTo(_targetPosition) < 30f)
        {

            // Плавное движение ракеты с помощью Lerp
            Position = Position.Lerp(_targetPosition, _mouseSmoothness * (float)delta);

            return;
        }

        // ELSE controlling rocket by touch offsets

        // get difference vector between current touch and previous touch positions
        _touchDelta = _targetPosition - _previousPosition;

        // Update rocket position based on touch offset
        Position = Position.MoveToward(Position + _touchDelta, _touchSmoothness * (float)delta);
        //TranslateObjectLocal(_touchDelta * _touchSmoothness);     // experimental - don't working as expected, but working

        // save translated input position for the next frame (will be previous in next frame)
        _previousPosition = _targetPosition;
    }

    #endregion

    #region Mouse Control

    private void MouseInput(InputEvent @event)
    {
        if (@event is InputEventMouse mouseEvent)
        {
            _inputPosition = mouseEvent.Position;
        }
    }

    private void MouseMovement(double delta)
    {
        TranslateAndClampPosition();

        // Плавное движение ракеты в позицию ввода
        Position = Position.Lerp(_targetPosition, _mouseSmoothness * (float)delta);
    }

    #endregion

    #region Input Translation and Boundaries

    private void TranslateAndClampPosition()
    {
        // translate screen input into world position and save into _targetPosition
        (_targetPosition.X, _targetPosition.Y) = _camera.ProjectScreenPositionToWorldByXY(_inputPosition);

        // limit position by boundaries
        _targetPosition.X = Mathf.Clamp(_targetPosition.X, _minBounds.X + _xBoundaryOffset, _maxBounds.X - _xBoundaryOffset);
        _targetPosition.Y = Mathf.Clamp(_targetPosition.Y, _minBounds.Y + _yBoundaryOffset, _maxBounds.Y - _yBoundaryOffset);
    }

    private void UpdateScreenBounds() =>
		(_minBounds, _maxBounds) = _camera.CalculateScreenBounds(GetViewport().GetVisibleRect().Size);

    #endregion

}
