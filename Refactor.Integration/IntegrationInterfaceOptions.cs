using CommandLine;

namespace Refactor.Integration
{
    public class IntegrationInterfaceOptions
    {
        [Option('a', "agent",
            Required = true,
            HelpText = "The name of the agent class that interface will be generated from")]
        public string Agent { get; set; }

    }
}
