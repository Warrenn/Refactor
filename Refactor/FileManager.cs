using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Build.Evaluation;
using Mono.CSharp;
using RazorEngine;
using RazorEngine.Configuration;
using RazorEngine.Templating;

namespace Refactor
{
    public static class FileManager
    {
        public static void CopyIfChanged(FileEntry fileEntry)
        {
            if (fileEntry.Document.Text == fileEntry.CSharpFile.OriginalText)
            {
                return;
            }

            try
            {
                BackupFile(fileEntry.CSharpFile.FileName);
                File.WriteAllText(fileEntry.CSharpFile.FileName, fileEntry.Document.Text);
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.ToString());
            }
        }

        public static void BackupFile(string fileName)
        {
            var extension = Path.GetExtension(fileName);
            var backupId = string.IsNullOrEmpty(Options.CurrentOptions.BackupId)
                ? DateTime.Now.ToString("yyyyMMddhhmm")
                : Options.CurrentOptions.BackupId;
            var newExtension = extension + "." + backupId + ".backup";
            var backupName = Path.ChangeExtension(fileName, newExtension);

            if (File.Exists(backupName))
            {
                return;
            }

            DisableReadOnly(fileName);

            var contents = File.ReadAllText(fileName);
            Trace.WriteLine(fileName);
            File.WriteAllText(backupName, contents);
        }

        private static void DisableReadOnly(string fileName)
        {
            var fileAttributes = File.GetAttributes(fileName);

            if ((fileAttributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
            {
                File.SetAttributes(fileName, fileAttributes & ~FileAttributes.ReadOnly);
            }
        }

        public static string GetTemplate(string templateName)
        {
            var assembly = Assembly.GetExecutingAssembly();

            using (var stream = assembly.GetManifestResourceStream(templateName))
            using (var reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }

        public static void CreateFileFromTemplate<T>(string path, string templateName, T model, string templatePath)
        {
            if (File.Exists(path))
            {
                return;
            }

            var modelType = typeof(T);
            var fullTemplateName = modelType.Namespace + "." + templateName;
            var templateFileName = Path.Combine(templatePath, templateName);
            var moduleTemplate = (string.IsNullOrEmpty(templatePath) || !File.Exists(templateFileName))
                ? GetTemplate(fullTemplateName)
                : File.ReadAllText(templateFileName);
            var config = new TemplateServiceConfiguration
            {
                DisableTempFileLocking = true,
                CachingProvider = new DefaultCachingProvider(t => { })
            };

            Engine.Razor = RazorEngineService.Create(config);

            var content = Engine.Razor.RunCompile(moduleTemplate, templateName, modelType, model);
            Trace.WriteLine(path);
            var directory = Path.GetDirectoryName(path);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            File.WriteAllText(path, content);
        }

        public static void AddContentToProject(Project project, string include)
        {
            AddItemToProject("Content", project, include);
        }

        private static void AddItemToProject(string itemType, Project project, string include,
            IEnumerable<KeyValuePair<string, string>> metaData = null)
        {
            var msmodule = project.GetItems(itemType)
                .FirstOrDefault(i => i.UnevaluatedInclude == include);
            if (msmodule != null)
            {
                return;
            }
            var projectPath = project.FullPath;
            BackupFile(projectPath);
            if (metaData == null)
            {
                project.AddItem(itemType, include);
            }
            else
            {
                project.AddItem(itemType, include, metaData);
            }
            project.Save();
        }

        public static void AddCompileToProject(Project project, string include)
        {
            AddItemToProject("Compile", project, include);
        }

        public static void AddWcfReferenceToProject(Project project, string wcfPath)
        {
            var projectDirectory = Path.GetDirectoryName(project.FullPath);

            AddItemToProject("WCFMetadataStorage", project, wcfPath);

            var pathParts = wcfPath.Split(new[] {Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar},
                StringSplitOptions.RemoveEmptyEntries);
            AddItemToProject("WCFMetadata", project, pathParts[0] + Path.PathSeparator);


            foreach (var filename in Directory.GetFiles(Path.Combine(projectDirectory, wcfPath)))
            {
                var fileNameOnly = Path.GetFileName(filename);

                if (string.Equals(fileNameOnly, "Reference.cs",
                    StringComparison.InvariantCultureIgnoreCase))
                {
                    AddItemToProject("Compile", project, Path.Combine(wcfPath, "Reference.cs"),
                        new Dictionary<string, string>
                        {
                            {"AutoGen", "True"},
                            {"DesignTime", "True"},
                            {"DependentUpon", "Reference.svcmap"}
                        });
                    continue;
                }

                if (string.Equals(fileNameOnly, "Reference.svcmap",
                    StringComparison.InvariantCultureIgnoreCase))
                {
                    AddItemToProject("None", project, Path.Combine(wcfPath, "Reference.svcmap"),
                        new Dictionary<string, string>
                        {
                            {"Generator", "WCF Proxy Generator"},
                            {"LastGenOutput", "Reference.cs"}
                        });
                    continue;
                }

                var fileType = Path.GetExtension(filename);

                if (fileType == ".datasource")
                {
                    AddItemToProject("None", project, Path.Combine(wcfPath, fileNameOnly),
                        new Dictionary<string, string>
                        {
                            {"DependentUpon", "Reference.svcmap"}
                        });
                    continue;
                }

                if ((new[] { ".svcinfo", ".wsdl" }).Contains(fileType))
                {
                    AddItemToProject("None", project, Path.Combine(wcfPath, fileNameOnly));
                }
            }

            project.Save();
        }
    }
}
