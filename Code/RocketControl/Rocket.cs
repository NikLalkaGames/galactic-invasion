using galacticinvasion.Code.Common;
using Godot;
using System;

namespace galacticinvasion.Code.RocketControl;

public partial class Rocket : CharacterBody3D
{
    #region InputDevice

    private enum InputDevice
    {
        Mouse,
        Touch
    }

    #endregion

    #region Export fields, customize for design and technical stability
    [Export]
    private float _mouseMoveSpeed = 5f;
    [Export]
    private float _touchMoveSpeed = 20f;
    /// <summary>
    /// Minimum distance between one touch and another at which the drag will be considered stopped
    /// </summary>
    [Export()]
    private float _dragTolerance = 1f;
    [Export]
    private float _xBoundaryOffset = 8.5f;
    [Export]
    private float _yBoundaryOffset = 13f;
    [Export]
    private Camera3D _camera;
    [Export]
    private InputDevice _inputDevice = InputDevice.Touch;

    #endregion

    #region Input and movement callbacks

    private event Action<InputEvent> InputHandler;
    private event Action<double> MovementHandler;

    #endregion

    #region Fields (Input and Movement)

    private Vector2 _lastDragPosition;
    private Vector2 _dragDelta;
    private Vector3 _targetOffset;
    private bool _isTouching;
    private bool _isDragging;
    private bool _IsDragStopped;

    private Vector2 _mousePosition;
    private Vector3 _targetPosition;
    private Vector3 _previousPosition;
    private Vector3 _moveDelta;

    private Vector2 _minBounds;  // Минимальные границы по X и Y
    private Vector2 _maxBounds;  // Максимальные границы по X и Y

    #endregion

    #region Init

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

    #endregion

    #region Main loops with delegation

    public override void _Input(InputEvent @event)
    {
        InputHandler.Invoke(@event);
    }

    public override void _Process(double delta)
    {
        MovementHandler.Invoke(delta);
    }

    #endregion

    #region Touch Control

    private void TouchInput(InputEvent @event)
    {
        if (@event is InputEventScreenTouch touchEvent)
        {
            if (touchEvent.Pressed)
            {
                _isTouching = true;
            }
            else
            {
                _isTouching = false;
                _isDragging = false;
            }
        }

        if (@event is InputEventScreenDrag dragEvent && _isTouching)
        {

            if (!_isDragging)
            {
                _isDragging = true;
                _lastDragPosition = dragEvent.Position;
            }
            else
            {
                if (dragEvent.Position.DistanceTo(_lastDragPosition) <= _dragTolerance)
                {
                    _IsDragStopped = true;
                    GD.Print("Позиция касания находится примерно в одном месте");
                }
                else
                {
                    _IsDragStopped = false;
                    GD.Print("Позиция касания изменилась");
                }

                _dragDelta = dragEvent.Position - _lastDragPosition;

                _lastDragPosition = dragEvent.Position;
            }
        }

    }

    private void TouchMovement(double delta)
    {
        ClampPosition(ref _targetPosition);

        Position = _targetPosition;

        if (!_isTouching || _IsDragStopped)
            return;

        _targetOffset = Utils.ConvertToVector3(_dragDelta.X, -_dragDelta.Y);

        _targetPosition = Position + _targetOffset * _touchMoveSpeed * (float)delta;
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
