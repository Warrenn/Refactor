using CommandLine;

namespace Refactor.Angular
{
    public class WireUpOptions
    {
        [Option('v', "service",
            Required = true,
            HelpText = "The service interface to base the creation of objects")]
        public string Service { get; set; }

        [Option('b', "bundleid",
            Required = false,
            HelpText = "The bundle identifier to add the javascript file references to")]
        public string BundleId { get; set; }

        [Option('j', "jsroot",
            Required = false,
            HelpText = "The root path of the javascript files to add")]
        public string JsRoot { get; set; }

        [Option('p', "project",
            Required = false,
            HelpText = "The Project name containing all the files that will be examined.")]
        public string Project { get; set; }

        [Option('t', "route",
            Required = false,
            HelpText = "The route identifier of the Web API route")]
        public string Route { get; set; }
    }
}
