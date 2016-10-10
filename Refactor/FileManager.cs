using System;
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
            var newExtension = string.IsNullOrEmpty(Options.CurrentOptions.BackupId)
                ? extension + ".backup"
                : extension + "." + Options.CurrentOptions.BackupId + ".backup";
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

        private static void AddItemToProject(string itemType, Project project, string include)
        {
            var msmodule = project.GetItems(itemType)
                .FirstOrDefault(i => i.UnevaluatedInclude == include);
            if (msmodule != null)
            {
                return;
            }
            var projectPath = project.FullPath;
            BackupFile(projectPath);
            project.AddItem(itemType, include);
            project.Save();
        }

        public static void AddCompileToProject(Project project, string include)
        {
            AddItemToProject("Compile", project, include);
        }
    }
}
