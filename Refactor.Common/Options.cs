using CommandLine;

namespace Refactor
{
    public class Options
    {
        [Option('s', "solution",
            Required = true,
            HelpText = "Solution file containing all files and projects that will be examined.")]
        public string Solution { get; set; }

        [Option('p', "project",
            Required = false,
            HelpText = "The Project name containing all the files that will be examined.")]
        public string Project { get; set; }

        [Option('r', "refactory",
            Required = true,
            HelpText = "Strategy to use in refactoring the files in the solution.")]
        public string Refactory { get; set; }

        [Option('k', "backupid",
            Required = false,
            HelpText = "Backup id is used to prevent overriding of previous backups and to keep the backup of changes in sync.")]
        public string BackupId { get; set; }

        [Option('l', "template",
            Required = false,
            HelpText = "The directory where the cshtml templates are stored")]
        public string TemplatesFolder { get; set; }

        public static Options CurrentOptions { get; set; }
    }
}