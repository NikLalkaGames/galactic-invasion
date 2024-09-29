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

        // Вычисляем минимальные и максимальные значения по X и Z
        return (new Vector2(
            Mathf.Min(Mathf.Min(topLeft.X, bottomLeft.X), Mathf.Min(topRight.X, bottomRight.X)),
            Mathf.Min(Mathf.Min(topLeft.Z, bottomLeft.Z), Mathf.Min(topRight.Z, bottomRight.Z))
        ),
        new Vector2(
            Mathf.Max(Mathf.Max(topLeft.X, bottomLeft.X), Mathf.Max(topRight.X, bottomRight.X)),
            Mathf.Max(Mathf.Max(topLeft.Z, bottomLeft.Z), Mathf.Max(topRight.Z, bottomRight.Z))
        ));
    }

    public static Vector3 ProjectScreenPositionToWorld(this Camera3D camera, Vector2 screenPosition)
    {
        Vector3 from = camera.ProjectRayOrigin(screenPosition);
        Vector3 direction = camera.ProjectRayNormal(screenPosition);

        // Рассчитываем пересечение луча с плоскостью Y = planeY (planeY убран, предполагается, что плоскость на нуле)
        float t = /* planeY */ - from.Y / direction.Y;
        return from + direction * t;
    }

    public static (float X, float Z) ProjectScreenPositionToWorldByXZ(this Camera3D camera, Vector2 screenPosition)
    {
        Vector3 from = camera.ProjectRayOrigin(screenPosition);
        Vector3 direction = camera.ProjectRayNormal(screenPosition);

        // Рассчитываем пересечение луча с плоскостью Y = planeY (planeY убран, предполагается, что плоскость на нуле)
        float t = /* planeY */ -from.Y / direction.Y;
        var translatedPosition = from + direction * t;
        return (translatedPosition.X, translatedPosition.Z);
    }
}
