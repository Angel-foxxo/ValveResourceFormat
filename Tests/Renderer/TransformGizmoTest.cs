using System.Threading.Tasks;
using ValveResourceFormat.Renderer;

namespace Tests.Renderer
{
    public class TransformGizmoTest
    {
        private static GizmoView ViewFrom(Vector3 eye, Vector3 target)
        {
            var camera = new Camera();
            camera.SetViewportSize(1280, 720);
            camera.SetLocation(eye);
            camera.LookAt(target);
            camera.RecalculateMatrices();
            return GizmoView.FromCamera(camera);
        }

        private static Vector2 Screen(GizmoView view, Vector3 world)
        {
            view.Project(world, out var screen);
            return screen;
        }

        /// <summary>
        /// Dragging an arrow moves the object along that axis by as much as the cursor moved along it.
        /// </summary>
        [Test]
        public async Task MoveArrowFollowsTheCursorAlongItsAxis()
        {
            var pivot = new Vector3(100f, 50f, 0f);
            var view = ViewFrom(pivot + new Vector3(-300f, -600f, 250f), pivot);
            var size = TransformGizmo.Size(pivot, view);
            var gizmo = new TransformGizmo();

            var grab = pivot + Vector3.UnitX * size * 0.9f;
            await Assert.That(gizmo.BeginDrag(Screen(view, grab), pivot, Vector3.Zero, view)).IsTrue();
            await Assert.That(gizmo.Active).IsEqualTo(GizmoHandle.MoveX);

            var (position, angles) = gizmo.Drag(Screen(view, grab + Vector3.UnitX * 40f), view);

            await Assert.That(position.X).IsEqualTo(pivot.X + 40f).Within(0.5f);
            await Assert.That(position.Y).IsEqualTo(pivot.Y).Within(1e-3f);
            await Assert.That(position.Z).IsEqualTo(pivot.Z).Within(1e-3f);
            await Assert.That(angles).IsEqualTo(Vector3.Zero);
        }

        /// <summary>
        /// Sweeping the yaw ring a quarter turn anticlockwise, seen from above, adds a quarter turn of yaw,
        /// the way yaw grows in a QAngle.
        /// </summary>
        [Test]
        public async Task YawRingTurnsTheWayYawGrows()
        {
            var pivot = new Vector3(0f, 0f, 0f);
            var view = ViewFrom(new Vector3(-1f, 0f, 600f), pivot);
            var radius = TransformGizmo.Size(pivot, view) * 0.7f;
            var gizmo = new TransformGizmo();

            // Grabbed between the arrows so the ring is what is under the cursor
            var start = pivot + new Vector3(MathF.Cos(MathF.PI / 4f), MathF.Sin(MathF.PI / 4f), 0f) * radius;
            var end = pivot + new Vector3(MathF.Cos(MathF.PI * 3f / 4f), MathF.Sin(MathF.PI * 3f / 4f), 0f) * radius;

            await Assert.That(gizmo.BeginDrag(Screen(view, start), pivot, Vector3.Zero, view)).IsTrue();
            await Assert.That(gizmo.Active).IsEqualTo(GizmoHandle.Yaw);

            var (_, angles) = gizmo.Drag(Screen(view, end), view);

            await Assert.That(angles.Y).IsEqualTo(90f).Within(1f);
            await Assert.That(angles.X).IsEqualTo(0f);
            await Assert.That(angles.Z).IsEqualTo(0f);
        }

        /// <summary>
        /// Lifting the front of the pitch ring tilts the view up, which is negative pitch.
        /// </summary>
        [Test]
        public async Task PitchRingTiltsTheWayItIsDragged()
        {
            var pivot = new Vector3(0f, 0f, 0f);

            // Looking at the object from its right, so the pitch ring faces the viewer
            var view = ViewFrom(new Vector3(0f, -600f, 0f), pivot);
            var radius = TransformGizmo.Size(pivot, view) * 0.7f;
            var gizmo = new TransformGizmo();

            // Its forward (+X) at 45 degrees below horizontal, swung up to 45 above
            var start = pivot + new Vector3(MathF.Cos(-MathF.PI / 4f), 0f, MathF.Sin(-MathF.PI / 4f)) * radius;
            var end = pivot + new Vector3(MathF.Cos(MathF.PI / 4f), 0f, MathF.Sin(MathF.PI / 4f)) * radius;

            await Assert.That(gizmo.BeginDrag(Screen(view, start), pivot, Vector3.Zero, view)).IsTrue();
            await Assert.That(gizmo.Active).IsEqualTo(GizmoHandle.Pitch);

            var (_, angles) = gizmo.Drag(Screen(view, end), view);

            await Assert.That(angles.X).IsEqualTo(-89f).Within(1f).Because("clamped at looking straight up");
        }

        /// <summary>
        /// Dragging a square moves the object across its plane to wherever the cursor is over it, and not
        /// off the plane.
        /// </summary>
        [Test]
        public async Task PlaneSquareFollowsTheCursorAcrossItsPlane()
        {
            var pivot = new Vector3(100f, 50f, 20f);
            var view = ViewFrom(pivot + new Vector3(-400f, -300f, 500f), pivot);
            var size = TransformGizmo.Size(pivot, view);
            var gizmo = new TransformGizmo();

            var grab = pivot + new Vector3(0.35f, 0.35f, 0f) * size;
            await Assert.That(gizmo.BeginDrag(Screen(view, grab), pivot, Vector3.Zero, view)).IsTrue();
            await Assert.That(gizmo.Active).IsEqualTo(GizmoHandle.MoveXY);

            var (position, _) = gizmo.Drag(Screen(view, grab + new Vector3(30f, -20f, 0f)), view);

            await Assert.That(position.X).IsEqualTo(pivot.X + 30f).Within(0.5f);
            await Assert.That(position.Y).IsEqualTo(pivot.Y - 20f).Within(0.5f);
            await Assert.That(position.Z).IsEqualTo(pivot.Z).Within(1e-3f);
        }

        [Test]
        public async Task MissingEverythingGrabsNothing()
        {
            var pivot = Vector3.Zero;
            var view = ViewFrom(new Vector3(-500f, 0f, 100f), pivot);
            var gizmo = new TransformGizmo();

            await Assert.That(gizmo.BeginDrag(new Vector2(5f, 5f), pivot, Vector3.Zero, view)).IsFalse();
            await Assert.That(gizmo.Active).IsEqualTo(GizmoHandle.None);
        }
    }
}
