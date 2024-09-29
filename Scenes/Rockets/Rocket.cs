using Godot;
using System;

public partial class Rocket : CharacterBody3D
{
	[Export]
	private float Smoothness = 5f;  // Коэффициент плавности изменения скорости

	private Camera3D _camera;  // Ссылка на камеру
	private Vector3 _targetPosition;  // Целевая позиция на основе мыши

	public override void _Ready()
	{
		// Получаем камеру из текущего вьюпорта
		_camera = GetViewport().GetCamera3D();

		// Инициализируем начальную целевую позицию ракетой
		_targetPosition = Position;
	}

	public override void _Process(double delta)
	{
		// Получаем позицию мыши в координатах экрана
		Vector2 mousePosition = GetViewport().GetMousePosition();
		
		// Обновляем целевую позицию на основе текущего положения мыши
		UpdateTargetPosition(mousePosition);
		
		// Двигаем ракету к целевой позиции с учетом Smoothness
		MoveRocket((float)delta);
	}

	// Обработка ввода для сенсорных экранов
	public override void _Input(InputEvent @event)
	{
		// Если сенсорное касание экрана (первое нажатие)
		if (@event is InputEventScreenTouch touchEvent && touchEvent.Pressed)
		{
			// Обновляем целевую позицию для сенсорного нажатия
			Vector2 touchPosition = touchEvent.Position;
			UpdateTargetPosition(touchPosition);
		}
		// Если пользователь проводит пальцем по экрану
		else if (@event is InputEventScreenDrag dragEvent)
		{
			// Обновляем целевую позицию на основе перемещения пальца
			Vector2 dragPosition = dragEvent.Position;
			UpdateTargetPosition(dragPosition);
		}
	}

	private void UpdateTargetPosition(Vector2 screenPosition)
	{
		// Получаем мировые координаты позиции сенсорного ввода или мыши
		_targetPosition = GetWorldMousePosition(screenPosition);
	}
	
	private void MoveRocket(float delta)
	{
		// Двигаем ракету плавно по оси X и Z
		Vector3 newPosition = Position;

		// Интерполяция между текущей и целевой позициями с учетом smoothness
		newPosition.X = Mathf.Lerp(newPosition.X, _targetPosition.X, Smoothness * delta);
		newPosition.Z = Mathf.Lerp(newPosition.Z, _targetPosition.Z, Smoothness * delta);
		
		// Обновляем позицию ракеты
		Position = newPosition;
	}

	// Метод для получения мировых координат мыши или касания экрана
	private Vector3 GetWorldMousePosition(Vector2 screenPosition)
	{
		// Проецируем позицию мыши или касания на луч из камеры
		Vector3 from = _camera.ProjectRayOrigin(screenPosition);
		Vector3 direction = _camera.ProjectRayNormal(screenPosition);

		// Рассчитываем пересечение луча с плоскостью Z = 0
		float intersectionDistance = -from.Y / direction.Y;
		Vector3 worldPosition = from + direction * intersectionDistance;

		return worldPosition;
	}
}
