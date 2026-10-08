using ValveResourceFormat.Renderer.SceneNodes;
using ValveResourceFormat.ResourceTypes;

namespace ValveResourceFormat.Renderer;

/// <summary>
/// A part of a <see cref="TransformGizmo"/> that can be grabbed.
/// </summary>
public enum GizmoHandle
{
    /// <summary>Nothing.</summary>
    None,

    /// <summary>Arrow moving along world X.</summary>
    MoveX,

    /// <summary>Arrow moving along world Y.</summary>
    MoveY,

    /// <summary>Arrow moving along world Z.</summary>
    MoveZ,

    /// <summary>Square moving across the world XY plane.</summary>
    MoveXY,

    /// <summary>Square moving across the world XZ plane.</summary>
    MoveXZ,

    /// <summary>Square moving across the world YZ plane.</summary>
    MoveYZ,

    /// <summary>Ring turning about world up, changing yaw.</summary>
    Yaw,

    /// <summary>Ring turning about the object's right, changing pitch.</summary>
    Pitch,

    /// <summary>Ring turning about the object's forward, changing roll.</summary>
    Roll,
}

/// <summary>
/// The view a <see cref="TransformGizmo"/> is drawn and picked in, captured from a camera.
/// </summary>
/// <param name="Eye">Camera position.</param>
/// <param name="Forward">Camera look direction.</param>
/// <param name="ViewProjection">World to clip transform.</param>
/// <param name="ViewportSize">Viewport size in pixels, which is also the space mouse positions are in.</param>
/// <param name="TanHalfFovY">Tangent of half the vertical field of view.</param>
public readonly record struct GizmoView(Vector3 Eye, Vector3 Forward, Matrix4x4 ViewProjection, Vector2 ViewportSize, float TanHalfFovY)
{
    /// <summary>Captures the view of a camera whose matrices are up to date.</summary>
    public static GizmoView FromCamera(Camera camera)
    {
        ArgumentNullException.ThrowIfNull(camera);

        return new(camera.Location, camera.Forward, camera.ViewProjectionMatrix, camera.WindowSize, MathF.Tan(camera.GetFOV() * 0.5f));
    }

    /// <summary>
    /// Projects a world position to pixels, with y growing downwards. False for points behind the camera.
    /// </summary>
    public bool Project(Vector3 world, out Vector2 screen)
    {
        var clip = Vector4.Transform(new Vector4(world, 1f), ViewProjection);

        if (clip.W <= 1e-4f)
        {
            screen = default;
            return false;
        }

        var ndc = new Vector2(clip.X, clip.Y) / clip.W;
        screen = new Vector2((ndc.X * 0.5f + 0.5f) * ViewportSize.X, (0.5f - ndc.Y * 0.5f) * ViewportSize.Y);
        return true;
    }
}

/// <summary>
/// An on screen handle for moving an object along the world axes and turning it by its euler angles:
/// three arrows and three squares for position, one per axis and one per plane between two axes, and
/// rings for yaw, pitch and roll.
/// </summary>
public class TransformGizmo
{
    /// <summary>How much of the viewport's height the gizmo spans, whatever its distance.</summary>
    private const float ScreenFraction = 0.13f;

    /// <summary>How close in pixels the mouse has to be to a handle to grab it.</summary>
    private const float PickPixels = 9f;

    private const float RingRadius = 0.7f;
    private const float ShaftStart = 0.2f;
    private const int RingSegments = 64;

    // Plane squares sit between their two arrows, inside the rings
    private const float PlaneStart = 0.25f;
    private const float PlaneEnd = 0.45f;

    private static readonly Color32 HighlightColor = new(1f, 0.9f, 0.2f, 1f);
    private static readonly GizmoHandle[] RingHandles = [GizmoHandle.Yaw, GizmoHandle.Pitch, GizmoHandle.Roll];
    private static readonly GizmoHandle[] PlaneHandles = [GizmoHandle.MoveXY, GizmoHandle.MoveXZ, GizmoHandle.MoveYZ];

    private Vector3 dragPivot;
    private Vector3 dragAngles;
    private float dragAxisStart;
    private float dragDialStart;
    private Vector3 dragPlaneStart;
    private Vector3 dragAxis;

    /// <summary>The handle under the mouse, drawn highlighted.</summary>
    public GizmoHandle Hovered { get; set; }

    /// <summary>The handle being dragged, or <see cref="GizmoHandle.None"/>.</summary>
    public GizmoHandle Active { get; private set; }

    /// <summary>World size of the gizmo at <paramref name="pivot"/>, so it keeps the same size on screen.</summary>
    public static float Size(Vector3 pivot, GizmoView view)
    {
        var depth = MathF.Max(Vector3.Dot(pivot - view.Eye, view.Forward), 1f);
        return depth * view.TanHalfFovY * 2f * ScreenFraction;
    }

    /// <summary>The handle within grabbing distance of the mouse, closest first.</summary>
    /// <param name="mouse">Mouse position in pixels.</param>
    /// <param name="pivot">Object position.</param>
    /// <param name="angles">Object QAngle in degrees.</param>
    /// <param name="view">View the gizmo is drawn in.</param>
    public static GizmoHandle HitTest(Vector2 mouse, Vector3 pivot, Vector3 angles, GizmoView view)
    {
        var best = GizmoHandle.None;
        var bestDistance = PickPixels;

        foreach (var (handle, points) in Handles(pivot, angles, Size(pivot, view)))
        {
            // Squares can be grabbed anywhere inside, not just by their edges
            if (IsPlane(handle) && InsideProjected(mouse, points, view))
            {
                return handle;
            }

            for (var i = 1; i < points.Length; i++)
            {
                if (!view.Project(points[i - 1], out var a) || !view.Project(points[i], out var b))
                {
                    continue;
                }

                var distance = DistanceToSegment(mouse, a, b);

                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = handle;
                }
            }
        }

        return best;
    }

    /// <summary>
    /// Starts dragging the handle under the mouse, if there is one.
    /// </summary>
    /// <returns>Whether a handle was grabbed.</returns>
    public bool BeginDrag(Vector2 mouse, Vector3 pivot, Vector3 angles, GizmoView view)
    {
        var handle = HitTest(mouse, pivot, angles, view);

        if (handle == GizmoHandle.None)
        {
            return false;
        }

        Active = handle;
        dragPivot = pivot;
        dragAngles = angles;

        if (handle is GizmoHandle.MoveX or GizmoHandle.MoveY or GizmoHandle.MoveZ)
        {
            dragAxis = handle switch
            {
                GizmoHandle.MoveX => Vector3.UnitX,
                GizmoHandle.MoveY => Vector3.UnitY,
                _ => Vector3.UnitZ,
            };
            dragAxisStart = AxisParameter(mouse, view) ?? 0f;
        }
        else if (IsPlane(handle))
        {
            dragAxis = PlaneAxes(handle).Normal;
            dragPlaneStart = PlaneHit(mouse, view) ?? pivot;
        }
        else
        {
            dragAxis = RingAxis(handle, angles);
            dragDialStart = DialAngle(mouse, view) ?? 0f;
        }

        return true;
    }

    /// <summary>
    /// The object's position and angles for the mouse's current position during a drag.
    /// </summary>
    public (Vector3 Position, Vector3 Angles) Drag(Vector2 mouse, GizmoView view)
    {
        switch (Active)
        {
            case GizmoHandle.MoveX or GizmoHandle.MoveY or GizmoHandle.MoveZ:
            {
                // Looking straight down the axis leaves nothing to drag along, so hold still
                var parameter = AxisParameter(mouse, view);
                var position = parameter is { } t ? dragPivot + dragAxis * (t - dragAxisStart) : dragPivot;
                return (position, dragAngles);
            }

            case GizmoHandle.MoveXY or GizmoHandle.MoveXZ or GizmoHandle.MoveYZ:
            {
                // The object follows where the mouse ray meets the plane, from where it was grabbed; seen
                // edge on the plane gives no answer, so hold still
                var position = PlaneHit(mouse, view) is { } hit ? dragPivot + (hit - dragPlaneStart) : dragPivot;
                return (position, dragAngles);
            }

            case GizmoHandle.Yaw or GizmoHandle.Pitch or GizmoHandle.Roll:
            {
                if (DialAngle(mouse, view) is not { } dial)
                {
                    return (dragPivot, dragAngles);
                }

                // The dial is turned on screen; about an axis that faces the viewer, anticlockwise on
                // screen is anticlockwise about the axis, which with y down is a negative screen angle
                var facing = Vector3.Dot(dragAxis, view.Eye - dragPivot) >= 0f ? -1f : 1f;
                var degrees = float.RadiansToDegrees(WrapRadians(dial - dragDialStart)) * facing;

                // Turning forward about right lifts it, and pitch is positive looking down
                var angles = Active switch
                {
                    GizmoHandle.Yaw => dragAngles with { Y = dragAngles.Y + degrees },
                    GizmoHandle.Pitch => dragAngles with { X = Math.Clamp(dragAngles.X - degrees, -89f, 89f) },
                    _ => dragAngles with { Z = dragAngles.Z + degrees },
                };
                return (dragPivot, angles);
            }

            default:
                return (dragPivot, dragAngles);
        }
    }

    /// <summary>Ends a drag.</summary>
    public void EndDrag() => Active = GizmoHandle.None;

    /// <summary>Adds the gizmo's lines for an object to a line batch.</summary>
    public void AddLines(List<SimpleVertex> vertices, Vector3 pivot, Vector3 angles, GizmoView view)
    {
        ArgumentNullException.ThrowIfNull(vertices);

        var size = Size(pivot, view);

        foreach (var (handle, points) in Handles(pivot, angles, size))
        {
            var color = handle == Active || (Active == GizmoHandle.None && handle == Hovered) ? HighlightColor : ColorOf(handle);

            if (IsPlane(handle))
            {
                AddPlaneFill(vertices, handle, pivot, size, color, view);
            }

            for (var i = 1; i < points.Length; i++)
            {
                AddThickLine(vertices, points[i - 1], points[i], color, view);
            }

            if (handle is GizmoHandle.MoveX or GizmoHandle.MoveY or GizmoHandle.MoveZ)
            {
                AddArrowHead(vertices, points[0], points[^1], size * 0.12f, color, view);
            }
        }
    }

    /// <summary>Stroke width of the gizmo in pixels.</summary>
    private const float LineThicknessPixels = 3f;

    /// <summary>
    /// A line several pixels wide. The context only draws lines one pixel wide, so this lays parallel lines
    /// side by side across the screen, under a pixel apart so no gaps show between them.
    /// </summary>
    internal static void AddThickLine(List<SimpleVertex> vertices, Vector3 from, Vector3 to, Color32 color, GizmoView view,
        float thicknessPixels = LineThicknessPixels)
    {
        var middle = (from + to) * 0.5f;
        var across = Vector3.Cross(to - from, view.Eye - middle);

        if (across.LengthSquared() < 1e-8f || view.ViewportSize.Y <= 0f)
        {
            ShapeSceneNode.AddLine(vertices, from, to, color);
            return;
        }

        // World size of one pixel at this depth
        var depth = MathF.Max(Vector3.Dot(middle - view.Eye, view.Forward), 1f);
        var pixel = 2f * depth * view.TanHalfFovY / MathF.Max(view.ViewportSize.Y, 1f);
        across = Vector3.Normalize(across) * pixel;

        // Enough strands that neighbours are under a pixel apart
        var strands = Math.Max(2, (int)MathF.Ceiling(thicknessPixels / 0.75f) + 1);
        for (var k = 0; k < strands; k++)
        {
            var offset = across * thicknessPixels * ((float)k / (strands - 1) - 0.5f);
            ShapeSceneNode.AddLine(vertices, from + offset, to + offset, color);
        }
    }

    private static Color32 ColorOf(GizmoHandle handle) => handle switch
    {
        GizmoHandle.MoveX => new(1f, 0.25f, 0.25f, 1f),
        GizmoHandle.MoveY => new(0.3f, 1f, 0.3f, 1f),
        GizmoHandle.MoveZ => new(0.3f, 0.5f, 1f, 1f),

        // A square takes the colour of the axis it holds still
        GizmoHandle.MoveXY => new(0.3f, 0.5f, 1f, 1f),
        GizmoHandle.MoveXZ => new(0.3f, 1f, 0.3f, 1f),
        GizmoHandle.MoveYZ => new(1f, 0.25f, 0.25f, 1f),
        GizmoHandle.Yaw => new(0.3f, 0.5f, 1f, 0.9f),
        GizmoHandle.Pitch => new(1f, 0.35f, 0.35f, 0.9f),
        _ => new(0.9f, 0.9f, 0.9f, 0.9f),
    };

    /// <summary>Every handle as a polyline in world space.</summary>
    private static IEnumerable<(GizmoHandle Handle, Vector3[] Points)> Handles(Vector3 pivot, Vector3 angles, float size)
    {
        yield return (GizmoHandle.MoveX, [pivot + Vector3.UnitX * size * ShaftStart, pivot + Vector3.UnitX * size]);
        yield return (GizmoHandle.MoveY, [pivot + Vector3.UnitY * size * ShaftStart, pivot + Vector3.UnitY * size]);
        yield return (GizmoHandle.MoveZ, [pivot + Vector3.UnitZ * size * ShaftStart, pivot + Vector3.UnitZ * size]);

        foreach (var handle in PlaneHandles)
        {
            var (u, v, _) = PlaneAxes(handle);
            var near = size * PlaneStart;
            var far = size * PlaneEnd;

            yield return (handle,
            [
                pivot + u * near + v * near,
                pivot + u * far + v * near,
                pivot + u * far + v * far,
                pivot + u * near + v * far,
                pivot + u * near + v * near,
            ]);
        }

        foreach (var handle in RingHandles)
        {
            yield return (handle, Ring(pivot, RingAxis(handle, angles), size * RingRadius));
        }
    }

    private static bool IsPlane(GizmoHandle handle) => handle is GizmoHandle.MoveXY or GizmoHandle.MoveXZ or GizmoHandle.MoveYZ;

    /// <summary>The two world axes a square moves along, and the one it holds still.</summary>
    private static (Vector3 U, Vector3 V, Vector3 Normal) PlaneAxes(GizmoHandle handle) => handle switch
    {
        GizmoHandle.MoveXY => (Vector3.UnitX, Vector3.UnitY, Vector3.UnitZ),
        GizmoHandle.MoveXZ => (Vector3.UnitX, Vector3.UnitZ, Vector3.UnitY),
        _ => (Vector3.UnitY, Vector3.UnitZ, Vector3.UnitX),
    };

    /// <summary>A faint fill across a square, as lines are all there is to draw with.</summary>
    private static void AddPlaneFill(List<SimpleVertex> vertices, GizmoHandle handle, Vector3 pivot, float size, Color32 color, GizmoView view)
    {
        const int Lines = 6;
        var (u, v, _) = PlaneAxes(handle);
        var near = size * PlaneStart;
        var far = size * PlaneEnd;
        var fill = color with { A = 90 };

        for (var k = 0; k < Lines; k++)
        {
            var across = near + (far - near) * (k + 0.5f) / Lines;
            AddThickLine(vertices, pivot + u * near + v * across, pivot + u * far + v * across, fill, view, 4f);
        }
    }

    /// <summary>Whether the mouse is inside a closed outline once projected to the screen.</summary>
    private static bool InsideProjected(Vector2 mouse, Vector3[] outline, GizmoView view)
    {
        var positive = false;
        var negative = false;

        for (var i = 1; i < outline.Length; i++)
        {
            if (!view.Project(outline[i - 1], out var a) || !view.Project(outline[i], out var b))
            {
                return false;
            }

            // A convex outline has the point on the same side of every edge
            var side = (b.X - a.X) * (mouse.Y - a.Y) - (b.Y - a.Y) * (mouse.X - a.X);
            positive |= side > 0f;
            negative |= side < 0f;
        }

        return !(positive && negative);
    }

    /// <summary>
    /// Where the mouse ray meets the plane through the drag pivot square to the drag axis. Null when the
    /// plane is seen edge on, or lies behind the camera.
    /// </summary>
    private Vector3? PlaneHit(Vector2 mouse, GizmoView view)
    {
        var (origin, direction) = MouseRay(mouse, view);
        var facing = Vector3.Dot(dragAxis, direction);

        if (MathF.Abs(facing) < 0.02f)
        {
            return null;
        }

        var distance = Vector3.Dot(dragAxis, dragPivot - origin) / facing;
        return distance > 0f ? origin + direction * distance : null;
    }

    /// <summary>The axis a ring turns about: world up for yaw, the object's right for pitch, its forward for roll.</summary>
    private static Vector3 RingAxis(GizmoHandle handle, Vector3 angles)
    {
        // Rows of a rotation are forward, left and up
        var rotation = EntityTransformHelper.EulerAnglesToRotationMatrix(angles with { Z = 0f });

        return handle switch
        {
            GizmoHandle.Yaw => Vector3.UnitZ,
            GizmoHandle.Pitch => -rotation.GetRow(1).AsVector3(),
            _ => rotation.GetRow(0).AsVector3(),
        };
    }

    private static Vector3[] Ring(Vector3 center, Vector3 axis, float radius)
    {
        var u = MathUtils.SafeNormalize(Vector3.Cross(axis, MathF.Abs(axis.Z) < 0.9f ? Vector3.UnitZ : Vector3.UnitX));
        var v = Vector3.Cross(axis, u);
        var points = new Vector3[RingSegments + 1];

        for (var i = 0; i <= RingSegments; i++)
        {
            var (sin, cos) = MathF.SinCos(MathF.Tau * i / RingSegments);
            points[i] = center + (u * cos + v * sin) * radius;
        }

        return points;
    }

    private static void AddArrowHead(List<SimpleVertex> vertices, Vector3 from, Vector3 tip, float length, Color32 color, GizmoView view)
    {
        var direction = Vector3.Normalize(tip - from);
        var side = MathUtils.SafeNormalize(Vector3.Cross(direction, MathF.Abs(direction.Z) < 0.9f ? Vector3.UnitZ : Vector3.UnitX));
        var other = Vector3.Cross(direction, side);
        var back = tip - direction * length;

        AddThickLine(vertices, tip, back + side * length * 0.4f, color, view);
        AddThickLine(vertices, tip, back - side * length * 0.4f, color, view);
        AddThickLine(vertices, tip, back + other * length * 0.4f, color, view);
        AddThickLine(vertices, tip, back - other * length * 0.4f, color, view);
    }

    /// <summary>
    /// Where along the drag axis the mouse ray passes closest, measured from the drag pivot. Null when the
    /// axis points along the ray.
    /// </summary>
    private float? AxisParameter(Vector2 mouse, GizmoView view)
    {
        var (origin, direction) = MouseRay(mouse, view);

        // Closest points of two lines: the axis through the pivot and the ray from the eye
        var along = Vector3.Dot(dragAxis, direction);
        var denominator = 1f - along * along;

        if (denominator < 1e-4f)
        {
            return null;
        }

        var offset = dragPivot - origin;
        return (along * Vector3.Dot(direction, offset) - Vector3.Dot(dragAxis, offset)) / denominator;
    }

    /// <summary>Angle of the mouse around the pivot on screen, in radians. Null right on top of it.</summary>
    private float? DialAngle(Vector2 mouse, GizmoView view)
    {
        if (!view.Project(dragPivot, out var center))
        {
            return null;
        }

        var offset = mouse - center;
        return offset.LengthSquared() < 4f ? null : MathF.Atan2(offset.Y, offset.X);
    }

    private static (Vector3 Origin, Vector3 Direction) MouseRay(Vector2 mouse, GizmoView view)
    {
        if (!Matrix4x4.Invert(view.ViewProjection, out var inverse))
        {
            return (view.Eye, view.Forward);
        }

        var ndc = new Vector2(mouse.X / view.ViewportSize.X * 2f - 1f, 1f - mouse.Y / view.ViewportSize.Y * 2f);

        // Reverse depth: 1 is the near plane, and smaller values lie further out
        var near = Vector4.Transform(new Vector4(ndc, 1f, 1f), inverse);
        var far = Vector4.Transform(new Vector4(ndc, 0.5f, 1f), inverse);
        var direction = far.AsVector3() / far.W - near.AsVector3() / near.W;

        return (view.Eye, MathUtils.SafeNormalize(direction, view.Forward));
    }

    private static float DistanceToSegment(Vector2 point, Vector2 a, Vector2 b)
    {
        var ab = b - a;
        var lengthSquared = ab.LengthSquared();
        var t = lengthSquared > 0f ? Math.Clamp(Vector2.Dot(point - a, ab) / lengthSquared, 0f, 1f) : 0f;
        return Vector2.Distance(point, a + ab * t);
    }

    private static float WrapRadians(float angle) => MathUtils.Wrap(angle, -MathF.PI, MathF.PI);
}
