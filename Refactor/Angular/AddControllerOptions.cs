using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        [Option('s', "service",
            Required = false,
            HelpText = "The service method call made by the angular controller")]
        public string Service { get; set; }

        [Option('j', "jsid",
            Required = false,
            HelpText = "The identifier of the javascript bundles collection")]
        public string JsIdentifier { get; set; }
    }
}
