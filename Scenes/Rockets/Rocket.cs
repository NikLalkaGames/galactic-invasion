using Godot;
using System;

public partial class Rocket : CharacterBody3D
{
	[Export]
	public float Speed = 40f; // Скорость реакции на ввод

	[Export]
	public float MaxSpeed = 120f; // Максимальная скорость ракеты по оси X

	[Export]
	public float Smoothness = 1f; // Плавность изменения скорости

	// TODO: Поменять значение, когда будет бесконечная сцена в движением вперед, 
	// значение отрицательное
	[Export]
	public float UpwardSpeed = 0f; // Постоянная скорость движения вверх по оси Z

	[Export]
	public float Sensitivity = 0.1f; // Коэффициент чувствительности к вводу

	private Vector3 _desiredVelocity = Vector3.Zero; // Желаемая скорость
	private Camera3D camera; // Ссылка на камеру

	public override void _Ready()
	{
		// Инициализация камеры
		camera = GetViewport().GetCamera3D();
		if (camera == null)
		{
			GD.PrintErr("Камера не найдена в текущем Viewport!");
		}
	}

	public override void _Input(InputEvent @event)
	{
		// Обработка ввода мыши
		if (@event is InputEventMouseMotion mouseMotion)
		{
			Vector2 relative = mouseMotion.Relative;
			if (relative != Vector2.Zero)
			{
				ProcessInput(relative);
			}
		}
		// Обработка ввода сенсорного экрана
		else if (@event is InputEventScreenDrag screenDrag)
		{
			Vector2 relative = screenDrag.Relative;
			if (relative != Vector2.Zero)
			{
				ProcessInput(relative);
			}
		}
	}

	private void ProcessInput(Vector2 relative)
	{
		if (camera == null)
		{
			GD.PrintErr("Камера не найдена в текущем Viewport!");
			return;
		}

		// Рассчитываем движение только по оси X (влево-вправо)
		float inputX = relative.X * Sensitivity;

		// Обновляем желаемую скорость по оси X
		_desiredVelocity.X = inputX * Speed;
	}

	public override void _PhysicsProcess(double delta)
	{
		// Постоянное движение вверх по оси Z
		_desiredVelocity.Z = UpwardSpeed;

		// Плавно изменяем текущую скорость в направлении желаемой скорости
		Velocity = Velocity.Lerp(_desiredVelocity, Smoothness * (float)delta);

		// Ограничение максимальной скорости по оси X
		if (Mathf.Abs(Velocity.X) > MaxSpeed)
		{
			Vector3 tempVelocity = Velocity;
			tempVelocity.X = Mathf.Sign(tempVelocity.X) * MaxSpeed;
			Velocity = tempVelocity;
		}

		// Перемещение ракеты
		MoveAndSlide();
		
		Rotation = new Vector3(Rotation.X, 0, Rotation.Z);

		// TODO: Подумать нужно ли, потому что уже сейчас получился красивый эффект параллакса ракеты
		// Поворот ракеты в направлении движения по оси X
		//if (Mathf.Abs(Velocity.X) > 0.01f)
		//{
			//// Наклоняем ракету вокруг оси Y для визуального эффекта
			//Rotation = new Vector3(Rotation.X, -Velocity.X / MaxSpeed, Rotation.Z);
		//}
		//else
		//{
			//// Возвращаем ракету в исходное положение
			//Rotation = new Vector3(Rotation.X, 0, Rotation.Z);
		//}
	}
}
