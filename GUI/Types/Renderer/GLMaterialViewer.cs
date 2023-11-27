using System;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using GUI.Types.Viewers;
using GUI.Utils;
using ValveResourceFormat.IO;
using ValveResourceFormat.ResourceTypes;

namespace GUI.Types.Renderer
{
    /// <summary>
    /// GL Render control with material controls (render modes maybe at some point?).
    /// Renders a list of MatarialRenderers.
    /// </summary>
    class GLMaterialViewer : GLSingleNodeViewer
    {
        private readonly ValveResourceFormat.Resource Resource;
        private readonly TabControl Tabs;
        private TableLayoutPanel ParamsTable;
        public ValveCompositeMaterial SpirvRenderer { get; private set; }

        public GLMaterialViewer(VrfGuiContext guiContext, ValveResourceFormat.Resource resource, TabControl tabs) : base(guiContext)
        {
            Resource = resource;
            Tabs = tabs;

            Camera.ModifySpeed(0);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ParamsTable?.Dispose();
            }

            base.Dispose(disposing);
        }

        protected override void LoadScene()
        {
            base.LoadScene();

            var assembly = Assembly.GetExecutingAssembly();
            using var stream = assembly.GetManifestResourceStream($"GUI.Utils.env_cubemap.vmdl_c");

            using var cubemapResource = new ValveResourceFormat.Resource()
            {
                FileName = "env_cubemap.vmdl_c"
            };
            cubemapResource.Read(stream);

            var node = new ModelSceneNode(Scene, (Model)cubemapResource.DataBlock);

            foreach (var renderable in node.RenderableMeshes)
            {
                renderable.SetMaterialForMaterialViewer(Resource);
            }

            Scene.Add(node, false);

            var material = node.RenderableMeshes[0].DrawCallsOpaque[0];
            if (material.Material.Material.ShaderName == "csgo_customweapon.vfx")
            {
                SpirvRenderer = new ValveCompositeMaterial(material.Material.Material, GuiContext);
                if (!SpirvRenderer.IsValid())
                {
                    SpirvRenderer = null;
                }
            }

#if DEBUG
            // Assume cubemap model only has one opaque draw call
            var drawCall = node.RenderableMeshes[0].DrawCallsOpaque[0];
            var usedParams = drawCall.Material.Shader.GetAllUniformNames().Select(x => x.Name).ToHashSet();

            foreach (var (paramName, currentValue) in drawCall.Material.Material.FloatParams.OrderBy(x => x.Key))
            {
                if (!usedParams.Contains(paramName))
                {
                    continue;
                }

                var row = ParamsTable.RowCount;
                ParamsTable.RowCount = row + 2;
                ParamsTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
                ParamsTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));

                ParamsTable.Controls.Add(new Label()
                {
                    Dock = DockStyle.Fill,
                    AutoSize = false,
                    Text = paramName
                }, 0, row);

                var currentParamName = paramName;
                var input = new NumericUpDown
                {
                    Width = ParamsTable.Width / 2,
                    Minimum = decimal.MinValue,
                    Maximum = decimal.MaxValue,
                    DecimalPlaces = 6,
                    Increment = 0.1M,
                    Value = (decimal)currentValue
                };
                input.ValueChanged += (sender, e) =>
                {
                    drawCall.Material.Material.FloatParams[currentParamName] = (float)input.Value;
                };
                input.MouseWheel += (sender, e) =>
                {
                    // Fix bug where one scroll causes increments more than once, https://stackoverflow.com/a/16338022
                    (e as HandledMouseEventArgs).Handled = true;

                    if (e.Delta > 0)
                    {
                        input.Value += input.Increment;
                    }
                    else if (e.Delta < 0)
                    {
                        input.Value -= input.Increment;
                    }
                };
                ParamsTable.Controls.Add(input, 0, row + 1);
            }
#endif
        }

        private void OnShadersButtonClick(object s, EventArgs e)
        {
            var material = (Material)Resource.DataBlock;

            var shaders = GuiContext.FileLoader.LoadShader(material.ShaderName);

            var featureState = ShaderDataProvider.GetMaterialFeatureState(material);

            AddZframeTab(shaders.Vertex);
            AddZframeTab(shaders.Pixel);

            void AddZframeTab(ValveResourceFormat.CompiledShader.ShaderFile stage)
            {
                var result = ShaderDataProvider.GetStaticConfiguration_ForFeatureState(shaders.Features, stage, featureState);

                var zframeTab = new TabPage($"{stage.VcsProgramType} Static[{result.ZFrameId}]");
                var zframeRichTextBox = new CompiledShader.ZFrameRichTextBox(Tabs, stage, shaders, result.ZFrameId);
                zframeTab.Controls.Add(zframeRichTextBox);

                using var zFrame = stage.GetZFrameFile(result.ZFrameId);
                var gpuSourceTab = CompiledShader.CreateDecompiledTabPage(shaders, stage, zFrame, 0, $"{stage.VcsProgramType} Source[0]");

                Tabs.Controls.Add(zframeTab);
                Tabs.TabPages.Add(gpuSourceTab);
                Tabs.SelectTab(gpuSourceTab);
            }
        }

        private void AddShaderButton()
        {
            var button = new Button
            {
                Text = "Open shader zframe",
                AutoSize = true,
            };

            button.Click += OnShadersButtonClick;

            AddControl(button);
        }

        protected override void InitializeControl()
        {
            AddRenderModeSelectionControl();
            AddShaderButton();

            ParamsTable = new TableLayoutPanel
            {
                AutoScroll = true,
                Width = 220,
                Height = 300,
            };
            AddControl(ParamsTable);

            ParamsTable.ColumnCount = 1;
            ParamsTable.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize, 1));
        }
    }
}
