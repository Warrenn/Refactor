using CommandLine;

namespace Refactor.Angular
{
    public class AddModuleOptions
    {
        [Option('m', "module",
            Required = true,
            HelpText = "The module area that will added")]
        public string Module { get; set; }
    }
}
