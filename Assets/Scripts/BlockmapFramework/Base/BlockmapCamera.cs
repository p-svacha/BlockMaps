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
        private const float SHIFT_MOVE_SPEED = 50f;

        private const float ZOOM_SPEED = 0.8f;
        private const float CAMERA_ANGLE = 1f; // 1f = 45 degrees, the lower the value the higher the camera
        private const float MIN_HEIGHT = 2f;
        private const float MAX_HEIGHT = 40f;

        // Pan animation
        public bool IsPanning { get; private set; }
        private float PanDuration;
        private float PanDelay;
        private Vector3 PanSourcePosition;
        private Vector3 PanTargetPosition;
        private Entity PostPanFollowEntity;

        // Follow
        public Entity FollowEntity { get; private set; }

        // Camera Position
        private float CurrentAngle;
        private float OffsetRadius;
        private float CurrentZoom;
        private Vector3 CurrentPosition; // Camera is currently looking at this position

        public void Update()
        {
            UpdatePanAnimation();
            UpdateFollow();
            HandleInputs();
        }

        private void UpdatePanAnimation()
        {
            if(IsPanning)
            {
                PanDelay += Time.deltaTime;

                if(PanDelay >= PanDuration) // Pan done
                {
                    CurrentPosition = PanTargetPosition;
                    UpdatePosition();
                    FollowEntity = PostPanFollowEntity;
                    IsPanning = false;
                }

                else // Pan in progress
                {
                    CurrentPosition = HelperFunctions.SmoothLerp(PanSourcePosition, PanTargetPosition, (PanDelay / PanDuration));
                    UpdatePosition();
                }
            }
        }

        private void UpdateFollow()
        {
            if(FollowEntity != null)
            {
                CurrentPosition = FollowEntity.WorldPosition;
                UpdatePosition();
            }
        }

        private void HandleInputs()
        {
            bool isUiElementFocussed = EventSystem.current.currentSelectedGameObject != null;
            if (isUiElementFocussed) return;
            if (IsPanning) return;


            float moveSpeed = MOVE_SPEED;
            if (Input.GetKey(KeyCode.LeftShift)) moveSpeed = SHIFT_MOVE_SPEED;

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
                CurrentPosition.x -= moveSpeed * Mathf.Sin(Mathf.Deg2Rad * CurrentAngle) * Time.deltaTime;
                CurrentPosition.z -= moveSpeed * Mathf.Cos(Mathf.Deg2Rad * CurrentAngle) * Time.deltaTime;
                UpdatePosition();
                FollowEntity = null;
            }
            if (Input.GetKey(KeyCode.A)) // A - Move camera left
            {
                CurrentPosition.x += moveSpeed * Mathf.Sin(Mathf.Deg2Rad * (CurrentAngle + 90)) * Time.deltaTime;
                CurrentPosition.z += moveSpeed * Mathf.Cos(Mathf.Deg2Rad * (CurrentAngle + 90)) * Time.deltaTime;
                UpdatePosition();
                FollowEntity = null;
            }
            if (Input.GetKey(KeyCode.S)) // S - Move camera down
            {
                CurrentPosition.x += moveSpeed * Mathf.Sin(Mathf.Deg2Rad * CurrentAngle) * Time.deltaTime;
                CurrentPosition.z += moveSpeed * Mathf.Cos(Mathf.Deg2Rad * CurrentAngle) * Time.deltaTime;
                UpdatePosition();
                FollowEntity = null;
            }
            if (Input.GetKey(KeyCode.D)) // D - Move camera right
            {
                CurrentPosition.x -= moveSpeed * Mathf.Sin(Mathf.Deg2Rad * (CurrentAngle + 90)) * Time.deltaTime;
                CurrentPosition.z -= moveSpeed * Mathf.Cos(Mathf.Deg2Rad * (CurrentAngle + 90)) * Time.deltaTime;
                UpdatePosition();
                FollowEntity = null;
            }

            if (Input.mouseScrollDelta.y < 0 && !Input.GetKey(KeyCode.LeftControl)) // Scroll down - Zoom out
            {
                CurrentZoom += ZOOM_SPEED;
                UpdatePosition();
            }
            if (Input.mouseScrollDelta.y > 0 && !Input.GetKey(KeyCode.LeftControl)) // Scroll up - Zoom in
            {
                CurrentZoom -= ZOOM_SPEED;
                UpdatePosition();
            }
        }

        private void UpdatePosition()
        {
            CurrentZoom = Mathf.Clamp(CurrentZoom, MIN_HEIGHT, MAX_HEIGHT);

            OffsetRadius = CAMERA_ANGLE * CurrentZoom;

            float cameraOffsetX = Mathf.Sin(Mathf.Deg2Rad * CurrentAngle) * OffsetRadius;
            float cameraOffsetY = Mathf.Cos(Mathf.Deg2Rad * CurrentAngle) * OffsetRadius;

            if (Camera == null) Camera = GetComponent<Camera>();
            Camera.transform.position = new Vector3(CurrentPosition.x + cameraOffsetX, CurrentPosition.y + CurrentZoom, CurrentPosition.z + cameraOffsetY);
            Camera.transform.LookAt(CurrentPosition);
        }

        public void SetPosition(Vector3 pos)
        {
            CurrentPosition = pos;
            UpdatePosition();
        }

        public void PanTo(float time, Vector3 targetPos, Entity postPanFollowEntity = null)
        {
            IsPanning = true;
            PanSourcePosition = CurrentPosition;
            PanTargetPosition = targetPos;
            PanDuration = time;
            PostPanFollowEntity = postPanFollowEntity;
            PanDelay = 0f;
        }
        public void Unfollow()
        {
            FollowEntity = null;
        }

        public void SetZoom(float height)
        {
            CurrentZoom = height;
            UpdatePosition();
        }

        public void SetAngle(float angle)
        {
            CurrentAngle = angle;
            UpdatePosition();
        }
    }
}
