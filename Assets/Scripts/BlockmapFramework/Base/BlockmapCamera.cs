using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace BlockmapFramework
{
    public class BlockmapCamera : MonoBehaviour
    {
        public Camera Camera;

        private const float TURN_SPEED = 80f;
        private const float MOVE_SPEED = 10f;
        private const float ZOOM_SPEED = 0.8f;
        private const float OFFSET_RADIUS_SCALE = 1f; // How far the camera is from the center depending on the height 
        private const float MIN_HEIGHT = 2f;
        private const float MAX_HEIGHT = 40f;

        // Camera Position
        private float CurrentAngle;
        private float OffsetRadius;
        private float CurrentHeight;
        private Vector2 CurrentPosition; // Camera is currently looking at this position

        public void Update()
        {
            HandleInputs();
        }

        private void HandleInputs()
        {
            bool isUiElementFocussed = EventSystem.current.currentSelectedGameObject != null;
            if (isUiElementFocussed) return;

            if (Input.GetKey(KeyCode.Q)) // Q - Rotate camera anti-clockwise
            {
                CurrentAngle = CurrentAngle += TURN_SPEED * Time.deltaTime;
                UpdatePosition();
            }
            if (Input.GetKey(KeyCode.E)) // E - Rotate camera clockwise
            {
                CurrentAngle = CurrentAngle -= TURN_SPEED * Time.deltaTime;
                UpdatePosition();
            }

            if (Input.GetKey(KeyCode.W)) // W - Move camera up
            {
                CurrentPosition.x -= MOVE_SPEED * Mathf.Sin(Mathf.Deg2Rad * CurrentAngle) * Time.deltaTime;
                CurrentPosition.y -= MOVE_SPEED * Mathf.Cos(Mathf.Deg2Rad * CurrentAngle) * Time.deltaTime;
                UpdatePosition();
            }
            if (Input.GetKey(KeyCode.A)) // A - Move camera left
            {
                CurrentPosition.x += MOVE_SPEED * Mathf.Sin(Mathf.Deg2Rad * (CurrentAngle + 90)) * Time.deltaTime;
                CurrentPosition.y += MOVE_SPEED * Mathf.Cos(Mathf.Deg2Rad * (CurrentAngle + 90)) * Time.deltaTime;
                UpdatePosition();
            }
            if (Input.GetKey(KeyCode.S)) // S - Move camera down
            {
                CurrentPosition.x += MOVE_SPEED * Mathf.Sin(Mathf.Deg2Rad * CurrentAngle) * Time.deltaTime;
                CurrentPosition.y += MOVE_SPEED * Mathf.Cos(Mathf.Deg2Rad * CurrentAngle) * Time.deltaTime;
                UpdatePosition();
            }
            if (Input.GetKey(KeyCode.D)) // D - Move camera right
            {
                CurrentPosition.x -= MOVE_SPEED * Mathf.Sin(Mathf.Deg2Rad * (CurrentAngle + 90)) * Time.deltaTime;
                CurrentPosition.y -= MOVE_SPEED * Mathf.Cos(Mathf.Deg2Rad * (CurrentAngle + 90)) * Time.deltaTime;
                UpdatePosition();
            }

            if (Input.mouseScrollDelta.y < 0 && !Input.GetKey(KeyCode.LeftControl)) // Scroll down - Zoom out
            {
                CurrentHeight += ZOOM_SPEED;
                UpdatePosition();
            }
            if (Input.mouseScrollDelta.y > 0 && !Input.GetKey(KeyCode.LeftControl)) // Scroll up - Zoom in
            {
                CurrentHeight -= ZOOM_SPEED;
                UpdatePosition();
            }
        }

        private void UpdatePosition()
        {
            CurrentHeight = Mathf.Clamp(CurrentHeight, MIN_HEIGHT, MAX_HEIGHT);

            OffsetRadius = OFFSET_RADIUS_SCALE * CurrentHeight;

            float cameraOffsetX = Mathf.Sin(Mathf.Deg2Rad * CurrentAngle) * OffsetRadius;
            float cameraOffsetY = Mathf.Cos(Mathf.Deg2Rad * CurrentAngle) * OffsetRadius;

            if (Camera == null) Camera = GetComponent<Camera>();
            Camera.transform.position = new Vector3(CurrentPosition.x + cameraOffsetX, CurrentHeight, CurrentPosition.y + cameraOffsetY);
            Camera.transform.LookAt(new Vector3(CurrentPosition.x, 0, CurrentPosition.y));
        }

        public void SetPosition(Vector2 pos)
        {
            CurrentPosition = pos;
            UpdatePosition();
        }

        public void SetZoom(float height)
        {
            CurrentHeight = height;
            UpdatePosition();
        }

        public void SetAngle(float angle)
        {
            CurrentAngle = angle;
            UpdatePosition();
        }
    }
}
