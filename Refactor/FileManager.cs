using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Build.Evaluation;
using RazorEngine;
using RazorEngine.Configuration;
using RazorEngine.Templating;

namespace Refactor
{
    public static class FileManager
    {
        public static void BackupFile(string fileName, string backupId)
        {
            var fileAttributes = File.GetAttributes(fileName);
            var extension = Path.GetExtension(fileName);
            var newExtension = string.IsNullOrEmpty(backupId)
                ? extension + "." + backupId + ".backup"
                : extension + ".backup";
            if ((fileAttributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
            {
                File.SetAttributes(fileName, fileAttributes & ~FileAttributes.ReadOnly);
            }

            var backupName = Path.ChangeExtension(fileName, newExtension);
            for (var i = 1; File.Exists(backupName); i++)
            {
                backupName = Path.ChangeExtension(fileName, extension + "." + i + ".backup");
            }

            var contents = File.ReadAllText(fileName);
            Trace.WriteLine(fileName);
            File.WriteAllText(backupName, contents);
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

        public static void CreateFileFromTemplate(string path, string templateName, object model)
        {
            CreateFileFromTemplate(path, templateName, null, model);
        }

        public static void CreateFileFromTemplate(string path, string templateName, Type modelType, object model)
        {
            if (File.Exists(path))
            {
                return;
            }
            var config = new TemplateServiceConfiguration
            {
                DisableTempFileLocking = true,
                CachingProvider = new DefaultCachingProvider(t => { })
            };

            Engine.Razor = RazorEngineService.Create(config);
            var moduleTemplate = GetTemplate(templateName);
            var content = Engine.Razor.RunCompile(moduleTemplate, templateName, modelType, model);
            Trace.WriteLine(path);
            File.WriteAllText(path, content);
        }

        public static void AddContentToProject(Project project, string include, string backupId)
        {
            var msmodule = project.GetItems("Content")
                .FirstOrDefault(i => i.UnevaluatedInclude == include);
            if (msmodule != null)
            {
                return;
            }
            var projectPath = project.FullPath;
            BackupFile(projectPath, backupId);
            project.AddItem("Content", include);
            project.Save();
        }
    }
}
