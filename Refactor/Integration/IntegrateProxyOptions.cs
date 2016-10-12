using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;

namespace Refactor.Integration
{
    public class IntegrateProxyOptions
    {
        [Option('n', "name",
            Required = true,
            HelpText = "The name of the section that the remote proxy belongs to")]
        public string Name { get; set; }

        [Option('c', "cerberus",
            Required = true,
            HelpText = "The solution path of the cerberus gateway solution")]
        public string CerberusSln { get; set; }
    }
}
