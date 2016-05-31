using System;
using System.Globalization;
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
        public static void BackupFile(string fileName)
        {
            var fileAttributes = File.GetAttributes(fileName);
            var extension = Path.GetExtension(fileName);
            if ((fileAttributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
            {
                File.SetAttributes(fileName, fileAttributes & ~FileAttributes.ReadOnly);
            }

            var backupName = Path.ChangeExtension(fileName, extension + ".backup");
            for (var i = 1; File.Exists(backupName); i++)
            {
                backupName = Path.ChangeExtension(fileName, extension + "." + i + ".backup");
            }

            var contents = File.ReadAllText(fileName);
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
            var content = Engine.Razor.RunCompile(moduleTemplate, templateName, null, model);
            File.WriteAllText(path, content);
        }

        public static void AddContentToProject(Project project, string include)
        {
            var msmodule = project.GetItems("Content")
                .FirstOrDefault(i => i.UnevaluatedInclude == include);
            if (msmodule != null)
            {
                return;
            }
            var projectPath = project.FullPath;
            BackupFile(projectPath);
            project.AddItem("Content", include);
            project.Save();
        }
    }
}
