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
    }
}
