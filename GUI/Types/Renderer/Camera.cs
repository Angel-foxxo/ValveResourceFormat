using System;
using System.Drawing;
using System.Numerics;
using System.Windows.Forms;
using GUI.Utils;
using OpenTK.Graphics.OpenGL;

namespace GUI.Types.Renderer
{
    class Camera
    {
        [Flags]
        public enum TrackedKeys
        {
            None = 0,
            Shift = 1 << 0,
            Alt = 1 << 1,
            Forward = 1 << 2,
            Left = 1 << 3,
            Back = 1 << 4,
            Right = 1 << 5,
            Up = 1 << 6,
            Down = 1 << 7,
        }

        private const float MovementSpeed = 300f; // WASD movement, per second
        private const float AltMovementSpeed = 10f; // Holding shift or alt movement

        private readonly float[] SpeedModifiers = new float[]
        {
            0.1f,
            0.5f,
            1.0f,
            2.0f,
            5.0f,
            10.0f,
        };
        private int CurrentSpeedModifier = 2;

        public Vector3 Location { get; private set; }
        public float Pitch { get; private set; }
        public float Yaw { get; private set; }
        public float Scale { get; set; } = 1.0f;

        private Matrix4x4 ProjectionMatrix;
        public Matrix4x4 CameraViewMatrix { get; private set; }
        public Matrix4x4 ViewProjectionMatrix { get; private set; }
        public Frustum ViewFrustum { get; } = new Frustum();
        public PickingTexture Picker { get; set; }

        // Set from outside this class by forms code
        public bool MouseOverRenderArea { get; set; }

        private Vector2 WindowSize;
        private float AspectRatio;

        private bool MouseDragging;

        private Point MouseDelta;
        private Point MousePreviousPosition;

        public Camera()
        {
            Location = Vector3.One;
            LookAt(Vector3.Zero);
        }

        private void RecalculateMatrices()
        {
            CameraViewMatrix = Matrix4x4.CreateScale(Scale) * Matrix4x4.CreateLookAt(Location, Location + GetForwardVector(), Vector3.UnitZ);
            ViewProjectionMatrix = CameraViewMatrix * ProjectionMatrix;
            ViewFrustum.Update(ViewProjectionMatrix);
        }

        // Calculate forward vector from pitch and yaw
        private Vector3 GetForwardVector()
        {
            var yawSin = MathF.Sin(Yaw);
            var yawCos = MathF.Cos(Yaw);
            var pitchSin = MathF.Sin(Pitch);
            var pitchCos = MathF.Cos(Pitch);
            return new Vector3(yawCos * pitchCos, yawSin * pitchCos, pitchSin);
        }

        private Vector3 GetUpVector()
        {
            var yawSin = MathF.Sin(Yaw);
            var yawCos = MathF.Cos(Yaw);
            var pitchSin = MathF.Sin(Pitch);
            var pitchCos = MathF.Cos(Pitch);
            return new Vector3(yawCos * pitchSin, yawSin * pitchSin, pitchCos);
        }

        private Vector3 GetRightVector()
        {
            return new Vector3(MathF.Cos(Yaw - OpenTK.MathHelper.PiOver2), MathF.Sin(Yaw - OpenTK.MathHelper.PiOver2), 0);
        }

        public void SetViewConstants(UniformBuffers.ViewConstants viewConstants)
        {
            viewConstants.WorldToProjection = ProjectionMatrix;
            viewConstants.WorldToView = CameraViewMatrix;
            viewConstants.ViewToProjection = ViewProjectionMatrix;
            viewConstants.CameraPosition = Location / Scale;
        }

        public void SetViewportSize(int viewportWidth, int viewportHeight)
        {
            // Store window size and aspect ratio
            AspectRatio = viewportWidth / (float)viewportHeight;
            WindowSize = new Vector2(viewportWidth, viewportHeight);

            // Calculate projection matrix
            ProjectionMatrix = Matrix4x4.CreatePerspectiveFieldOfView(GetFOV(), AspectRatio, 1.0f, 20000.0f);

            RecalculateMatrices();

            // setup viewport
            GL.Viewport(0, 0, viewportWidth, viewportHeight);

            Picker?.Resize(viewportWidth, viewportHeight);
        }

        public void CopyFrom(Camera fromOther)
        {
            AspectRatio = fromOther.AspectRatio;
            WindowSize = fromOther.WindowSize;
            Location = fromOther.Location;
            Pitch = fromOther.Pitch;
            Yaw = fromOther.Yaw;
            ProjectionMatrix = fromOther.ProjectionMatrix;
            CameraViewMatrix = fromOther.CameraViewMatrix;
        }

        public void SetScaledProjectionMatrix()
        {
            ProjectionMatrix = Matrix4x4.CreatePerspectiveFieldOfView(GetFOV(), AspectRatio, 10f * Scale, 20000.0f * Scale);
        }

        public void SetLocation(Vector3 location)
        {
            Location = location;
            RecalculateMatrices();
        }

        public void SetLocationPitchYaw(Vector3 location, float pitch, float yaw)
        {
            Location = location;
            Pitch = pitch;
            Yaw = yaw;
            RecalculateMatrices();
        }

        public void LookAt(Vector3 target)
        {
            var dir = Vector3.Normalize(target - Location);
            Yaw = MathF.Atan2(dir.Y, dir.X);
            Pitch = MathF.Asin(dir.Z);

            ClampRotation();
            RecalculateMatrices();
        }

        public void SetFromTransformMatrix(Matrix4x4 matrix)
        {
            Location = matrix.Translation;

            // Extract view direction from view matrix and use it to calculate pitch and yaw
            var dir = new Vector3(matrix.M11, matrix.M12, matrix.M13);
            Yaw = MathF.Atan2(dir.Y, dir.X);
            Pitch = MathF.Asin(dir.Z);

            RecalculateMatrices();
        }

        public void Tick(float deltaTime, Point mouseState, MouseButtons mouseButtons, TrackedKeys keyboardState)
        {
            HandleMouseInput(mouseState, mouseButtons);

            if (!MouseOverRenderArea)
            {
                return;
            }

            if ((keyboardState & TrackedKeys.Shift) > 0)
            {
                // Camera truck and pedestal movement (blender calls this pan)
                var speed = AltMovementSpeed * deltaTime * SpeedModifiers[CurrentSpeedModifier];

                Location += GetUpVector() * speed * -MouseDelta.Y;
                Location += GetRightVector() * speed * MouseDelta.X;
            }
            else if ((keyboardState & TrackedKeys.Alt) > 0)
            {
                // Move forward or backwards when holding alt
                var totalDelta = MouseDelta.X + (MouseDelta.Y * -1);
                var speed = AltMovementSpeed * deltaTime * SpeedModifiers[CurrentSpeedModifier];

                Location += GetForwardVector() * totalDelta * speed;
            }
            else
            {
                // Use the keyboard state to update position
                HandleKeyboardInput(deltaTime, keyboardState);

                // Full width of the screen is a 1 PI (180deg)
                Yaw -= MathF.PI * MouseDelta.X / WindowSize.X;
                Pitch -= MathF.PI / AspectRatio * MouseDelta.Y / WindowSize.Y;
            }

            ClampRotation();

            RecalculateMatrices();
        }

        public float ModifySpeed(bool increase)
        {
            if (increase)
            {
                CurrentSpeedModifier += 1;

                if (CurrentSpeedModifier >= SpeedModifiers.Length)
                {
                    CurrentSpeedModifier = SpeedModifiers.Length - 1;
                }
            }
            else
            {
                CurrentSpeedModifier -= 1;

                if (CurrentSpeedModifier < 0)
                {
                    CurrentSpeedModifier = 0;
                }
            }

            return SpeedModifiers[CurrentSpeedModifier];
        }

        private const int LeftAndRightMouseButtons = (int)(MouseButtons.Left | MouseButtons.Right);

        private void HandleMouseInput(Point mouseState, MouseButtons mouseButtons)
        {
            if (MouseOverRenderArea && ((int)mouseButtons & LeftAndRightMouseButtons) > 0)
            {
                if (!MouseDragging)
                {
                    MouseDragging = true;
                    MousePreviousPosition = mouseState;
                }

                MouseDelta.X = mouseState.X - MousePreviousPosition.X;
                MouseDelta.Y = mouseState.Y - MousePreviousPosition.Y;

                MousePreviousPosition = mouseState;
            }

            if (!MouseOverRenderArea || ((int)mouseButtons & LeftAndRightMouseButtons) == 0)
            {
                MouseDragging = false;
                MouseDelta = default;
            }
        }

        private void HandleKeyboardInput(float deltaTime, TrackedKeys keyboardState)
        {
            var speed = MovementSpeed * deltaTime * SpeedModifiers[CurrentSpeedModifier];

            if ((keyboardState & TrackedKeys.Forward) > 0)
            {
                Location += GetForwardVector() * speed;
            }

            if ((keyboardState & TrackedKeys.Back) > 0)
            {
                Location -= GetForwardVector() * speed;
            }

            if ((keyboardState & TrackedKeys.Right) > 0)
            {
                Location += GetRightVector() * speed;
            }

            if ((keyboardState & TrackedKeys.Left) > 0)
            {
                Location -= GetRightVector() * speed;
            }

            if ((keyboardState & TrackedKeys.Down) > 0)
            {
                Location += new Vector3(0, 0, -speed);
            }

            if ((keyboardState & TrackedKeys.Up) > 0)
            {
                Location += new Vector3(0, 0, speed);
            }
        }

        // Prevent camera from going upside-down
        private void ClampRotation()
        {
            const float PITCH_LIMIT = 89.5f * MathF.PI / 180f;

            if (Pitch >= PITCH_LIMIT)
            {
                Pitch = PITCH_LIMIT;
            }
            else if (Pitch <= -PITCH_LIMIT)
            {
                Pitch = -PITCH_LIMIT;
            }
        }

        private static float GetFOV()
        {
            return Settings.Config.FieldOfView * MathF.PI / 180f;
        }

        public static TrackedKeys RemapKey(Keys key) => key switch
        {
            Keys.W => TrackedKeys.Forward,
            Keys.A => TrackedKeys.Left,
            Keys.S => TrackedKeys.Back,
            Keys.D => TrackedKeys.Right,
            Keys.Q => TrackedKeys.Up,
            Keys.Z => TrackedKeys.Down,
            Keys.LShiftKey => TrackedKeys.Shift,
            Keys.LMenu => TrackedKeys.Alt,
            _ => TrackedKeys.None,
        };
    }
}
