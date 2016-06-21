using CommandLine;

namespace Refactor.Angular
{
    public class AddDirectiveOptions
    {
        [Option('a', "area",
            Required = true,
            HelpText = "the area that will contain the new directive")]
        public string Area { get; set; }

        [Option('d', "directive",
            Required = true,
            HelpText = "the name of the new directive")]
        public string Directive { get; set; }

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
