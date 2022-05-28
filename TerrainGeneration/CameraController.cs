﻿using System;

using OpenTK;
using OpenTK.Input;

namespace TerrainGeneration
{
    /// <summary>
    /// Класс, который обрабатывает пользовательский ввод-вывод и управляет камерой.
    /// </summary>
    public abstract class CameraController : IDisposable
    {
        public Camera Camera { get; set; }
        public abstract void UpdateCamera(FrameEventArgs e);
        public abstract void UpdateCameraParams();
        public abstract void Dispose();
    }

    /// <summary>
    /// Контроллер камеры, который позволяет пользователю смотреть вокруг от первого лица.
    /// </summary>
    public class FirstPersonCameraController : CameraController
    {
        private Vector3 up = Vector3.UnitY;

        public float Phi = 0.0f;
        public float Theta = 0.0f;

        public float PhiVelocity = 0.01f;
        public float ThetaVelocity = 0.01f;
        public float MoveVelocity = 120.0f;

        protected float phiError = 0.1f;
        protected int lastMouseX = 0;
        protected int lastMouseY = 0;
        protected int lastMouseWheel = 0;

        protected KeyboardDevice keyboard;
        protected MouseDevice mouse;

        protected bool bUpdateCameraRotation = false;

        public FirstPersonCameraController(Camera camera, KeyboardDevice keyboard, MouseDevice mouse)
        {
            Camera = camera;

            this.keyboard = keyboard;
            this.mouse = mouse;

            mouse.ButtonDown += OnMouseDown;
            mouse.ButtonUp += OnMouseUp;

            ComputeSphericalCoordinates(camera);
            UpdateCameraParams();
        }

        protected void ComputeSphericalCoordinates(Camera camera)
        {
            var cameraToTarget = camera.Target - camera.Position;

            Theta = (float)Math.Atan2(cameraToTarget.Z, cameraToTarget.X);
            Phi = (float)Math.Acos(cameraToTarget.Y / cameraToTarget.Length);

            ClampAngles();
        }

        public Vector3 LookDirection
        {
            get
            {
                return new Vector3((float)(Math.Cos(Theta) * Math.Sin(Phi)), (float)Math.Cos(Phi), (float)(Math.Sin(Theta) * Math.Sin(Phi)));
            }
        }

        private void OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            bUpdateCameraRotation = false;
        }

        private void OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            bUpdateCameraRotation = true;

            lastMouseX = e.X;
            lastMouseY = e.Y;
        }

        public override void UpdateCamera(FrameEventArgs e)
        {
            var SideDirection = Vector3.Cross(LookDirection, up);

            // Перемещение камеры
            if (keyboard[Key.W])
                Camera.Position += LookDirection * MoveVelocity * (float)e.Time;
            if (keyboard[Key.S])
                Camera.Position -= LookDirection * MoveVelocity * (float)e.Time;
            if (keyboard[Key.A])
                Camera.Position -= SideDirection * MoveVelocity * (float)e.Time;
            if (keyboard[Key.D])
                Camera.Position += SideDirection * MoveVelocity * (float)e.Time;

            // Rotate camera if necessary
            if (bUpdateCameraRotation)
            {
                var deltaX = (float)(mouse.X - lastMouseX);
                var deltaY = (float)(mouse.Y - lastMouseY);

                lastMouseX = mouse.X;
                lastMouseY = mouse.Y;

                Phi += deltaY * PhiVelocity;
                Theta += deltaX * ThetaVelocity;

                ClampAngles();
            }

            UpdateCameraParams();
        }

        public void ClampAngles()
        {
            Phi = Clamp(Phi, phiError, (float)Math.PI - phiError);
            Theta = Theta % (float)(2.0 * Math.PI);
        }

        public override void UpdateCameraParams()
        {
            Camera.Target = Camera.Position + LookDirection;
        }

        public float Clamp(float value, float min, float max)
        {
            return Math.Min(Math.Max(value, min), max);
        }

        public override void Dispose()
        {
            mouse.ButtonDown -= OnMouseDown;
            mouse.ButtonUp -= OnMouseUp;
        }
    }

    /// <summary>
    /// Контроллер камеры, который позволяет вращаться вокруг точки поворота с помощью мыши и колесика мыши.
    /// </summary>
    public class RotationCameraController : CameraController
    {
        public float Phi = 0.0f;
        public float Theta = 0.0f;
        public float Radius = 64.0f;
        public const float MinRadius = 1.0f;
        public Vector3 CameraCenter = Vector3.Zero;

        public float PhiVelocity = 0.01f;
        public float ThetaVelocity = 0.01f;
        public float RadiusVelocity = 4.0f;

        protected int lastMouseX = 0;
        protected int lastMouseY = 0;
        protected int lastMouseWheel = 0;

        protected KeyboardDevice keyboard;
        protected MouseDevice mouse;

        protected bool bUpdateCamera = false;

        public RotationCameraController(Camera camera, KeyboardDevice keyboard, MouseDevice mouse)
        {
            Camera = camera;

            this.keyboard = keyboard;
            this.mouse = mouse;

            mouse.ButtonDown += OnMouseDown;
            mouse.ButtonUp += OnMouseUp;
            mouse.WheelChanged += OnMouseWheelChanged;

            UpdateCameraParams();
        }

        private void OnMouseWheelChanged(object sender, MouseWheelEventArgs e)
        {
            Radius += RadiusVelocity * (float)e.Delta;
            Radius = Math.Max(Radius, MinRadius);

            UpdateCameraParams();
        }

        private void OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            bUpdateCamera = false;
        }

        private void OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            bUpdateCamera = true;

            lastMouseX = e.X;
            lastMouseY = e.Y;
        }

        public override void UpdateCamera(FrameEventArgs e)
        {
            if (bUpdateCamera)
            {
                var deltaX = (float)(mouse.X - lastMouseX);
                var deltaY = (float)(mouse.Y - lastMouseY);

                lastMouseX = mouse.X;
                lastMouseY = mouse.Y;

                Phi -= deltaY * PhiVelocity;
                Theta += deltaX * ThetaVelocity;

                Phi = Clamp(Phi, 0.1f, (float)Math.PI / 2f);
                Theta = Theta % (float)(2.0 * Math.PI);

                UpdateCameraParams();
            }
        }

        public override void UpdateCameraParams()
        {
            Camera.Target = CameraCenter;
            Camera.Position = Camera.Target + Radius * new Vector3((float)(Math.Cos(Theta) * Math.Sin(Phi)), (float)Math.Cos(Phi), (float)(Math.Sin(Theta) * Math.Sin(Phi)));
        }

        public float Clamp(float value, float min, float max)
        {
            return Math.Min(Math.Max(value, min), max);
        }

        public override void Dispose()
        {
            mouse.ButtonDown -= OnMouseDown;
            mouse.ButtonUp -= OnMouseUp;
            mouse.WheelChanged -= OnMouseWheelChanged;
        }
    }
}
