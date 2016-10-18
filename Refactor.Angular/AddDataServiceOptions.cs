using CommandLine;

namespace Refactor.Angular
{
    public class AddDataServiceOptions
    {
        [Option('c', "controller",
            Required = true,
            HelpText = "the web api controller name for the data service")]
        public string Controller { get; set; }

        [Option('p', "project",
            Required = false,
            HelpText = "The Project name containing all the files that will be examined.")]
        public string Project { get; set; }

        [Option('b', "bundleid",
            Required = false,
            HelpText = "The bundle identifier to add the javascript file references to")]
        public string BundleId { get; set; }

        [Option('j', "jsroot",
            Required = false,
            HelpText = "The root path of the javascript files to add")]
        public string JsRoot { get; set; }

        [Option('t', "route",
            Required = false,
            HelpText = "The route identifier of the Web API route")]
        public string Route { get; set; }

        [Option('l', "template",
            Required = false,
            HelpText = "The directory where the cshtml templates are stored")]
        public string Template { get; set; }
    }
}
