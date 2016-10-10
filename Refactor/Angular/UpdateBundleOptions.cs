using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;

namespace Refactor.Angular
{
    public class UpdateBundleOptions
    {
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
