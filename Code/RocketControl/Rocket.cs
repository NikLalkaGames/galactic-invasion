using galacticinvasion.Code.Common;
using Godot;

namespace galacticinvasion.Code.RocketControl;

public partial class Rocket : CharacterBody3D
{
	[Export]
	private float Smoothness = 5f;  // Коэффициент плавности изменения скорости
	[Export]
	private float _xBoundaryOffset = 8.5f;
	[Export]
	private float _yBoundaryOffset = 13f;
	[Export]
	private Camera3D _camera; 

	private Vector2 _inputPosition;
	private Vector3 _worldPosition;
	private Vector3 _targetPosition;

	private Vector2 _minBounds;  // Минимальные границы по X и Y
	private Vector2 _maxBounds;  // Максимальные границы по X и Y

	public override void _Ready()
	{
		// Установка камеры
		if (_camera is null)
		{
			GD.Print("DANGER!!! No game camera assigned, using default 3d camera");
			_camera = GetViewport().GetCamera3D();
		}

		// Вычисляем границы экрана в мировых координатах + изменение границ мира при изменении границы экрана
		UpdateScreenBounds();
		GetTree().Root.SizeChanged += UpdateScreenBounds;

		// Сохраняем позицию ракеты для будущего смещения
		_targetPosition = Position;
	}

	private void UpdateScreenBounds() =>
		(_minBounds, _maxBounds) = _camera.CalculateScreenBounds(GetViewport().GetVisibleRect().Size);

	// Обработка ввода для сенсорных экранов
	public override void _Input(InputEvent @event)
	{
		// Если сенсорное касание экрана (первое нажатие)
		if (@event is InputEventScreenTouch touchEvent && touchEvent.Pressed)
		{
			// Обновляем целевую позицию для сенсорного нажатия
			_inputPosition = touchEvent.Position;
		}
		// Если пользователь проводит пальцем по экрану
		else if (@event is InputEventScreenDrag dragEvent)
		{
			// Обновляем целевую позицию на основе перемещения пальца
			_inputPosition = dragEvent.Position;
		}
		else if (@event is InputEventMouse mouseEvent)
		{
			_inputPosition = mouseEvent.Position;
		}
	}

	public override void _Process(double delta)
	{
		// Управление ракетой

		// Обновляем целевую позицию на основе ввода (транслируем позицию на экране в позицию в мире)
		(_targetPosition.X, _targetPosition.Y) = _camera.ProjectScreenPositionToWorldByXY(_inputPosition);

		// Ограничиваем позицию ракеты границами камеры (посчитаны исключительно для камеры)
		LimitPositionByBoundaries();

		// Плавное движение ракеты с помощью Lerp
		Position = Position.Lerp(_targetPosition, Smoothness * (float)delta);
	}

	public void LimitPositionByBoundaries()
	{
		_targetPosition.X = Mathf.Clamp(_targetPosition.X, _minBounds.X + _xBoundaryOffset, _maxBounds.X - _xBoundaryOffset);
		_targetPosition.Y = Mathf.Clamp(_targetPosition.Y, _minBounds.Y + _yBoundaryOffset, _maxBounds.Y - _yBoundaryOffset);
	}

}
