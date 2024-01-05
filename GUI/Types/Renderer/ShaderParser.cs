using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace GUI.Types.Renderer
{
    partial class ShaderParser
    {
        private const string ShaderDirectory = "GUI.Types.Renderer.Shaders.";
        private const string ExpectedShaderVersion = "#version 460";

        [GeneratedRegex("^#include \"(?<IncludeName>[^\"]+)\"")]
        private static partial Regex RegexInclude();
        [GeneratedRegex("^#define (?<ParamName>\\S+) (?<DefaultValue>\\S+)")]
        private static partial Regex RegexDefine();

        private int sourceFileNumber;
        public List<string> SourceFiles { get; } = [];

#if DEBUG
        public List<List<string>> SourceFileLines { get; } = [];
#endif

        public void Reset()
        {
            sourceFileNumber = 0;
            SourceFiles.Clear();

#if DEBUG
            SourceFileLines.Clear();
#endif
        }

        public string PreprocessShader(string shaderFile, string originalShaderName, IReadOnlyDictionary<string, byte> arguments, HashSet<string> defines)
        {
            var isFirstLine = true;
            var resolvedIncludes = new HashSet<string>(4);
            var builder = new StringBuilder();

            void AppendLineNumber(int a, int b)
            {
                builder.Append("#line ");
                builder.Append(a.ToString(CultureInfo.InvariantCulture));
                builder.Append(' ');
                builder.Append(b.ToString(CultureInfo.InvariantCulture));
                builder.Append('\n');
            }

            void LoadShaderString(string shaderFileToLoad, string parentFile, bool isInclude)
            {
                if (parentFile != null)
                {
                    var folder = Path.GetDirectoryName(parentFile);

                    if (!string.IsNullOrEmpty(folder))
                    {
                        shaderFileToLoad = $"{folder}/{shaderFileToLoad}";
                    }

                    if (!resolvedIncludes.Add(shaderFileToLoad))
                    {
                        //Console.WriteLine($"{shaderFileToLoad} already loaded");
                        return;
                    }
                }

                using var stream = GetShaderStream(shaderFileToLoad);
                using var reader = new StreamReader(stream);
                string line;
                var lineNum = 1;
                var currentSourceFileNumber = sourceFileNumber++;
                SourceFiles.Add(shaderFileToLoad);

#if DEBUG
                var currentSourceLines = new List<string>();
                SourceFileLines.Add(currentSourceLines);
#endif

                while ((line = reader.ReadLine()) != null)
                {
                    lineNum++;

                    if (lineNum == 2)
                    {
                        if (line != ExpectedShaderVersion)
                        {
                            throw new InvalidProgramException($"First line must be '{ExpectedShaderVersion}' in '{shaderFileToLoad}'");
                        }

                        if (isInclude)
                        {
#if DEBUG
                            currentSourceLines.Add("// :VrfPreprocessed {line}");
#endif

                            // We add #version even in includes so that they can be compiled individually for better editing experience
                            continue;
                        }
                    }

#if DEBUG
                    currentSourceLines.Add(line);
#endif

                    // TODO: Support leading whitespace?
                    if (line.Length > 7 && line[0] == '#')
                    {
                        // Includes
                        var match = RegexInclude().Match(line);

                        if (match.Success)
                        {
                            // Recursively append included shaders

                            var includeName = match.Groups["IncludeName"].Value;

                            AppendLineNumber(1, sourceFileNumber);
                            LoadShaderString(includeName, shaderFileToLoad, isInclude: true);
                            AppendLineNumber(lineNum, currentSourceFileNumber);

                            continue;
                        }

                        // Defines
                        match = RegexDefine().Match(line);

                        if (match.Success)
                        {
                            var defineName = match.Groups["ParamName"].Value;

                            defines.Add(defineName);

                            // Check if this parameter is in the arguments
                            if (!arguments.TryGetValue(defineName, out var value))
                            {
                                builder.Append(line);
                                builder.Append('\n');
                                continue;
                            }

                            // Overwrite default value
                            var newValue = value.ToString(CultureInfo.InvariantCulture);

                            builder.Append("#define ");
                            builder.Append(defineName);
                            builder.Append(' ');
                            builder.Append(newValue);
                            builder.Append(" // :VrfPreprocessed\n");

                            continue;
                        }
                    }

                    builder.Append(line);
                    builder.Append('\n');

                    if (line.Contains("#endif", StringComparison.Ordinal))
                    {
                        // Fix an issue where #include is inside of an #if, which messes up line numbers
                        AppendLineNumber(lineNum, currentSourceFileNumber);
                    }

                    // Append original shader name as a define
                    if (isFirstLine)
                    {
                        isFirstLine = false;
                        builder.Append("#define ");
                        builder.Append(Path.GetFileNameWithoutExtension(originalShaderName));
                        builder.Append("_vfx 1 // :VrfPreprocessed\n");
                        AppendLineNumber(lineNum, currentSourceFileNumber);
                    }
                }
            }

            LoadShaderString(shaderFile, null, isInclude: false);

            return builder.ToString();
        }

#if !DEBUG
        private static Stream GetShaderStream(string name)
        {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            return assembly.GetManifestResourceStream($"{ShaderDirectory}{name.Replace('/', '.')}");
        }
#else
        public static readonly string ShadersFolderPathOnDisk = GetShadersFolder();

        private static FileStream GetShaderStream(string name)
        {
            return File.Open(GetShaderDiskPath(name), FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        }

        public static string GetShaderDiskPath(string name)
        {
            return Path.Combine(ShadersFolderPathOnDisk, ShaderDirectory.Replace('.', '/'), name);
        }

        private static string GetShadersFolder()
        {
            var root = System.Reflection.Assembly.GetExecutingAssembly().Location;
            var failsafe = 10;
            var fileName = string.Empty;

            do
            {
                root = Path.GetDirectoryName(root);
                fileName = Path.GetFileName(root);

                if (failsafe-- == 0)
                {
                    throw new DirectoryNotFoundException("Failed to find GUI folder for the shaders, are you debugging in some unconventional setup?");
                }
            }
            while (fileName != "GUI");

            return Path.GetDirectoryName(root);
        }
#endif
    }
}