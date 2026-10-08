using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Windows.Forms;
using GUI.Controls;
using GUI.Utils;
using ValveResourceFormat.Renderer;
using ValveResourceFormat.Renderer.Input;

namespace GUI.Types.GLViewers;

/// <summary>
/// Records camera keyframes from the view, plays the smoothed path back, and saves, loads and exports it.
/// </summary>
/// <remarks>
/// Editing happens on the UI thread while playback and drawing happen on the render thread, so both sides
/// go through <see cref="sync"/>.
/// </remarks>
sealed class CameraPathEditor(RendererContext rendererContext, UserInput input, string? mapName) : IDisposable
{
    private const float JumpTransitionDuration = 0.3f;

    private readonly Lock sync = new();
    private CameraPath path = new();
    private int selectedKeyframe = -1;
    private bool playing;
    private float playbackTime;
    private bool loopPlayback;
    private bool showPath = true;
    private CameraPathRenderer? pathRenderer;

    private readonly TransformGizmo gizmo = new();
    private GizmoView? lastView;
    private bool dragging;

    /// <summary>How close in pixels a click has to be to a keyframe's marker to select it.</summary>
    private const float MarkerPickPixels = 14f;

    /// <summary>The whole path as it was before an edit.</summary>
    private sealed record Snapshot(CameraKeyframe[] Keyframes, float ConstantSpeed, int SelectedKeyframe);

    private const int MaxUndoSteps = 200;

    /// <summary>Edits of the same kind this close together count as one step, like dragging a number.</summary>
    private static readonly TimeSpan UndoMergeWindow = TimeSpan.FromSeconds(1);

    private readonly List<Snapshot> undoStack = [];
    private readonly List<Snapshot> redoStack = [];
    private string? lastUndoAction;
    private long lastUndoTimestamp;

    private ComboBox? keyframeComboBox;
    private ThemedFloatNumeric? durationInput;
    private ThemedFloatNumeric? speedInput;
    private Label? infoLabel;
    private bool updatingUi;

    public void AddControls(RendererControl ui)
    {
        using (ui.BeginGroup("Camera Path"))
        {
            AddGroupControls(ui);
        }

        RefreshUi();
    }

    private void AddGroupControls(RendererControl ui)
    {
        infoLabel = new Label { AutoSize = false, Height = ui.AdjustForDPI(20) };
        ui.AddControl(infoLabel);

        keyframeComboBox = ui.AddSelection("Keyframe", (_, index) =>
        {
            if (!updatingUi)
            {
                SelectKeyframe(index);
            }
        });

        var durationPanel = RendererControl.CreateFloatInput("Seconds to next", value =>
        {
            if (!updatingUi)
            {
                SetSelectedDuration(value);
            }
        }, CameraKeyframe.DefaultDuration, CameraPath.TickInterval, 600f);
        durationInput = durationPanel.Controls.OfType<ThemedFloatNumeric>().First();
        ui.AddControl(durationPanel);

        // 0 keeps the per keyframe durations; anything else times the whole path at that speed
        var speedPanel = RendererControl.CreateFloatInput("Constant speed", value =>
        {
            if (!updatingUi)
            {
                SetConstantSpeed(value);
            }
        }, 0f, 0f, 2000f);
        speedInput = speedPanel.Controls.OfType<ThemedFloatNumeric>().First();
        ui.AddControl(speedPanel);

        ui.AddControl(CreateButtonRow(ui,
            ("Add (K)", AddKeyframe),
            ("Update", UpdateSelectedKeyframe),
            ("Delete", () => DeleteSelectedKeyframe())));

        ui.AddControl(CreateButtonRow(ui,
            ("Play (P)", TogglePlayback),
            ("View", ViewSelectedKeyframe),
            ("Clear", Clear)));

        ui.AddControl(CreateButtonRow(ui,
            ("Save", Save),
            ("Load", Load),
            ("Export ticks", ExportTicks)));

        ui.AddCheckBox("Show Path", showPath, v =>
        {
            using var _ = sync.EnterScope();
            showPath = v;
        });

        ui.AddCheckBox("Loop Playback", loopPlayback, v =>
        {
            using var _ = sync.EnterScope();
            loopPlayback = v;
        });
    }

    /// <summary>Advances playback and, while playing, points the render camera along the path.</summary>
    public void Update(float frameTime, Camera renderCamera)
    {
        using var _ = sync.EnterScope();

        if (!playing)
        {
            return;
        }

        var duration = path.Duration;

        if (path.Keyframes.Count < 2)
        {
            StopPlayback();
            return;
        }

        playbackTime += frameTime;

        if (playbackTime > duration)
        {
            if (!loopPlayback)
            {
                StopPlayback();
                return;
            }

            playbackTime %= duration;
        }

        var sample = path.Evaluate(playbackTime);
        renderCamera.SetLocation(sample.Position);
        renderCamera.SetFromQAngle(sample.Angles);
    }

    /// <summary>Draws the path and keyframes, except during playback where they would be in the shot.</summary>
    /// <param name="camera">Camera the frame is drawn with, which mouse picking also uses.</param>
    public void Render(Camera camera)
    {
        using var _ = sync.EnterScope();

        if (!showPath || playing || path.Keyframes.Count == 0)
        {
            lastView = null;
            return;
        }

        var view = GizmoView.FromCamera(camera);
        lastView = view;

        pathRenderer ??= new CameraPathRenderer(rendererContext);
        pathRenderer.Render(path, selectedKeyframe, selectedKeyframe >= 0 ? gizmo : null, view);
    }

    /// <summary>
    /// Grabs a gizmo handle of the selected keyframe, or selects the keyframe whose marker was clicked.
    /// </summary>
    /// <returns>Whether the click was used, and should not reach the camera or scene picking.</returns>
    public bool OnMouseDown(int x, int y)
    {
        var mouse = new Vector2(x, y);

        using (sync.EnterScope())
        {
            if (lastView is not { } view)
            {
                return false;
            }

            if (selectedKeyframe >= 0)
            {
                var keyframe = path.Keyframes[selectedKeyframe];

                if (gizmo.BeginDrag(mouse, keyframe.Position, keyframe.Angles, view))
                {
                    PushUndo("drag");
                    dragging = true;
                    return true;
                }
            }

            var nearest = -1;
            var nearestDistance = MarkerPickPixels;

            for (var i = 0; i < path.Keyframes.Count; i++)
            {
                if (view.Project(path.Keyframes[i].Position, out var screen) && Vector2.Distance(screen, mouse) < nearestDistance)
                {
                    nearestDistance = Vector2.Distance(screen, mouse);
                    nearest = i;
                }
            }

            if (nearest < 0)
            {
                return false;
            }

            selectedKeyframe = nearest;
        }

        RefreshUi();
        return true;
    }

    /// <summary>Drags the grabbed handle, or highlights the handle under the mouse.</summary>
    public void OnMouseMove(int x, int y)
    {
        var mouse = new Vector2(x, y);

        using var _ = sync.EnterScope();

        if (lastView is not { } view || selectedKeyframe < 0)
        {
            gizmo.Hovered = GizmoHandle.None;
            return;
        }

        var keyframe = path.Keyframes[selectedKeyframe];

        if (!dragging)
        {
            gizmo.Hovered = TransformGizmo.HitTest(mouse, keyframe.Position, keyframe.Angles, view);
            return;
        }

        (keyframe.Position, keyframe.Angles) = gizmo.Drag(mouse, view);
        path.ApplyConstantSpeed();
    }

    /// <summary>Ends a drag.</summary>
    public void OnMouseUp()
    {
        using (sync.EnterScope())
        {
            if (!dragging)
            {
                return;
            }

            dragging = false;
            gizmo.EndDrag();
        }

        RefreshUi();
    }

    /// <summary>Steps back to the path as it was before the last edit.</summary>
    public void Undo() => StepHistory(undoStack, redoStack);

    /// <summary>Steps forward again after <see cref="Undo"/>.</summary>
    public void Redo() => StepHistory(redoStack, undoStack);

    private void StepHistory(List<Snapshot> from, List<Snapshot> to)
    {
        using (sync.EnterScope())
        {
            if (dragging || from.Count == 0)
            {
                return;
            }

            StopPlayback();
            to.Add(TakeSnapshot());

            var snapshot = from[^1];
            from.RemoveAt(from.Count - 1);

            path.Keyframes.Clear();
            path.Keyframes.AddRange(snapshot.Keyframes.Select(CopyKeyframe));
            path.ConstantSpeed = snapshot.ConstantSpeed;
            selectedKeyframe = Math.Min(snapshot.SelectedKeyframe, path.Keyframes.Count - 1);

            // The next edit starts a new step rather than merging into the one undone
            lastUndoAction = null;
        }

        RefreshUi();
    }

    /// <summary>
    /// Records the path before an edit, holding <see cref="sync"/>. With <paramref name="merge"/>, repeats of
    /// the same action in quick succession share the step the first one recorded.
    /// </summary>
    private void PushUndo(string action, bool merge = false)
    {
        var now = Stopwatch.GetTimestamp();
        var repeat = merge && action == lastUndoAction && Stopwatch.GetElapsedTime(lastUndoTimestamp, now) < UndoMergeWindow;

        lastUndoAction = action;
        lastUndoTimestamp = now;

        if (repeat)
        {
            return;
        }

        undoStack.Add(TakeSnapshot());

        if (undoStack.Count > MaxUndoSteps)
        {
            undoStack.RemoveAt(0);
        }

        redoStack.Clear();
    }

    private Snapshot TakeSnapshot() => new([.. path.Keyframes.Select(CopyKeyframe)], path.ConstantSpeed, selectedKeyframe);

    private static CameraKeyframe CopyKeyframe(CameraKeyframe keyframe) => new()
    {
        Position = keyframe.Position,
        Angles = keyframe.Angles,
        Duration = keyframe.Duration,
    };

    public void Dispose()
    {
        pathRenderer?.Delete();
        pathRenderer = null;

        infoLabel?.Dispose();
        keyframeComboBox?.Dispose();
        durationInput?.Dispose();
        speedInput?.Dispose();
    }

    /// <summary>Adds a keyframe at the current view, after the selected keyframe or at the end.</summary>
    public void AddKeyframe()
    {
        using (sync.EnterScope())
        {
            PushUndo("add");

            var index = selectedKeyframe >= 0 ? selectedKeyframe + 1 : path.Keyframes.Count;
            var keyframe = CreateKeyframeFromView();

            // Keep the pacing of the keyframe this one follows
            if (index > 0)
            {
                keyframe.Duration = path.Keyframes[index - 1].Duration;
            }

            path.Keyframes.Insert(index, keyframe);
            path.ApplyConstantSpeed();
            selectedKeyframe = index;
        }

        RefreshUi();
    }

    public void TogglePlayback()
    {
        using (sync.EnterScope())
        {
            if (playing)
            {
                StopPlayback();
            }
            else if (path.Keyframes.Count >= 2)
            {
                playing = true;
                playbackTime = 0f;
            }
        }
    }

    private void UpdateSelectedKeyframe()
    {
        using (sync.EnterScope())
        {
            if (selectedKeyframe < 0)
            {
                return;
            }

            PushUndo("update");

            var keyframe = path.Keyframes[selectedKeyframe];
            var view = CreateKeyframeFromView();

            keyframe.Position = view.Position;
            keyframe.Angles = view.Angles;
            path.ApplyConstantSpeed();
        }

        RefreshUi();
    }

    /// <summary>Deletes the selected keyframe.</summary>
    /// <returns>Whether there was one to delete.</returns>
    public bool DeleteSelectedKeyframe()
    {
        using (sync.EnterScope())
        {
            if (selectedKeyframe < 0 || dragging)
            {
                return false;
            }

            PushUndo("delete");
            path.Keyframes.RemoveAt(selectedKeyframe);
            path.ApplyConstantSpeed();
            selectedKeyframe = Math.Min(selectedKeyframe, path.Keyframes.Count - 1);
        }

        RefreshUi();
        return true;
    }

    private void Clear()
    {
        using (sync.EnterScope())
        {
            if (path.Keyframes.Count == 0)
            {
                return;
            }

            PushUndo("clear");
            StopPlayback();
            path.Keyframes.Clear();
            selectedKeyframe = -1;
        }

        RefreshUi();
    }

    private void SelectKeyframe(int index)
    {
        using (sync.EnterScope())
        {
            if (index < 0 || index >= path.Keyframes.Count)
            {
                return;
            }

            selectedKeyframe = index;
        }

        RefreshUi();
    }

    /// <summary>Moves the camera to look through the selected keyframe, to adjust it and press Update.</summary>
    private void ViewSelectedKeyframe()
    {
        CameraKeyframe keyframe;

        using (sync.EnterScope())
        {
            if (selectedKeyframe < 0)
            {
                return;
            }

            StopPlayback();
            keyframe = path.Keyframes[selectedKeyframe];
        }

        input.SaveCameraForTransition(JumpTransitionDuration);
        input.Camera.SetLocation(keyframe.Position);
        input.Camera.SetFromQAngle(keyframe.Angles);
    }

    private void SetSelectedDuration(float duration)
    {
        using (sync.EnterScope())
        {
            if (selectedKeyframe < 0)
            {
                return;
            }

            PushUndo($"duration {selectedKeyframe}", merge: true);
            path.Keyframes[selectedKeyframe].Duration = duration;
        }

        RefreshUi();
    }

    private void SetConstantSpeed(float speed)
    {
        using (sync.EnterScope())
        {
            PushUndo("speed", merge: true);
            path.ConstantSpeed = speed;
            path.ApplyConstantSpeed();
        }

        RefreshUi();
    }

    private void Save()
    {
        string json;

        using (sync.EnterScope())
        {
            if (path.Keyframes.Count == 0)
            {
                return;
            }

            json = path.ToJson();
        }

        var fileName = AppFileDialogs.SaveFile("Save camera path", $"{mapName ?? "camera"}_path.json", "json", "Camera path (*.json)|*.json");

        if (fileName != null)
        {
            File.WriteAllText(fileName, json);
        }
    }

    private void Load()
    {
        var fileName = AppFileDialogs.OpenFile("Load camera path", "Camera path (*.json)|*.json");

        if (fileName == null)
        {
            return;
        }

        CameraPath loaded;

        try
        {
            loaded = CameraPath.FromJson(File.ReadAllText(fileName));
        }
        catch (Exception e) when (e is JsonException or IOException or InvalidOperationException or FormatException)
        {
            Log.Error(nameof(CameraPathEditor), $"Failed to load camera path \"{fileName}\": {e.Message}");
            return;
        }

        using (sync.EnterScope())
        {
            PushUndo("load");
            StopPlayback();
            path = loaded;
            selectedKeyframe = -1;
        }

        RefreshUi();
    }

    private void ExportTicks()
    {
        using var writer = new StringWriter();

        using (sync.EnterScope())
        {
            if (path.Keyframes.Count == 0)
            {
                return;
            }

            path.WriteTicksCsv(writer);
        }

        var fileName = AppFileDialogs.SaveFile("Export camera ticks", $"{mapName ?? "camera"}_ticks.csv", "csv", "CSV (*.csv)|*.csv");

        if (fileName != null)
        {
            File.WriteAllText(fileName, writer.ToString());
        }
    }

    private CameraKeyframe CreateKeyframeFromView()
    {
        var camera = input.Camera;

        return new CameraKeyframe
        {
            Position = camera.Location,
            Angles = camera.GetQAngle(),
        };
    }

    private void StopPlayback()
    {
        if (!playing)
        {
            return;
        }

        playing = false;

        // Input only drives the render camera while the mouse is over the viewport, so hand the view back now
        input.ForceUpdate = true;
    }

    private void RefreshUi()
    {
        if (keyframeComboBox == null || durationInput == null || speedInput == null || infoLabel == null)
        {
            return;
        }

        updatingUi = true;

        try
        {
            using var _ = sync.EnterScope();

            keyframeComboBox.BeginUpdate();
            keyframeComboBox.Items.Clear();

            var time = 0f;

            for (var i = 0; i < path.Keyframes.Count; i++)
            {
                keyframeComboBox.Items.Add($"#{i + 1} at {time:0.00}s");
                time += path.Keyframes[i].Duration;
            }

            keyframeComboBox.SelectedIndex = selectedKeyframe;
            keyframeComboBox.EndUpdate();

            var hasSelection = selectedKeyframe >= 0;
            // Under a constant speed the durations follow from the path, so they are not edited by hand
            durationInput.Enabled = hasSelection && path.ConstantSpeed <= 0f;
            speedInput.Value = path.ConstantSpeed;

            if (hasSelection)
            {
                durationInput.Value = path.Keyframes[selectedKeyframe].Duration;
            }

            infoLabel.Text = $"{path.Keyframes.Count} keyframes, {path.Duration:0.00}s, {path.TickCount} ticks";
        }
        finally
        {
            updatingUi = false;
        }
    }

    private static TableLayoutPanel CreateButtonRow(RendererControl ui, params (string Text, Action OnClick)[] buttons)
    {
        var row = new TableLayoutPanel
        {
            ColumnCount = buttons.Length,
            RowCount = 1,
            Height = ui.AdjustForDPI(30),
            Padding = new Padding(0, 2, 0, 2),
            Margin = new Padding(0),
        };

        foreach (var (text, onClick) in buttons)
        {
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / buttons.Length));

            var button = new ThemedButton
            {
                Text = text,
                Dock = DockStyle.Fill,
                Margin = new Padding(1, 0, 1, 0),
            };
            button.Click += (_, _) => onClick();

            row.Controls.Add(button);
        }

        return row;
    }
}
