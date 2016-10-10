using CommandLine;

namespace Refactor.Angular
{
    public class AddControllerOptions
    {
        [Option('a', "area",
            Required = true,
            HelpText = "The area that will contain the new controller")]
        public string Area { get; set; }

        [Option('c', "controller",
            Required = true,
            HelpText = "The angular controller name")]
        public string Controller { get; set; }

        [Option('v', "service",
            Required = false,
            HelpText = "The service method call made by the angular controller")]
        public string Service { get; set; }

        [Option('b', "bundleid",
            Required = false,
            HelpText = "The bundle identifier to add the javascript file references to")]
        public string BundleId { get; set; }

        [Option('j', "jsroot",
            Required = false,
            HelpText = "The root path of the javascript files to add")]
        public string JsRoot { get; set; }

        [Option('l', "template",
            Required = false,
            HelpText = "The directory where the cshtml templates are stored")]
        public string Template { get; set; }
    }
}
