using UnityEngine;

namespace ThisIsBlast.Gameplay
{
    public static class CameraUtils
    {
        public static Vector3 GetPointerWorldPositionOnPlane(Camera camera, float planeY)
        {
            if (camera == null)
            {
                camera = Camera.main;
            }

            if (camera == null)
            {
                return Vector3.zero;
            }

            Ray ray = camera.ScreenPointToRay(Input.mousePosition);
            Plane dragPlane = new Plane(Vector3.up, new Vector3(0f, planeY, 0f));

            if (!dragPlane.Raycast(ray, out float distance))
            {
                return Vector3.zero;
            }

            return ray.GetPoint(distance);
        }
    }
}
