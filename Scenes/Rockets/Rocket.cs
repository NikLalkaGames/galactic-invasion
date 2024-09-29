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

	public override void _Ready()
	{
		// Получаем камеру из текущего вьюпорта
		_camera = GetViewport().GetCamera3D();

		// Инициализируем начальную целевую позицию ракетой
		_targetPosition = Position;
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

        // Рассчитываем пересечение луча с плоскостью Z = 0
        float intersectionDistance = -from.Y / direction.Y;
        Vector3 worldPosition = from + direction * intersectionDistance;

        _targetPosition = worldPosition;
	}
}
