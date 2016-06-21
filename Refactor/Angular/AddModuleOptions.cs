using CommandLine;

namespace Refactor.Angular
{
    public class AddModuleOptions
    {
        [Option('m', "module",
            Required = true,
            HelpText = "The module area that will added")]
        public string Module { get; set; }

        [Option('b', "bundleid",
            Required = false,
            HelpText = "The bundle identifier to add the javascript file references to")]
        public string BundleId { get; set; }

        [Option('j', "jsroot",
            Required = false,
            HelpText = "The root path of the javascript files to add")]
        public string JsRoot { get; set; }
    }
}
