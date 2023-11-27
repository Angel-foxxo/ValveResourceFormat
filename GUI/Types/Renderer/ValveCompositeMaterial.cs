using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using GUI.Utils;
using OpenTK.Graphics.OpenGL;
using ValveResourceFormat.CompiledShader;
using ValveResourceFormat.IO;
using ValveResourceFormat.ResourceTypes;
using Vortice.SPIRV;
using Vortice.SpirvCross;

namespace GUI.Types.Renderer
{
    class ValveCompositeMaterial
    {
        public ShaderFile Features => ShaderFiles.Features;
        public ShaderFile VertexShaders => ShaderFiles.Vertex;
        public ShaderFile PixelShaders => ShaderFiles.Pixel;

        public (int[] StaticConfig, long ZFrameId) VertexShaderState { get; private set; }
        public (int[] StaticConfig, long ZFrameId) PixelShaderState { get; private set; }

        public void SetShaderVariants()
        {
            var featureState = ShaderDataProvider.GetMaterialFeatureState(Material);
            VertexShaderState = ShaderDataProvider.GetStaticConfiguration_ForFeatureState(Features, VertexShaders, featureState);
            PixelShaderState = ShaderDataProvider.GetStaticConfiguration_ForFeatureState(Features, PixelShaders, featureState);
        }

        public ZFrameFile Vertex => VertexShaders.ZFrameCache.Get(VertexShaderState.ZFrameId);
        public ZFrameFile Pixel => PixelShaders.ZFrameCache.Get(PixelShaderState.ZFrameId);

        public Material Material { get; }
        public Dictionary<string, RenderTexture> Textures { get; } = [];

        public bool IsValid() => ShaderProgram != -1 && VaoHandle != -1 && VboHandle != -1;

        public ValveCompositeMaterial(Material material, VrfGuiContext guiContext)
        {
            Material = material;
            GuiContext = guiContext;

            ShaderFiles = GuiContext.FileLoader.LoadShader(material.ShaderName);

            Debug.Assert(ShaderFiles != null);
            Debug.Assert(ShaderFiles.Features.VcsPlatformType == VcsPlatformType.VULKAN);

            // Load textures
            Material.TextureParams.ToList().ForEach(textureParam =>
            {
                Textures.Add(textureParam.Key, GuiContext.MaterialLoader.GetTexture(textureParam.Value));
            });

            SetShaderVariants();

            // TODO: implement for multiple dynamic configs.
            Debug.Assert(Vertex.GpuSources.Count == 1);
            Debug.Assert(Pixel.GpuSources.Count == 1);

            // Create shader program
            var vertex = Compile(ShaderType.VertexShader, Vertex.GpuSources.First() as VulkanSource);
            var pixel = Compile(ShaderType.FragmentShader, Pixel.GpuSources.First() as VulkanSource);

            if (pixel == -1 || vertex == -1)
            {
                return;
            }

            ShaderProgram = GL.CreateProgram();
            GL.AttachShader(ShaderProgram, vertex);
            GL.AttachShader(ShaderProgram, pixel);
            GL.LinkProgram(ShaderProgram);

            GL.GetProgram(ShaderProgram, GetProgramParameterName.LinkStatus, out var status);
            if (status != 1)
            {
                var infoLog = GL.GetProgramInfoLog(ShaderProgram);
                GL.DeleteProgram(ShaderProgram);

                Log.Error("GL", $"Failed to link shader program: {infoLog}");
                ShaderProgram = -1;
                return;
            }

            Log.Info("GL", $"Shader program linked successfully");
            GL.DetachShader(ShaderProgram, vertex);
            GL.DetachShader(ShaderProgram, pixel);
            //GL.DeleteShader(vertex);
            //GL.DeleteShader(pixel);

            CreateScreenVertexArrayObject();
            //SetViewPort(1024, 1024);
        }

        private int ShaderProgram;
        private int VaoHandle = -1;
        private int VboHandle = -1;

        private VrfGuiContext GuiContext;
        private ShaderCollection ShaderFiles;

        public void Render()
        {
            if (ShaderProgram == -1)
            {
                return;
            }

            GL.Enable(EnableCap.DebugOutput);
            GL.Enable(EnableCap.CullFace);
            GL.Disable(EnableCap.DepthTest);

            GL.UseProgram(ShaderProgram);
            GL.BindVertexArray(VaoHandle);

            WriteMaterialBuffer(ShaderProgram, "VertexShader", VertexShaders.ParamBlocks, Vertex.LeadingData);
            WriteMaterialBuffer(ShaderProgram, "FragmentShader", PixelShaders.ParamBlocks, Pixel.LeadingData);

            var vfxSamplerNames = Pixel.LeadingData.Segment1
                .Select(f => PixelShaders.ParamBlocks[f.ParamId])
                .Where(p => p.VfxType == Vfx.Type.Sampler2D)
                .Select(p => p.Name)
                .ToList();

#if DEBUG
            var glSamplerNames = new Shader { Program = ShaderProgram, Name = string.Empty }
                .GetAllUniformNames()
                .Where(x => x.Type == ActiveUniformType.Sampler2D)
                .Select(x => x.Name)
                .ToList();

            Debug.Assert(glSamplerNames.Count == vfxSamplerNames.Count);
#endif

            var textureUnit = RenderMaterial.TextureUnitStart;
            foreach (var vfxSamplerName in vfxSamplerNames)
            {
                if (!Textures.TryGetValue(vfxSamplerName, out var texture))
                {
                    Debug.Assert(false, $"Required texture {vfxSamplerName} not found in material.");
                }

                var uniformLocation = GL.GetUniformLocation(ShaderProgram, vfxSamplerName);

                Debug.Assert(uniformLocation != -1, "Something went wrong");

                GL.ActiveTexture(TextureUnit.Texture0 + textureUnit);
                texture.Bind();
                GL.Uniform1(uniformLocation, textureUnit);
                textureUnit++;
            }

            GL.DrawElements(PrimitiveType.Triangles, 6, DrawElementsType.UnsignedShort, 0);

            GL.UseProgram(0);
            GL.BindVertexArray(0);
            GL.Disable(EnableCap.DebugOutput);
            GL.Enable(EnableCap.DepthTest);
        }

        private void WriteMaterialBuffer(int shaderProgram, string programType, List<ParamBlock> parameters, ZDataBlock mainWriteSequence)
        {
            var index = 0;
            var globalsReordered = mainWriteSequence.Globals.OrderBy(x => x.Dest).ToList();

            // create a new material that holds evaluated expressions
            var evaluated = new Material();
            evaluated.FloatParams.EnsureCapacity(globalsReordered.Count);
            evaluated.VectorParams.EnsureCapacity(globalsReordered.Count);

            foreach (var field in globalsReordered)
            {
                var location = GL.GetUniformLocation(shaderProgram, $"Material_{programType}._m{index}");
                Debug.Assert(location != -1, "hit a field that doesn't exist in the shader");

                var parameter = parameters[field.ParamId];
                var material = Material;
                if (VfxEvaluate(parameter, evaluated))
                {
                    material = evaluated;
                }

                switch (parameter.VfxType)
                {
                    case Vfx.Type.Float:
                        var value = material.FloatParams.GetValueOrDefault(parameter.Name, parameter.FloatDefs[0]);
                        GL.Uniform1(location, value);
                        break;

                    case Vfx.Type.Float2 or Vfx.Type.Float3 or Vfx.Type.Float4:
                        var valueAsVec4 = material.VectorParams.GetValueOrDefault(parameter.Name, new Vector4(parameter.FloatDefs));
                        switch (parameter.VfxType)
                        {
                            case Vfx.Type.Float2:
                                GL.Uniform2(location, valueAsVec4.X, valueAsVec4.Y);
                                break;
                            case Vfx.Type.Float3:
                                GL.Uniform3(location, valueAsVec4.X, valueAsVec4.Y, valueAsVec4.Z);
                                break;
                            case Vfx.Type.Float4:
                                GL.Uniform4(location, valueAsVec4.X, valueAsVec4.Y, valueAsVec4.Z, valueAsVec4.W);
                                break;
                        }
                        break;

                    case Vfx.Type.Int:
                        GL.Uniform1(location, (int)material.IntParams.GetValueOrDefault(parameter.Name, parameter.IntDefs[0]));
                        break;

                    case Vfx.Type.Bool:
                        GL.Uniform1(location, (uint)material.IntParams.GetValueOrDefault(parameter.Name, parameter.IntDefs[0]));
                        break;

                    default:
                        throw new NotImplementedException("Material buffer write for vfx type not implemented");
                }

                index++;
            }
        }

        static readonly ushort[] quadIndices = [0, 1, 2, 0, 2, 3];

        private void SetViewPort(int width, int height)
        {
            // update vbo positions
            GL.BindVertexArray(VaoHandle);
            GL.EnableVertexAttribArray(0);
            GL.BindBuffer(BufferTarget.ArrayBuffer, VboHandle);

            for (var v = 0; v < 4; v++)
            {
                var vector = v switch
                {
                    0 => new Vector3(0, 0, 0),
                    1 => new Vector3(width, 0, 0),
                    2 => new Vector3(width, height, 0),
                    3 => new Vector3(0, height, 0),
                    _ => throw new NotImplementedException(),
                };

                var floatVector = new float[] { vector.X, vector.Y, vector.Z };

                GL.BufferSubData(BufferTarget.ArrayBuffer, sizeof(float) * 9 * v, sizeof(float) * 3, floatVector);
            }

            GL.BindVertexArray(0);
        }

        // TODO: Implement this using VBIB flow. 
        private void CreateScreenVertexArrayObject()
        {
            var data = new float[]
            {
                // (vPositionSs, vColor, vTexCoord)
                0, 0, 0, 1, 1, 1, 1, 0, 0,
                1024, 0, 0, 1, 1, 1, 1, 1, 0,
                1024, 1024, 0, 1, 1, 1, 1, 1, 1,
                0, 1024, 0, 1, 1, 1, 1, 0, 1,
            };

            VaoHandle = GL.GenVertexArray();
            GL.BindVertexArray(VaoHandle);

            VboHandle = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, VboHandle);
            GL.BufferData(BufferTarget.ArrayBuffer, data.Length * sizeof(float), data, BufferUsageHint.StaticDraw);

            var ebo = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, ebo);
            GL.BufferData(BufferTarget.ElementArrayBuffer, quadIndices.Length * sizeof(ushort), quadIndices, BufferUsageHint.StaticDraw);
            GL.GetBufferParameter(BufferTarget.ElementArrayBuffer, BufferParameterName.BufferSize, out int indexBufferSize);
            Debug.Assert(indexBufferSize == quadIndices.Length * sizeof(ushort));

            GL.EnableVertexAttribArray(0);
            var positionAttributeLocation = GL.GetAttribLocation(ShaderProgram, "vPositionSs");
            GL.EnableVertexAttribArray(positionAttributeLocation);
            GL.VertexAttribPointer(positionAttributeLocation, 3, VertexAttribPointerType.Float, false, sizeof(float) * 9, 0);

            GL.EnableVertexAttribArray(1);
            var colorAttributeLocation = GL.GetAttribLocation(ShaderProgram, "vColor");
            GL.EnableVertexAttribArray(colorAttributeLocation);
            GL.VertexAttribPointer(colorAttributeLocation, 4, VertexAttribPointerType.Float, false, sizeof(float) * 9, sizeof(float) * 3);


            GL.EnableVertexAttribArray(2);
            var texCoordAttributeLocation = GL.GetAttribLocation(ShaderProgram, "vTexCoord");
            GL.EnableVertexAttribArray(texCoordAttributeLocation);
            GL.VertexAttribPointer(texCoordAttributeLocation, 2, VertexAttribPointerType.Float, false, sizeof(float) * 9, sizeof(float) * 7);

            GL.BindVertexArray(0);
        }

        private int Compile(ShaderType shaderType, VulkanSource vulkanSource)
        {
            var shader = GL.CreateShader(shaderType);

            /*
            if (compileType == CompileType.Native)
            {
                var binaryFormat = (BinaryFormat)All.ShaderBinaryFormatSpirV; // GL_SHADER_BINARY_FORMAT_SPIR_V
                GL.ShaderBinary(1, ref shader, binaryFormat, vulkanSource.Bytecode, vulkanSource.Bytecode.Length);
                GL.SpecializeShader(shader, "main", 0, (int[])null, (int[])null);
            }
            */

            SpirvCrossApi.spvc_context_create(out var context).CheckResult();
            SpirvCrossApi.spvc_context_parse_spirv(context, vulkanSource.Bytecode, out var parsedIr).CheckResult();
            SpirvCrossApi.spvc_context_create_compiler(context, spvc_backend.SPVC_BACKEND_GLSL, parsedIr, spvc_capture_mode.SPVC_CAPTURE_MODE_TAKE_OWNERSHIP, out var compiler).CheckResult();

            SpirvCrossApi.spvc_compiler_create_compiler_options(compiler, out var options).CheckResult();
            SpirvCrossApi.spvc_compiler_options_set_uint(options, spvc_compiler_option.SPVC_COMPILER_OPTION_GLSL_VERSION, 460);
            SpirvCrossApi.spvc_compiler_options_set_bool(options, spvc_compiler_option.SPVC_COMPILER_OPTION_GLSL_ES, SpirvCrossApi.SPVC_FALSE);
            SpirvCrossApi.spvc_compiler_options_set_bool(options, spvc_compiler_option.SPVC_COMPILER_OPTION_MSL_ENABLE_DECORATION_BINDING, SpirvCrossApi.SPVC_TRUE);
            SpirvCrossApi.spvc_compiler_options_set_bool(options, spvc_compiler_option.SPVC_COMPILER_OPTION_GLSL_EMIT_UNIFORM_BUFFER_AS_PLAIN_UNIFORMS, SpirvCrossApi.SPVC_TRUE);
            SpirvCrossApi.spvc_compiler_install_compiler_options(compiler, options);

            SpirvCrossApi.spvc_compiler_build_combined_image_samplers(compiler);

            unsafe
            {
                SpirvCrossApi.spvc_compiler_create_shader_resources(compiler, out var resources).CheckResult();
                spvc_set activeSet;
                SpirvCrossApi.spvc_compiler_get_active_interface_variables(compiler, &activeSet).CheckResult();
                SpirvCrossApi.spvc_compiler_create_shader_resources_for_active_variables(compiler, &resources, activeSet).CheckResult();

                // vs
                Rename(shaderType, compiler, resources, spvc_resource_type.SPVC_RESOURCE_TYPE_STAGE_INPUT);
                Rename(shaderType, compiler, resources, spvc_resource_type.SPVC_RESOURCE_TYPE_STAGE_OUTPUT);

                Rename(shaderType, compiler, resources, spvc_resource_type.SPVC_RESOURCE_TYPE_UNIFORM_BUFFER);

                Rename(shaderType, compiler, resources, spvc_resource_type.SPVC_RESOURCE_TYPE_SAMPLED_IMAGE);
                Rename(shaderType, compiler, resources, spvc_resource_type.SPVC_RESOURCE_TYPE_SEPARATE_IMAGE);
                Rename(shaderType, compiler, resources, spvc_resource_type.SPVC_RESOURCE_TYPE_SEPARATE_SAMPLERS);

                var combinedSamplers = SpirvCrossApi.spvc_compiler_get_combined_image_samplers(compiler);

                foreach (var sampler in combinedSamplers)
                {
                    Debug.Assert(images.ContainsKey(sampler.image_id), "pointers gone wrong.");
                    Debug.Assert(images.ContainsKey(sampler.sampler_id), "pointers gone wrong.");

                    var image_binding = SpirvCrossApi.spvc_compiler_get_decoration(compiler, sampler.image_id, SpvDecoration.Binding);
                    SpirvCrossApi.spvc_compiler_set_decoration(compiler, sampler.combined_id, SpvDecoration.Binding, image_binding);

                    var name = GetNameForSampler(image_binding);
                    SpirvCrossApi.spvc_compiler_set_name(compiler, sampler.combined_id, name);
                }
            }

            SpirvCrossApi.spvc_compiler_compile(compiler, out var code).CheckResult();

            if (string.IsNullOrEmpty(code))
            {
                Log.Error("GL", $"Failed to cross compile {shaderType}: {code}");
                return -1;
            }

            GL.ShaderSource(shader, code);
            GL.CompileShader(shader);

            GL.GetShader(shader, ShaderParameter.CompileStatus, out var status);
            if (status != 1)
            {
                var infoLog = GL.GetShaderInfoLog(shader);
                GL.DeleteShader(shader);
                Log.Error("GL", $"Failed to compile {shaderType}: {infoLog}");
                return -1;
            }

            Log.Info("GL", $"{shaderType} compiled successfully");
            return shader;
        }

        private Dictionary<uint, uint> images = new();
        private int textureStartingPoint = -1;

        private string GetNameForSampler(uint image_binding)
        {
            Debug.Assert(textureStartingPoint != -1, $"{nameof(textureStartingPoint)} not set");

            return Pixel.LeadingData.Segment1
                .Select<WriteSeqField, (WriteSeqField Field, ParamBlock Param)>(f => (f, PixelShaders.ParamBlocks[f.ParamId]))
                .Where(fp => fp.Param.VfxType == Vfx.Type.Sampler2D)
                .FirstOrDefault(fp => fp.Field.Dest == image_binding - textureStartingPoint).Param?.Name ?? "undetermined";
        }

        private unsafe spvc_resources Rename(ShaderType shaderType, spvc_compiler compiler, spvc_resources resources, spvc_resource_type resourceType)
        {
            SpirvCrossApi.spvc_resources_get_resource_list_for_type(resources, resourceType, out var outResources, out var outResourceCount).CheckResult();
            for (nuint i = 0; i < outResourceCount; i++)
            {
                spvc_reflected_resource resource = outResources[i];
                var location = (int)SpirvCrossApi.spvc_compiler_get_decoration(compiler, resource.id, SpvDecoration.Location);
                var index = SpirvCrossApi.spvc_compiler_get_decoration(compiler, resource.id, SpvDecoration.Index);
                var binding = SpirvCrossApi.spvc_compiler_get_decoration(compiler, resource.id, SpvDecoration.Binding);

                if (resourceType == spvc_resource_type.SPVC_RESOURCE_TYPE_SEPARATE_IMAGE
                || resourceType == spvc_resource_type.SPVC_RESOURCE_TYPE_SEPARATE_SAMPLERS
                || resourceType == spvc_resource_type.SPVC_RESOURCE_TYPE_SAMPLED_IMAGE)
                {
                    if (textureStartingPoint == -1)
                    {
                        textureStartingPoint = (int)binding;
                    }

                    images[resource.id] = binding;
                }

                var sharedStruct = new string[] { "vColorUnused", "vBaseUV_PatternUv", "vWearUV_GrungeUv" };
                var name = resourceType switch
                {
                    spvc_resource_type.SPVC_RESOURCE_TYPE_STAGE_INPUT => shaderType switch
                    {
                        ShaderType.VertexShader => new string[] { "vPositionSs", "vColor", "vTexCoord" }[location],
                        ShaderType.FragmentShader => sharedStruct[location],
                        _ => throw new NotImplementedException(),
                    },

                    spvc_resource_type.SPVC_RESOURCE_TYPE_STAGE_OUTPUT => shaderType switch
                    {
                        ShaderType.VertexShader => sharedStruct[location],
                        ShaderType.FragmentShader => new string[] { "vColorOut" }[location],
                        _ => throw new NotImplementedException(),
                    },

                    spvc_resource_type.SPVC_RESOURCE_TYPE_UNIFORM_BUFFER
                        => "Material_" + shaderType,

                    spvc_resource_type.SPVC_RESOURCE_TYPE_SEPARATE_IMAGE or spvc_resource_type.SPVC_RESOURCE_TYPE_SAMPLED_IMAGE
                        => GetNameForSampler(binding),

                    _ => string.Empty,
                };

                if (string.IsNullOrEmpty(name))
                {
                    continue;
                }


                SpirvCrossApi.spvc_compiler_set_name(compiler, resource.id, name);
            }

            return resources;
        }

        enum VfxEvalFunction
        {
            UvScaleTransform,
            WeaponLengthTransform,
            Unknown,
        }

        readonly byte[] WeaponLegthTransformPattern = [0x4d, 0xb1, 0xc2, 0x2c];

        // hardcoded dynamic expression evaluations.
        private bool VfxEvaluate(ParamBlock parameter, Material evaluated)
        {
            if (parameter.Name == "g_vViewport")
            {
                //SetViewPort(1024, 1024);
                evaluated.VectorParams["g_vViewport"] = new Vector4(0, 0, 1024, 1024);
                return true;
            }

            if (parameter.DynExp.Length == 0)
            {
                return false;
            }

            if (parameter.Name[^6..^1] == "Xform")
            {
                var transformNumber = parameter.Name.AsSpan()[^1..][0] - '0';
                var transformType = VfxEvalFunction.UvScaleTransform;
                if (parameter.DynExp.AsSpan().IndexOf(WeaponLegthTransformPattern) != -1)
                {
                    transformType = VfxEvalFunction.WeaponLengthTransform;
                }

                var texture = parameter.Name[3..^6];

                {
                    bool g_bIgnoreWeaponSizeScale = Material.IntParams.GetValueOrDefault("g_bIgnoreWeaponSizeScale", 0) != 0;
                    float weaponSizeScale;
                    if (transformType == VfxEvalFunction.WeaponLengthTransform)
                    {
                        var g_flWeaponLength1 = Material.FloatParams.GetValueOrDefault("g_flWeaponLength1", 0f);
                        weaponSizeScale = g_bIgnoreWeaponSizeScale ? 1.0f : (g_flWeaponLength1 / 36.0f);
                    }
                    else
                    {
                        var g_flUvScale1 = Material.FloatParams.GetValueOrDefault("g_flUvScale1", 0f);
                        weaponSizeScale = g_bIgnoreWeaponSizeScale ? 1.0f : g_flUvScale1;
                    }

                    var flRotation = Material.FloatParams.GetValueOrDefault($"g_fl{texture}Rotation", 0f);
                    var flScale = Material.FloatParams.GetValueOrDefault($"g_fl{texture}Scale", 0f);
                    var vOffset = Material.VectorParams.GetValueOrDefault($"g_v{texture}Offset", Vector4.Zero);

                    var v0 = MathF.Floor((flRotation + .005f) * 100) / 100;
                    var v1 = MathF.Floor(((flScale * weaponSizeScale) + .005f) * 100) / 100;
                    var v2_0 = MathF.Floor((vOffset.X + .005f) * 100) / 100;
                    var v2_1 = MathF.Floor((vOffset.Y + .005f) * 100) / 100;
                    var v3 = (v0 * 3.1415927f) / 180;
                    var v4 = MathF.Cos(v3);
                    var v5 = MathF.Sin(v3);
                    var v6 = .5f / ((v1 != 0) ? v1 : 1);
                    var v7 = MathF.Cos(-v3);
                    var v8 = MathF.Sin(-v3);
                    var v9 = (v6 * v7) - (v6 * v8);
                    var v10 = (v9 * v8) + (v6 * v7);

                    if (transformNumber == 0)
                    {
                        evaluated.VectorParams[parameter.Name] = new(v4 * v1, -v5 * v1, 0, (v1 * v4 * v9) + (v1 * -v5 * v10) + (v2_0 - .5f));
                    }
                    else if (transformNumber == 1)
                    {
                        evaluated.VectorParams[parameter.Name] = new(v5 * v1, v4 * v1, 0, (v1 * v5 * v9) + (v1 * v4 * v10) + (v2_1 - .5f));
                    }
                }
            }
            else if (parameter.Name.StartsWith("g_vColor", StringComparison.Ordinal))
            {
                var value = Material.VectorParams.GetValueOrDefault(parameter.Name, new Vector4(parameter.FloatDefs));
                // srgb to linear
                value.X = MathF.Pow(value.X, 2.2f);
                value.Y = MathF.Pow(value.Y, 2.2f);
                value.Z = MathF.Pow(value.Z, 2.2f);
                evaluated.VectorParams[parameter.Name] = value;
            }
            else if (parameter.Name == "g_vSprayBiasBlend")
            {
                // Why is this packed
                var g_bBiasSpray = Material.IntParams.GetValueOrDefault("g_bBiasSpray", 0);
                var value = Material.VectorParams.GetValueOrDefault(parameter.Name, new Vector4(parameter.FloatDefs));
                evaluated.VectorParams[parameter.Name] = new Vector4(g_bBiasSpray, value.X, value.Y, 0);
            }
            else if (parameter.Name == "bRoughnessMode")
            {
                evaluated.IntParams[parameter.Name] = Material.IntParams.GetValueOrDefault("F_ROUGHNESS_MODE", 0);
            }
            else if (parameter.Name == "g_bRoughnessPerColor")
            {
                evaluated.IntParams[parameter.Name] = Material.IntParams.GetValueOrDefault("F_ROUGHNESS_PER_COLOR", 0);
            }
            else if (parameter.Name == "g_vMetallicPaintAlbedoLevels")
            {
                var levels = new Vector3(-0.8f, .6f, 1.0f);
                evaluated.VectorParams[parameter.Name] = new Vector4(-levels.X, -1.4427f * MathF.Log(MathF.Max(.0001f, 1 - levels.Y)), 2 - levels.Z, 0);
            }
            else if (parameter.Name == "g_vPaintAlbedoLevels")
            {
                var levels = new Vector3(-0.5f, .6f, 1.0f);
                evaluated.VectorParams[parameter.Name] = new Vector4(-levels.X, -1.4427f * MathF.Log(MathF.Max(.0001f, 1 - levels.Y)), 2 - levels.Z, 0);
            }

            // todo: renderstates AddressU, AddressV

            return true;
        }
    }
}
