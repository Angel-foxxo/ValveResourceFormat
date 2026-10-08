using System.Globalization;
using System.IO;
using System.Text.Json;

namespace ValveResourceFormat.Renderer;

/// <summary>
/// A camera pose a <see cref="CameraPath"/> passes through.
/// </summary>
public class CameraKeyframe
{
    /// <summary>Seconds to the next keyframe given to keyframes that do not say otherwise.</summary>
    public const float DefaultDuration = 2f;

    /// <summary>World-space camera position.</summary>
    public Vector3 Position { get; set; }

    /// <summary>Camera orientation as a QAngle: (pitch, yaw, roll) in degrees, pitch positive downwards.</summary>
    public Vector3 Angles { get; set; }

    /// <summary>
    /// Seconds it takes to travel from this keyframe to the next one, at least one tick. Unused on the last keyframe.
    /// </summary>
    public float Duration
    {
        get;
        set => field = float.IsFinite(value) ? MathF.Max(value, CameraPath.TickInterval) : DefaultDuration;
    } = DefaultDuration;
}

/// <summary>
/// A camera pose sampled from a <see cref="CameraPath"/>.
/// </summary>
/// <param name="Position">World-space camera position.</param>
/// <param name="Angles">QAngle in degrees, with yaw and roll wrapped to [-180, 180).</param>
public readonly record struct CameraPathSample(Vector3 Position, Vector3 Angles);

/// <summary>
/// A smooth camera animation through a list of keyframes.
/// </summary>
/// <remarks>
/// Position and angles each follow a natural cubic spline whose knots are the keyframe times. Such a spline
/// has continuous velocity and acceleration through every keyframe, so neither the camera's movement nor
/// its turning changes abruptly as it passes one. Angles are interpolated as euler angles rather than
/// quaternions so that a path whose keyframes are all level stays level in between.
/// </remarks>
public class CameraPath
{
    /// <summary>Length of one tick in seconds, the rate the path is exported at.</summary>
    public const float TickInterval = 1f / 64f;

    /// <summary>The keyframes in playback order.</summary>
    public List<CameraKeyframe> Keyframes { get; } = [];

    /// <summary>
    /// Units per second the camera travels at, or 0 to follow the keyframe durations as they are.
    /// When set, each segment is covered at an even pace along the curve, and <see cref="ApplyConstantSpeed"/>
    /// derives the keyframe durations from it.
    /// </summary>
    public float ConstantSpeed
    {
        get;
        set => field = float.IsFinite(value) ? MathF.Max(value, 0f) : 0f;
    }

    /// <summary>Total length of the animation in seconds.</summary>
    public float Duration
    {
        get
        {
            var duration = 0f;

            for (var i = 0; i < Keyframes.Count - 1; i++)
            {
                duration += Keyframes[i].Duration;
            }

            return duration;
        }
    }

    /// <summary>Number of samples <see cref="SampleTicks"/> produces, both ends included.</summary>
    public int TickCount => Keyframes.Count == 0 ? 0 : (int)MathF.Floor(Duration / TickInterval + 1e-3f) + 1;

    /// <summary>
    /// Gets the time in seconds at which the camera passes through a keyframe.
    /// </summary>
    /// <param name="index">Index into <see cref="Keyframes"/>.</param>
    public float GetKeyframeTime(int index)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, Keyframes.Count);

        var time = 0f;

        for (var i = 0; i < index; i++)
        {
            time += Keyframes[i].Duration;
        }

        return time;
    }

    /// <summary>
    /// Samples the camera pose at a point in time, clamped to the length of the path.
    /// </summary>
    /// <remarks>Builds the spline for every call; use <see cref="Sample"/> for many samples.</remarks>
    /// <param name="time">Seconds since the first keyframe.</param>
    /// <exception cref="InvalidOperationException">The path has no keyframes.</exception>
    public CameraPathSample Evaluate(float time)
    {
        if (Keyframes.Count == 0)
        {
            throw new InvalidOperationException("Camera path has no keyframes.");
        }

        return BuildSpline().Evaluate(time, ConstantSpeed > 0f);
    }

    /// <summary>
    /// Samples the path at a fixed interval, from the first keyframe to the last, both ends included.
    /// </summary>
    /// <param name="interval">Seconds between samples.</param>
    public IEnumerable<CameraPathSample> Sample(float interval)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(interval);

        if (Keyframes.Count == 0)
        {
            yield break;
        }

        var spline = BuildSpline();
        var evenPace = ConstantSpeed > 0f;
        var duration = Duration;
        var count = (int)MathF.Floor(duration / interval + 1e-3f) + 1;

        for (var i = 0; i < count; i++)
        {
            yield return spline.Evaluate(i * interval, evenPace);
        }

        // The last sample lands short of the end unless the interval divides the duration
        if ((count - 1) * interval < duration - 1e-4f)
        {
            yield return spline.Evaluate(duration, evenPace);
        }
    }

    /// <summary>
    /// Sets every keyframe's duration so the whole path runs at <see cref="ConstantSpeed"/>. Does nothing
    /// while it is 0. Call again after moving, adding or removing keyframes.
    /// </summary>
    public void ApplyConstantSpeed()
    {
        if (ConstantSpeed <= 0f || Keyframes.Count < 2)
        {
            return;
        }

        // Start from straight line distances, then refine: the curve's shape depends on the durations
        // through its knots, so its length does too
        for (var i = 0; i < Keyframes.Count - 1; i++)
        {
            Keyframes[i].Duration = Vector3.Distance(Keyframes[i].Position, Keyframes[i + 1].Position) / ConstantSpeed;
        }

        for (var round = 0; round < 8; round++)
        {
            var spline = BuildSpline();

            for (var i = 0; i < Keyframes.Count - 1; i++)
            {
                Keyframes[i].Duration = spline.SegmentLength(i) / ConstantSpeed;
            }
        }
    }

    /// <summary>
    /// Samples the path once per tick of <see cref="TickInterval"/>, from the first keyframe to the last.
    /// </summary>
    public IEnumerable<CameraPathSample> SampleTicks()
    {
        if (Keyframes.Count == 0)
        {
            yield break;
        }

        var spline = BuildSpline();
        var evenPace = ConstantSpeed > 0f;
        var tickCount = TickCount;

        for (var tick = 0; tick < tickCount; tick++)
        {
            yield return spline.Evaluate(tick * TickInterval, evenPace);
        }
    }

    /// <summary>
    /// Writes the pose at every tick as CSV, with a <c>tick,x,y,z,pitch,yaw,roll</c> header.
    /// </summary>
    /// <param name="writer">Destination for the text.</param>
    public void WriteTicksCsv(TextWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);

        writer.Write("tick,x,y,z,pitch,yaw,roll\n");

        var tick = 0;

        foreach (var sample in SampleTicks())
        {
            var p = sample.Position;
            var a = sample.Angles;

            writer.Write(string.Create(CultureInfo.InvariantCulture, $"{tick},{p.X:F4},{p.Y:F4},{p.Z:F4},{a.X:F4},{a.Y:F4},{a.Z:F4}\n"));
            tick++;
        }
    }

    /// <summary>
    /// Serializes the keyframes to JSON, readable by <see cref="FromJson"/>.
    /// </summary>
    public string ToJson()
    {
        using var stream = new MemoryStream();

        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
        {
            writer.WriteStartObject();

            if (ConstantSpeed > 0f)
            {
                writer.WriteNumber("constantSpeed", ConstantSpeed);
            }

            writer.WriteStartArray("keyframes");

            foreach (var keyframe in Keyframes)
            {
                writer.WriteStartObject();
                WriteVector(writer, "position", keyframe.Position);
                WriteVector(writer, "angles", keyframe.Angles);
                writer.WriteNumber("duration", keyframe.Duration);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        return System.Text.Encoding.UTF8.GetString(stream.ToArray());
    }

    /// <summary>
    /// Reads keyframes written by <see cref="ToJson"/>.
    /// </summary>
    /// <param name="json">The JSON text.</param>
    /// <exception cref="JsonException">The text is not a camera path.</exception>
    public static CameraPath FromJson(string json)
    {
        using var document = JsonDocument.Parse(json);

        if (!document.RootElement.TryGetProperty("keyframes", out var keyframes) || keyframes.ValueKind != JsonValueKind.Array)
        {
            throw new JsonException("Camera path is missing its keyframes array.");
        }

        var path = new CameraPath();

        if (document.RootElement.TryGetProperty("constantSpeed", out var speed))
        {
            path.ConstantSpeed = speed.GetSingle();
        }

        foreach (var element in keyframes.EnumerateArray())
        {
            var keyframe = new CameraKeyframe
            {
                Position = ReadVector(element, "position"),
                Angles = ReadVector(element, "angles"),
            };

            if (element.TryGetProperty("duration", out var duration))
            {
                keyframe.Duration = duration.GetSingle();
            }

            path.Keyframes.Add(keyframe);
        }

        return path;
    }

    private Spline BuildSpline()
    {
        var count = Keyframes.Count;
        var times = new float[count];
        var positions = new Vector3[count];
        var angles = new Vector3[count];

        for (var i = 0; i < count; i++)
        {
            var keyframe = Keyframes[i];
            times[i] = i == 0 ? 0f : times[i - 1] + Keyframes[i - 1].Duration;
            positions[i] = keyframe.Position;

            // Each angle is brought within half a turn of the one before, so a yaw crossing 180 does not
            // spin the long way round
            angles[i] = i == 0 ? keyframe.Angles : UnwrapAngles(keyframe.Angles, angles[i - 1]);
        }

        return new Spline(times, positions, SecondDerivatives(times, positions), angles, SecondDerivatives(times, angles));
    }

    /// <summary>
    /// Second derivatives at the knots of the natural cubic spline through <paramref name="values"/>: the
    /// ones that make acceleration continuous at every interior knot, with none at either end.
    /// </summary>
    private static Vector3[] SecondDerivatives(float[] times, Vector3[] values)
    {
        var count = values.Length;
        var result = new Vector3[count];

        if (count < 3)
        {
            return result;
        }

        // Tridiagonal system over the interior knots, solved by forward elimination and back substitution
        var upper = new float[count];
        var rhs = new Vector3[count];

        for (var i = 1; i < count - 1; i++)
        {
            var before = times[i] - times[i - 1];
            var after = times[i + 1] - times[i];
            var slopeChange = 6f * ((values[i + 1] - values[i]) / after - (values[i] - values[i - 1]) / before);
            var diagonal = 2f * (before + after) - before * upper[i - 1];

            upper[i] = after / diagonal;
            rhs[i] = (slopeChange - before * rhs[i - 1]) / diagonal;
        }

        for (var i = count - 2; i >= 1; i--)
        {
            result[i] = rhs[i] - upper[i] * result[i + 1];
        }

        return result;
    }

    private static Vector3 UnwrapAngles(Vector3 angles, Vector3 reference)
        => reference + WrapAngles(angles - reference);

    private static Vector3 WrapAngles(Vector3 angles)
        => new(angles.X, MathUtils.Wrap(angles.Y, -180f, 180f), MathUtils.Wrap(angles.Z, -180f, 180f));

    private static void WriteVector(Utf8JsonWriter writer, string name, Vector3 value)
    {
        writer.WriteStartArray(name);
        writer.WriteNumberValue(value.X);
        writer.WriteNumberValue(value.Y);
        writer.WriteNumberValue(value.Z);
        writer.WriteEndArray();
    }

    private static Vector3 ReadVector(JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out var array) || array.ValueKind != JsonValueKind.Array || array.GetArrayLength() != 3)
        {
            throw new JsonException($"Camera keyframe is missing a three component \"{name}\".");
        }

        return new Vector3(array[0].GetSingle(), array[1].GetSingle(), array[2].GetSingle());
    }

    /// <summary>The path's splines, solved once so many samples can be taken from them.</summary>
    private sealed class Spline(float[] times, Vector3[] positions, Vector3[] positionCurvature, Vector3[] angles, Vector3[] angleCurvature)
    {
        private const int ArcSamples = 48;

        public CameraPathSample Evaluate(float time, bool evenPace)
        {
            if (times.Length == 1)
            {
                return new(positions[0], WrapAngles(angles[0]));
            }

            var segment = Array.BinarySearch(times, time);
            segment = segment < 0 ? ~segment - 1 : segment;
            segment = Math.Clamp(segment, 0, times.Length - 2);

            var length = times[segment + 1] - times[segment];
            var t = Math.Clamp((time - times[segment]) / length, 0f, 1f);

            if (evenPace)
            {
                t = EvenPaceParameter(segment, t);
            }

            var position = At(positions, positionCurvature, segment, t);
            var rotation = At(angles, angleCurvature, segment, t);

            return new(position, WrapAngles(rotation));
        }

        public float SegmentLength(int segment)
        {
            var length = 0f;
            var previous = positions[segment];

            for (var k = 1; k <= ArcSamples; k++)
            {
                var p = At(positions, positionCurvature, segment, (float)k / ArcSamples);
                length += Vector3.Distance(previous, p);
                previous = p;
            }

            return length;
        }

        /// <summary>
        /// The spline parameter at which a segment has covered fraction <paramref name="t"/> of its length, so
        /// that stepping <paramref name="t"/> evenly moves the camera at an even pace.
        /// </summary>
        private float EvenPaceParameter(int segment, float t)
        {
            Span<float> distances = stackalloc float[ArcSamples + 1];
            var previous = positions[segment];

            for (var k = 1; k <= ArcSamples; k++)
            {
                var p = At(positions, positionCurvature, segment, (float)k / ArcSamples);
                distances[k] = distances[k - 1] + Vector3.Distance(previous, p);
                previous = p;
            }

            var target = t * distances[ArcSamples];

            for (var k = 1; k <= ArcSamples; k++)
            {
                if (distances[k] >= target)
                {
                    var span = distances[k] - distances[k - 1];
                    var f = span > 0f ? (target - distances[k - 1]) / span : 0f;
                    return (k - 1 + f) / ArcSamples;
                }
            }

            return 1f;
        }

        private Vector3 At(Vector3[] values, Vector3[] curvature, int segment, float t)
        {
            var length = times[segment + 1] - times[segment];
            var s = 1f - t;

            return s * values[segment] + t * values[segment + 1]
                + (length * length / 6f) * ((s * s * s - s) * curvature[segment] + (t * t * t - t) * curvature[segment + 1]);
        }
    }
}
