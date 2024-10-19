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
	private float _mouseMoveSpeed = 5f;
    [Export]
    private float _touchMoveSpeed = 20f;
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

    private Vector2 _lastTouchPosition;
    private Vector2 _lastDragPosition;
    private Vector2 _touchDelta;
    private Vector3 _targetOffset;
    private bool isTouching;
    private bool isDragging;
    private bool IsDragFreezed;

    private Vector2 _mousePosition;
	private Vector3 _targetPosition;
	private Vector3 _previousPosition;
	private Vector3 _moveDelta;

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
        _previousPosition = Position;

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
        // Проверяем касание пальца
        if (@event is InputEventScreenTouch touchEvent)
        {
            if (touchEvent.Pressed)
            {
                // Начало касания — запоминаем стартовую позицию
                _lastTouchPosition = touchEvent.Position;
                isTouching = true;
            }
            else
            {
                // Завершение касания
                isTouching = false;
            }
        }

        // Проверка перемещения пальца (drag)
        if (@event is InputEventScreenDrag dragEvent && isTouching)
        {
            // Вычисляем разницу в позиции (смещение пальца)
            _touchDelta = dragEvent.Position - _lastTouchPosition;

            // Обновляем последнюю позицию пальца для следующего кадра
            _lastTouchPosition = dragEvent.Position;
        }

    }

    private void TouchMovement(double delta)
    {
        if (isTouching)
        {
            _targetOffset = Utils.ConvertToVector3(_touchDelta.X, -_touchDelta.Y);
            _targetPosition = Position + _targetOffset * _touchMoveSpeed * (float)delta;
        }

        ClampPosition(ref _targetPosition);
        Position = _targetPosition;
    }

    #endregion

    #region Mouse Control

    private void MouseInput(InputEvent @event)
    {
        if (@event is InputEventMouseMotion mouseEvent)
        {
            _mousePosition = mouseEvent.Position;
        }
    }

    private void MouseMovement(double delta)
    {
        (_targetPosition.X, _targetPosition.Y) = _camera.ProjectScreenPositionToWorldByXY(_mousePosition);
        ClampPosition(ref _targetPosition);

        // Плавное движение ракеты в позицию ввода
        Position = Position.Lerp(_targetPosition, _mouseMoveSpeed * (float)delta);
    }

    #endregion

    #region Boundaries

    /// <summary>
    /// Limit <paramref name="positionToClamp"/> by boundaries calculated and stored in _minBounds and _maxBounds 
    /// </summary>
    /// <param name="positionToClamp">Position that need to be limited by boundaries (_minBounds and _maxBounds)</param>
    private void ClampPosition(ref Vector3 positionToClamp)
    {
        positionToClamp.X = Mathf.Clamp(positionToClamp.X, _minBounds.X + _xBoundaryOffset, _maxBounds.X - _xBoundaryOffset);
        positionToClamp.Y = Mathf.Clamp(positionToClamp.Y, _minBounds.Y + _yBoundaryOffset, _maxBounds.Y - _yBoundaryOffset);
    }

    private void UpdateScreenBounds() =>
		(_minBounds, _maxBounds) = _camera.CalculateScreenBounds(GetViewport().GetVisibleRect().Size);

    #endregion

}
