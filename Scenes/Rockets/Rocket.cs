using Godot;
using System;

public partial class Rocket : CharacterBody3D
{
	[Export]
	private float Smoothness = 5f;  // Коэффициент плавности изменения скорости

	private Camera3D _camera;  // Ссылка на камеру

	private Vector2 _touchPosition;
	private Vector2 _dragPosition;
	private Vector2 _mousePosition;

	private Vector3 _targetPosition;  // Целевая позиция для ввода

	private Vector2 _minBounds;  // Минимальные границы по X и Z
	private Vector2 _maxBounds;  // Максимальные границы по X и Z

	public override void _Ready()
	{
		// Получаем камеру из текущего вьюпорта
		_camera = GetViewport().GetCamera3D();

		// Инициализируем начальную целевую позицию ракетой
		_targetPosition = Position;

		// Вычисляем границы экрана в мировых координатах
		CalculateScreenBounds();
	}

	// Обработка ввода для сенсорных экранов
	public override void _Input(InputEvent @event)
	{
		// Если сенсорное касание экрана (первое нажатие)
		if (@event is InputEventScreenTouch touchEvent && touchEvent.Pressed)
		{
			// Обновляем целевую позицию для сенсорного нажатия
			_touchPosition = touchEvent.Position;
		}
		// Если пользователь проводит пальцем по экрану
		else if (@event is InputEventScreenDrag dragEvent)
		{
			// Обновляем целевую позицию на основе перемещения пальца
			_dragPosition = dragEvent.Position;
		}
	}

	public override void _Process(double delta)
	{
		// Получаем позицию мыши в координатах экрана
		_mousePosition = GetViewport().GetMousePosition();

		// Обновляем целевую позицию на основе текущего положения мыши
		TranslateToWorldPosition(_mousePosition);

		// Двигаем ракету к целевой позиции с учетом Smoothness
		Position = Position.Lerp(_targetPosition, Smoothness * (float)delta);
	}

	private void TranslateToWorldPosition(Vector2 screenPosition)
	{
		// Проецируем позицию мыши или касания на луч из камеры
		Vector3 from = _camera.ProjectRayOrigin(screenPosition);
		Vector3 direction = _camera.ProjectRayNormal(screenPosition);

		// Рассчитываем пересечение луча с плоскостью Y = 0
		float t = -from.Y / direction.Y;
		Vector3 worldPosition = from + direction * t;

		// Ограничиваем позицию по границам
		worldPosition.X = Mathf.Clamp(worldPosition.X, _minBounds.X, _maxBounds.X);
		worldPosition.Z = Mathf.Clamp(worldPosition.Z, _minBounds.Y, _maxBounds.Y);

		// Устанавливаем Y в 0, если нужно
		worldPosition.Y = 0;

		_targetPosition = worldPosition;
	}

	private void CalculateScreenBounds()
	{
		// Получаем размеры вьюпорта
		Vector2 viewportSize = GetViewport().GetVisibleRect().Size;

		// Плоскость на которой движется ракета (Y = 0)
		float planeY = 0f;

		// Проецируем углы экрана на плоскость движения ракеты
		Vector3 topLeft = ProjectScreenPositionToWorld(new Vector2(0, 0), planeY);
		Vector3 topRight = ProjectScreenPositionToWorld(new Vector2(viewportSize.X, 0), planeY);
		Vector3 bottomLeft = ProjectScreenPositionToWorld(new Vector2(0, viewportSize.Y), planeY);
		Vector3 bottomRight = ProjectScreenPositionToWorld(new Vector2(viewportSize.X, viewportSize.Y), planeY);

		// Вычисляем минимальные и максимальные значения по X и Z
		_minBounds = new Vector2(
			Mathf.Min(Mathf.Min(topLeft.X, bottomLeft.X), Mathf.Min(topRight.X, bottomRight.X)),
			Mathf.Min(Mathf.Min(topLeft.Z, bottomLeft.Z), Mathf.Min(topRight.Z, bottomRight.Z))
		);

		_maxBounds = new Vector2(
			Mathf.Max(Mathf.Max(topLeft.X, bottomLeft.X), Mathf.Max(topRight.X, bottomRight.X)),
			Mathf.Max(Mathf.Max(topLeft.Z, bottomLeft.Z), Mathf.Max(topRight.Z, bottomRight.Z))
		);
	}

	private Vector3 ProjectScreenPositionToWorld(Vector2 screenPosition, float planeY)
	{
		Vector3 from = _camera.ProjectRayOrigin(screenPosition);
		Vector3 direction = _camera.ProjectRayNormal(screenPosition);

		// Рассчитываем пересечение луча с плоскостью Y = planeY
		float t = (planeY - from.Y) / direction.Y;
		return from + direction * t;
	}
}
