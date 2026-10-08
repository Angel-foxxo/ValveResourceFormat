using ValveResourceFormat.Renderer.SceneNodes;
using ValveResourceFormat.ResourceTypes;

namespace ValveResourceFormat.Renderer;

/// <summary>
/// Draws a <see cref="CameraPath"/>: the route the camera takes, and a small view frustum at every keyframe.
/// </summary>
public class CameraPathRenderer : LineDebugRenderer
{
    private const float FrustumLength = 24f;
    private const float FrustumHalfWidth = 16f;
    private const float FrustumHalfHeight = 9f;

    private const float PathThicknessPixels = 4f;

    private static readonly Color32 PathOutlineColor = new(0.02f, 0.02f, 0.02f, 0.85f);
    private static readonly Color32 KeyframeColor = new(1f, 1f, 1f, 0.8f);
    private static readonly Color32 SelectedKeyframeColor = new(0.25f, 0.8f, 1f, 1f);

    private readonly List<SimpleVertex> vertices = [];
    private readonly List<Vector3> samplePositions = [];
    private float[] segmentSpeeds = [];
    private float[] sortedSpeeds = [];

    /// <summary>Creates the GPU line buffer.</summary>
    /// <param name="rendererContext">Renderer context for loading shaders.</param>
    public CameraPathRenderer(RendererContext rendererContext)
        : base(rendererContext, nameof(CameraPathRenderer))
    {
    }

    /// <summary>Rebuilds the lines for the path and draws them on top of the scene.</summary>
    /// <param name="path">Path to draw.</param>
    /// <param name="selectedKeyframe">Index of the keyframe to highlight, or -1 for none.</param>
    /// <param name="gizmo">Gizmo to draw on the selected keyframe, if any.</param>
    /// <param name="view">View the frame is drawn in, which sizes the route's line and the gizmo on screen.</param>
    public void Render(CameraPath path, int selectedKeyframe, TransformGizmo? gizmo = null, GizmoView view = default)
    {
        ArgumentNullException.ThrowIfNull(path);

        vertices.Clear();

        if (path.Keyframes.Count > 1)
        {
            AddPath(path, view);
        }

        for (var i = 0; i < path.Keyframes.Count; i++)
        {
            var keyframe = path.Keyframes[i];
            AddFrustum(keyframe.Position, keyframe.Angles, i == selectedKeyframe ? SelectedKeyframeColor : KeyframeColor);
        }

        if (gizmo != null && selectedKeyframe >= 0 && selectedKeyframe < path.Keyframes.Count)
        {
            var selected = path.Keyframes[selectedKeyframe];
            gizmo.AddLines(vertices, selected.Position, selected.Angles, view);
        }

        Upload(vertices);

        // Over everything, so the route and the gizmo stay visible through walls
        using var _ = GraphicsContext.RenderState.Scope(depthTest: false);
        RenderLines();
    }

    /// <summary>
    /// The route as a thick outlined line coloured by speed against the path's typical speed: green at it,
    /// shading to blue where the camera is slower and red where it is faster. A path at one speed is all green.
    /// </summary>
    private void AddPath(CameraPath path, GizmoView view)
    {
        const float SampleStep = 1f / 32f;

        samplePositions.Clear();

        foreach (var sample in path.Sample(SampleStep))
        {
            samplePositions.Add(sample.Position);
        }

        var segments = samplePositions.Count - 1;

        if (segments < 1)
        {
            return;
        }

        if (segmentSpeeds.Length < segments)
        {
            segmentSpeeds = new float[segments];
            sortedSpeeds = new float[segments];
        }

        for (var i = 0; i < segments; i++)
        {
            segmentSpeeds[i] = Vector3.Distance(samplePositions[i], samplePositions[i + 1]) / SampleStep;
        }

        // The last sample lands on the end of the path rather than a full step on, so its segment is short
        if (segments > 1)
        {
            segmentSpeeds[segments - 1] = segmentSpeeds[segments - 2];
        }

        Array.Copy(segmentSpeeds, sortedSpeeds, segments);
        Array.Sort(sortedSpeeds, 0, segments);
        var typical = MathF.Max(sortedSpeeds[segments / 2], 1e-3f);

        // Outline first, so the colour is drawn over it
        for (var i = 0; i < segments; i++)
        {
            TransformGizmo.AddThickLine(vertices, samplePositions[i], samplePositions[i + 1], PathOutlineColor, view, PathThicknessPixels + 3f);
        }

        for (var i = 0; i < segments; i++)
        {
            TransformGizmo.AddThickLine(vertices, samplePositions[i], samplePositions[i + 1], SpeedColor(segmentSpeeds[i] / typical), view, PathThicknessPixels);
        }
    }

    /// <summary>
    /// Colour for a speed as a ratio of the typical speed: about 70% of it or less is fully blue, about 140%
    /// or more fully red, so even modest changes of pace show.
    /// </summary>
    private static Color32 SpeedColor(float ratio)
    {
        var slow = new Vector3(0.25f, 0.55f, 1f);
        var typical = new Vector3(0.2f, 1f, 0.35f);
        var fast = new Vector3(1f, 0.25f, 0.2f);

        // Logarithmic, so slowing down and speeding up by the same factor are equally far from green
        var t = Math.Clamp(MathF.Log2(ratio) * 2f, -1f, 1f);
        var color = t < 0f ? Vector3.Lerp(typical, slow, -t) : Vector3.Lerp(typical, fast, t);

        return new Color32(color.X, color.Y, color.Z, 1f);
    }

    private void AddFrustum(Vector3 position, Vector3 angles, Color32 color)
    {
        // Rows of a rotation are forward, left and up
        var rotation = EntityTransformHelper.EulerAnglesToRotationMatrix(angles);
        var forward = rotation.GetRow(0).AsVector3();
        var left = rotation.GetRow(1).AsVector3();
        var up = rotation.GetRow(2).AsVector3();

        var center = position + forward * FrustumLength;
        var topLeft = center + left * FrustumHalfWidth + up * FrustumHalfHeight;
        var topRight = center - left * FrustumHalfWidth + up * FrustumHalfHeight;
        var bottomLeft = center + left * FrustumHalfWidth - up * FrustumHalfHeight;
        var bottomRight = center - left * FrustumHalfWidth - up * FrustumHalfHeight;

        ShapeSceneNode.AddLine(vertices, position, topLeft, color);
        ShapeSceneNode.AddLine(vertices, position, topRight, color);
        ShapeSceneNode.AddLine(vertices, position, bottomLeft, color);
        ShapeSceneNode.AddLine(vertices, position, bottomRight, color);

        ShapeSceneNode.AddLine(vertices, topLeft, topRight, color);
        ShapeSceneNode.AddLine(vertices, topRight, bottomRight, color);
        ShapeSceneNode.AddLine(vertices, bottomRight, bottomLeft, color);
        ShapeSceneNode.AddLine(vertices, bottomLeft, topLeft, color);

        // A notch on the top edge, so a rolled keyframe reads as rolled
        var topCenter = center + up * FrustumHalfHeight;
        ShapeSceneNode.AddLine(vertices, topCenter, topCenter + up * (FrustumHalfHeight * 0.5f), color);
    }
}
