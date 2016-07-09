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
        public static void CopyIfChanged(FileEntry fileEntry, string backupId)
        {
            if (fileEntry.Document.Text == fileEntry.CSharpFile.OriginalText)
            {
                return;
            }

            try
            {
                BackupFile(fileEntry.CSharpFile.FileName, backupId);
                File.WriteAllText(fileEntry.CSharpFile.FileName, fileEntry.Document.Text);
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.ToString());
            }
        }

        public static void BackupFile(string fileName, string backupId)
        {
            var extension = Path.GetExtension(fileName);
            var newExtension = string.IsNullOrEmpty(backupId)
                ? extension + ".backup"
                : extension + "." + backupId + ".backup";
            var backupName = Path.ChangeExtension(fileName, newExtension);

            DisableReadOnly(fileName);
            BackupTheBackup(backupName);

            var contents = File.ReadAllText(fileName);
            Trace.WriteLine(fileName);
            File.WriteAllText(backupName, contents);
        }

        private static void BackupTheBackup(string backupFileName)
        {
            if (!File.Exists(backupFileName))
            {
                return;
            }

            var original = backupFileName;
            DisableReadOnly(original);

            for (var i = 1; File.Exists(backupFileName); i++)
            {
                backupFileName = Path.ChangeExtension(original, "." + i + ".backup");
            }

            var contents = File.ReadAllText(original);
            File.WriteAllText(backupFileName, contents);
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
            var directory = Path.GetDirectoryName(path);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
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
