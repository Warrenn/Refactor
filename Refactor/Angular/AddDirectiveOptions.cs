using CommandLine;

namespace Refactor.Angular
{
    public class AddDirectiveOptions
    {
        [Option('a', "area",
            Required = false,
            HelpText = "the area that will contain the new directive")]
        public string Area { get; set; }

        [Option('d', "directive",
            Required = false,
            HelpText = "the name of the new directive")]
        public string Directive { get; set; }

        [Option('w', "webapi",
            Required = true,
            HelpText = "The web api controller name and method call")]
        public string WebApi { get; set; }

        [Option('m', "method",
            Required = false,
            HelpText = "The web api service method called")]
        public string ServiceMethod { get; set; }

        [Option('n', "servicename",
            Required = false,
            HelpText = "The name of angular dataservice")]
        public string ServiceName { get; set; }

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
