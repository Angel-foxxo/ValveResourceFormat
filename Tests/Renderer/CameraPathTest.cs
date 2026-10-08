using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ValveResourceFormat.Renderer;

namespace Tests.Renderer
{
    public class CameraPathTest
    {
        private const float Tolerance = 1e-3f;

        private static CameraPath MakePath()
        {
            var path = new CameraPath();
            path.Keyframes.Add(new CameraKeyframe { Position = new(0f, 0f, 0f), Angles = new(0f, 0f, 0f), Duration = 1f });
            path.Keyframes.Add(new CameraKeyframe { Position = new(100f, 0f, 50f), Angles = new(10f, 90f, 0f), Duration = 3f });
            path.Keyframes.Add(new CameraKeyframe { Position = new(100f, 300f, 0f), Angles = new(-20f, 170f, 0f), Duration = 2f });
            path.Keyframes.Add(new CameraKeyframe { Position = new(0f, 300f, 0f), Angles = new(0f, -150f, 0f) });

            return path;
        }

        [Test]
        public async Task PassesThroughEveryKeyframe()
        {
            var path = MakePath();

            for (var i = 0; i < path.Keyframes.Count; i++)
            {
                var sample = path.Evaluate(path.GetKeyframeTime(i));
                var keyframe = path.Keyframes[i];

                await Assert.That(Vector3.Distance(sample.Position, keyframe.Position)).IsLessThan(Tolerance).Because($"position of keyframe {i}");
                await Assert.That(sample.Angles.X).IsEqualTo(keyframe.Angles.X).Within(Tolerance).Because($"pitch of keyframe {i}");
                await Assert.That(sample.Angles.Y).IsEqualTo(keyframe.Angles.Y).Within(Tolerance).Because($"yaw of keyframe {i}");
            }
        }

        /// <summary>
        /// Segments of different lengths meet without the camera changing speed, which is what makes a path
        /// read as one smooth move rather than a string of separate ones.
        /// </summary>
        [Test]
        public async Task VelocityIsContinuousThroughKeyframes()
        {
            var path = MakePath();
            // One sided differences disagree by about acceleration times the step even for a smooth path,
            // well under the tens of units per second a mismatched tangent would show
            const float Step = 1e-2f;

            for (var i = 1; i < path.Keyframes.Count - 1; i++)
            {
                var time = path.GetKeyframeTime(i);
                var center = path.Evaluate(time);

                var incoming = (center.Position - path.Evaluate(time - Step).Position) / Step;
                var outgoing = (path.Evaluate(time + Step).Position - center.Position) / Step;

                await Assert.That(Vector3.Distance(incoming, outgoing)).IsLessThan(5f).Because($"velocity through keyframe {i}");
            }
        }

        /// <summary>
        /// Acceleration carries through keyframes too, so how fast the camera is turning or curving does
        /// not change abruptly as it passes one. Keyframes land on ticks here, and acceleration is measured
        /// either side of each from the samples.
        /// </summary>
        [Test]
        public async Task AccelerationIsContinuousThroughKeyframes()
        {
            var path = new CameraPath();
            path.Keyframes.Add(new CameraKeyframe { Position = new(0f, 0f, 0f), Angles = new(0f, 0f, 0f), Duration = 1f });
            path.Keyframes.Add(new CameraKeyframe { Position = new(100f, 0f, 20f), Angles = new(10f, 60f, 0f), Duration = 1.5f });
            path.Keyframes.Add(new CameraKeyframe { Position = new(160f, 120f, 0f), Angles = new(-5f, 150f, 0f), Duration = 1f });
            path.Keyframes.Add(new CameraKeyframe { Position = new(60f, 200f, 0f), Angles = new(0f, -140f, 0f) });

            var ticks = path.SampleTicks().ToList();
            const float H = CameraPath.TickInterval;

            foreach (var time in new[] { 1f, 2.5f })
            {
                var k = (int)MathF.Round(time / H);
                var before = (ticks[k].Position - 2f * ticks[k - 1].Position + ticks[k - 2].Position) / (H * H);
                var after = (ticks[k + 2].Position - 2f * ticks[k + 1].Position + ticks[k].Position) / (H * H);

                // The two estimates sit a tick either side of the keyframe, so even a smooth curve
                // differs by about two ticks' worth of jerk; a corner in acceleration differs by far more
                await Assert.That(Vector3.Distance(before, after)).IsLessThan(20f).Because($"position acceleration at {time}s");

                var yawBefore = (ticks[k].Angles.Y - 2f * ticks[k - 1].Angles.Y + ticks[k - 2].Angles.Y) / (H * H);
                var yawAfter = (ticks[k + 2].Angles.Y - 2f * ticks[k + 1].Angles.Y + ticks[k].Angles.Y) / (H * H);
                await Assert.That(MathF.Abs(yawBefore - yawAfter)).IsLessThan(20f).Because($"yaw acceleration at {time}s");
            }
        }

        [Test]
        public async Task YawTakesTheShortWayAround()
        {
            var path = new CameraPath();
            path.Keyframes.Add(new CameraKeyframe { Angles = new(0f, 170f, 0f), Duration = 1f });
            path.Keyframes.Add(new CameraKeyframe { Angles = new(0f, -170f, 0f) });

            var middle = path.Evaluate(0.5f);

            await Assert.That(MathF.Abs(middle.Angles.Y)).IsEqualTo(180f).Within(Tolerance);
        }

        [Test]
        public async Task ExportsOneRowPerTickIncludingBothEnds()
        {
            var path = new CameraPath();
            path.Keyframes.Add(new CameraKeyframe { Position = Vector3.Zero, Duration = 2f });
            path.Keyframes.Add(new CameraKeyframe { Position = new(64f, 0f, 0f) });

            using var writer = new StringWriter();
            path.WriteTicksCsv(writer);

            var lines = writer.ToString().TrimEnd('\n').Split('\n');

            await Assert.That(path.TickCount).IsEqualTo(129);
            await Assert.That(lines.Length).IsEqualTo(130);
            await Assert.That(lines[0]).IsEqualTo("tick,x,y,z,pitch,yaw,roll");
            await Assert.That(lines[1]).IsEqualTo("0,0.0000,0.0000,0.0000,0.0000,0.0000,0.0000");
            await Assert.That(lines[^1]).IsEqualTo("128,64.0000,0.0000,0.0000,0.0000,0.0000,0.0000");
        }

        /// <summary>
        /// With a constant speed set, every tick covers the same distance, through corners and across
        /// keyframes that are unevenly spaced.
        /// </summary>
        [Test]
        public async Task ConstantSpeedHoldsThroughCornersAndUnevenSpacing()
        {
            var path = MakePath();
            path.Keyframes.Insert(1, new CameraKeyframe { Position = new(20f, 5f, 0f) });
            path.ConstantSpeed = 200f;
            path.ApplyConstantSpeed();

            var ticks = path.SampleTicks().ToList();

            // The last tick can land short of the end
            for (var t = 1; t < ticks.Count - 1; t++)
            {
                var speed = Vector3.Distance(ticks[t - 1].Position, ticks[t].Position) / CameraPath.TickInterval;
                await Assert.That(speed).IsEqualTo(200f).Within(2f).Because($"speed at tick {t}");
            }
        }

        [Test]
        public async Task JsonRoundTrips()
        {
            var path = MakePath();
            path.ConstantSpeed = 150f;
            var loaded = CameraPath.FromJson(path.ToJson());

            await Assert.That(loaded.ConstantSpeed).IsEqualTo(150f);
            await Assert.That(loaded.Keyframes.Count).IsEqualTo(path.Keyframes.Count);

            for (var i = 0; i < path.Keyframes.Count; i++)
            {
                await Assert.That(loaded.Keyframes[i].Position).IsEqualTo(path.Keyframes[i].Position);
                await Assert.That(loaded.Keyframes[i].Angles).IsEqualTo(path.Keyframes[i].Angles);
                await Assert.That(loaded.Keyframes[i].Duration).IsEqualTo(path.Keyframes[i].Duration);
            }
        }
    }
}
