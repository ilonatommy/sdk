// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.CommandLine;
using Microsoft.DotNet.Cli.Extensions;
using Microsoft.DotNet.Cli.Utils;

namespace Microsoft.DotNet.Cli.Telemetry;

internal class AllowListToSendFirstAppliedOptions(
    HashSet<string> topLevelCommandNameAllowList) : IParseResultLogRule
{
    private HashSet<string> _topLevelCommandNameAllowList { get; } = topLevelCommandNameAllowList;

    public List<ApplicationInsightsEntryFormat> AllowList(ParseResult parseResult, Dictionary<string, double> measurements = null)
    {
        var topLevelCommandNameFromParse = parseResult.RootSubCommandResult();
        var result = new List<ApplicationInsightsEntryFormat>();
        if (_topLevelCommandNameAllowList.Contains(topLevelCommandNameFromParse))
        {
            var firstOption = parseResult.RootCommandResult.Children
                .OfType<System.CommandLine.Parsing.CommandResult>().FirstOrDefault()?
                .Children.OfType<System.CommandLine.Parsing.CommandResult>().FirstOrDefault()?.Command.Name ?? null;
            if (firstOption != null)
            {
                result.Add(new ApplicationInsightsEntryFormat(
                    "sublevelparser/command",
                    new Dictionary<string, string>
                    {
                        { "verb", topLevelCommandNameFromParse},
                        {"argument", firstOption}
                    },
                    measurements));
            }
        }
        return result;
    }
}
