﻿using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Aspect.Abstractions;
using Aspect.Extensions;
using Aspect.Policies.Suite;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Aspect.Commands
{
    internal class PolicyInitCommand : Command<PolicyInitCommandSettings>
    {
        private readonly IReadOnlyDictionary<string, ICloudProvider> _cloudProviders;
        private readonly IPolicySuiteSerializer _policySuiteSerializer;

        public PolicyInitCommand(IReadOnlyDictionary<string, ICloudProvider> cloudProviders, IPolicySuiteSerializer policySuiteSerializer)
        {
            _cloudProviders = cloudProviders;
            _policySuiteSerializer = policySuiteSerializer;
        }

        public override ValidationResult Validate([NotNull] CommandContext context, [NotNull] PolicyInitCommandSettings settings)
        {
            if (string.IsNullOrWhiteSpace(settings.FileName))
                return ValidationResult.Error("Please specify a file name.");

            if (File.Exists(settings.FileName))
                return ValidationResult.Error($"The file {settings.FileName} already exists.");

            return base.Validate(context, settings);
        }

        public override int Execute([NotNull] CommandContext context, [NotNull] PolicyInitCommandSettings settings)
        {
            string policy;
            if (settings.InitializeSuite.GetValueOrDefault(false))
            {
                policy = InitializePolicySuite();
            }
            else
            {
                policy = InitializePolicyFile(settings.Resource);
            }


            using var file = File.CreateText(settings.FileName!);
            file.Write(policy);
            file.Flush();
            file.Close();

            return 0;
        }

        private string InitializePolicySuite()
        {
            return _policySuiteSerializer.Serialize(new PolicySuite
            {
                Name = "My Best Practises",
                Description = "Describe what the policy suite does",
                Policies = new []
                {
                    new PolicyElement
                    {
                        Name = "AWS Best Practises",
                        Description = "Describing this section",
                        Type = "AWS",
                        Regions = new [] {"eu-west-1"},
                        Policies = new [] { "D:\\Policies\\MyPolicy.policy" }
                    },
                    new PolicyElement
                    {
                        Name = "Azure Best Practises",
                        Description = "Describing this section",
                        Type = "Azure",
                        Regions = new [] {"uk-south"},
                        Policies = new [] { "D:\\Policies\\MyPolicy.policy" }
                    }
                }
            });
        }

        private string InitializePolicyFile(string? resource)
        {
            if (string.IsNullOrWhiteSpace(resource))
            {
                var provider = _cloudProviders[ConsoleExtensions.PromptOrDefault("Select cloud provider:", _cloudProviders.Keys, "AWS")];
                var resources = provider.GetResources();
                resource = ConsoleExtensions.PromptOrDefault("Select resource:", resources.Keys);
            }

            return $@"resource ""{resource}""

validate {{
    # Enter one or more statements like the following that should be validated
    input.Property == ""something""
}}";
        }
    }
}
