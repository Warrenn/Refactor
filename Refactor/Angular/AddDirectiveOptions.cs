using CommandLine;

namespace Refactor.Angular
{
    public class AddDirectiveOptions
    {
        [Option('a', "area",
            Required = true,
            HelpText = "the area that wil contain the new directive")]
        public string Area { get; set; }

        [Option('d', "directive",
            Required = true,
            HelpText = "the name of the new directive")]
        public string Directive { get; set; }
    }
}
