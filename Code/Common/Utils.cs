using Godot;

namespace galacticinvasion.Code.Common;

public static class Utils
{
	public static (Vector2 minBounds, Vector2 maxBounds) CalculateScreenBounds(this Camera3D camera, Vector2 viewportSize)
	{
		Vector3 topLeft = ProjectScreenPositionToWorld(camera, new Vector2(0, 0));
		Vector3 topRight = ProjectScreenPositionToWorld(camera, new Vector2(viewportSize.X, 0));
		Vector3 bottomLeft = ProjectScreenPositionToWorld(camera, new Vector2(0, viewportSize.Y));
		Vector3 bottomRight = ProjectScreenPositionToWorld(camera, new Vector2(viewportSize.X, viewportSize.Y));

		// Вычисляем минимальные и максимальные значения по X и Y
		return (new Vector2(
			Mathf.Min(Mathf.Min(topLeft.X, bottomLeft.X), Mathf.Min(topRight.X, bottomRight.X)),
			Mathf.Min(Mathf.Min(topLeft.Y, bottomLeft.Y), Mathf.Min(topRight.Y, bottomRight.Y))
		),
		new Vector2(
			Mathf.Max(Mathf.Max(topLeft.X, bottomLeft.X), Mathf.Max(topRight.X, bottomRight.X)),
			Mathf.Max(Mathf.Max(topLeft.Y, bottomLeft.Y), Mathf.Max(topRight.Y, bottomRight.Y))
		));
	}

	public static Vector3 ProjectScreenPositionToWorld(this Camera3D camera, Vector2 screenPosition)
	{
		Vector3 from = camera.ProjectRayOrigin(screenPosition);
		Vector3 direction = camera.ProjectRayNormal(screenPosition);

		// Рассчитываем пересечение луча с плоскостью Z = planeZ (planeZ убран, предполагается, что плоскость на нуле)
		float t = /* planeZ */ -from.Z / direction.Z;
		return from + direction * t;
	}

	public static (float X, float Y) ProjectScreenPositionToWorldByXY(this Camera3D camera, Vector2 screenPosition)
	{
		Vector3 from = camera.ProjectRayOrigin(screenPosition);
		Vector3 direction = camera.ProjectRayNormal(screenPosition);

		// Рассчитываем пересечение луча с плоскостью Z = planeZ (planeZ убран, предполагается, что плоскость на нуле)
		float t = /* planeZ */ -from.Z / direction.Z;
		var translatedPosition = from + direction * t;
		return (translatedPosition.X, translatedPosition.Y);
	}
	
	public static Vector2 ConvertToVector2(float X, float Y) => new(X, Y);
	public static Vector2 ConvertToVector2(this Vector3 vectorToCut) => new(vectorToCut.X, vectorToCut.Y);
    public static Vector3 ConvertToVector3(float X = 0, float Y = 0, float Z = 0) => new(X, Y, Z);
	public static Vector3 ConvertToVector3(this Vector2 vectorToExtend) => new(vectorToExtend.X, vectorToExtend.Y, 0);

}
