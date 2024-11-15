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
        private bool EnableUnbreakableFollowAfterPan;

        // Follow
        public Entity FollowedEntity { get; private set; }
        public bool InUnbreakableFollow; // If true, moving camera is disabled until Unfollow()

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
                    FollowedEntity = PostPanFollowEntity;
                    IsPanning = false;
                    if(EnableUnbreakableFollowAfterPan) InUnbreakableFollow = true;
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
            if(FollowedEntity != null)
            {
                CurrentPosition = FollowedEntity.WorldPosition;
                UpdatePosition();
            }
        }

        private void HandleInputs()
        {
            bool isUiElementFocussed = EventSystem.current.currentSelectedGameObject != null;
            if (isUiElementFocussed) return;
            bool canMoveCamera = !InUnbreakableFollow && !IsPanning;

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

            if (Input.GetKey(KeyCode.W) && canMoveCamera) // W - Move camera up
            {
                CurrentPosition.x -= moveSpeed * Mathf.Sin(Mathf.Deg2Rad * CurrentAngle) * Time.deltaTime;
                CurrentPosition.z -= moveSpeed * Mathf.Cos(Mathf.Deg2Rad * CurrentAngle) * Time.deltaTime;
                UpdatePosition();
                FollowedEntity = null;
            }
            if (Input.GetKey(KeyCode.A) && canMoveCamera) // A - Move camera left
            {
                CurrentPosition.x += moveSpeed * Mathf.Sin(Mathf.Deg2Rad * (CurrentAngle + 90)) * Time.deltaTime;
                CurrentPosition.z += moveSpeed * Mathf.Cos(Mathf.Deg2Rad * (CurrentAngle + 90)) * Time.deltaTime;
                UpdatePosition();
                FollowedEntity = null;
            }
            if (Input.GetKey(KeyCode.S) && canMoveCamera) // S - Move camera down
            {
                CurrentPosition.x += moveSpeed * Mathf.Sin(Mathf.Deg2Rad * CurrentAngle) * Time.deltaTime;
                CurrentPosition.z += moveSpeed * Mathf.Cos(Mathf.Deg2Rad * CurrentAngle) * Time.deltaTime;
                UpdatePosition();
                FollowedEntity = null;
            }
            if (Input.GetKey(KeyCode.D) && canMoveCamera) // D - Move camera right
            {
                CurrentPosition.x -= moveSpeed * Mathf.Sin(Mathf.Deg2Rad * (CurrentAngle + 90)) * Time.deltaTime;
                CurrentPosition.z -= moveSpeed * Mathf.Cos(Mathf.Deg2Rad * (CurrentAngle + 90)) * Time.deltaTime;
                UpdatePosition();
                FollowedEntity = null;
            }

            if (Input.mouseScrollDelta.y < 0 && !Input.GetKey(KeyCode.LeftControl) && !Input.GetKey(KeyCode.LeftAlt)) // Scroll down - Zoom out
            {
                CurrentZoom += ZOOM_SPEED;
                UpdatePosition();
            }
            if (Input.mouseScrollDelta.y > 0 && !Input.GetKey(KeyCode.LeftControl) && !Input.GetKey(KeyCode.LeftAlt)) // Scroll up - Zoom in
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

        public void PanTo(float time, Vector3 targetPos, Entity postPanFollowEntity = null, bool unbreakableFollow = false)
        {
            // Init pan
            IsPanning = true;
            PanSourcePosition = CurrentPosition;
            PanTargetPosition = targetPos;
            PanDuration = time;
            PostPanFollowEntity = postPanFollowEntity;
            PanDelay = 0f;
            EnableUnbreakableFollowAfterPan = unbreakableFollow;

            // Immediately end pan if we are already very close to target position
            if (Vector3.Distance(CurrentPosition, targetPos) <= 0.01f) PanDelay = PanDuration;
        }
        public void Unfollow()
        {
            FollowedEntity = null;
            InUnbreakableFollow = false;
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
