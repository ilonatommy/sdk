// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.CommandLine;
using Microsoft.DotNet.Cli.BuildServer;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.Cli.Utils.Extensions;

namespace Microsoft.DotNet.Cli.Commands.BuildServer.Shutdown;

internal class BuildServerShutdownCommand : CommandBase
{
    private readonly ServerEnumerationFlags _enumerationFlags;
    private readonly IBuildServerProvider _serverProvider;
    private readonly bool _useOrderedWait;
    private readonly IReporter _reporter;
    private readonly IReporter _errorReporter;

    public BuildServerShutdownCommand(
        ParseResult result,
        IBuildServerProvider serverProvider = null,
        bool useOrderedWait = false,
        IReporter reporter = null)
        : base(result)
    {
        bool msbuild = result.GetValue(BuildServerShutdownCommandParser.MSBuildOption);
        bool vbcscompiler = result.GetValue(BuildServerShutdownCommandParser.VbcsOption);
        bool razor = result.GetValue(BuildServerShutdownCommandParser.RazorOption);
        bool all = !msbuild && !vbcscompiler && !razor;

        _enumerationFlags = ServerEnumerationFlags.None;
        if (msbuild || all)
        {
            _enumerationFlags |= ServerEnumerationFlags.MSBuild;
        }

        if (vbcscompiler || all)
        {
            _enumerationFlags |= ServerEnumerationFlags.VBCSCompiler;
        }

        if (razor || all)
        {
            _enumerationFlags |= ServerEnumerationFlags.Razor;
        }

        _serverProvider = serverProvider ?? new BuildServerProvider();
        _useOrderedWait = useOrderedWait;
        _reporter = reporter ?? Reporter.Output;
        _errorReporter = reporter ?? Reporter.Error;
    }

    public override int Execute()
    {
        var tasks = StartShutdown();

        if (tasks.Count == 0)
        {
            _reporter.WriteLine(CliCommandStrings.NoServersToShutdown.Green());
            return 0;
        }

        bool success = true;
        while (tasks.Count > 0)
        {
            var index = WaitForResult([.. tasks.Select(t => t.Item2)]);
            var (server, task) = tasks[index];

            if (task.IsFaulted)
            {
                success = false;
                WriteFailureMessage(server, task.Exception);
            }
            else
            {
                WriteSuccessMessage(server);
            }

            tasks.RemoveAt(index);
        }

        return success ? 0 : 1;
    }

    private List<(IBuildServer, Task)> StartShutdown()
    {
        var tasks = new List<(IBuildServer, Task)>();
        foreach (var server in _serverProvider.EnumerateBuildServers(_enumerationFlags))
        {
            WriteShutdownMessage(server);
            tasks.Add((server, Task.Run(() => server.Shutdown())));
        }

        return tasks;
    }

    private int WaitForResult(Task[] tasks)
    {
        if (_useOrderedWait)
        {
            return Task.WaitAny(tasks.First());
        }
        return Task.WaitAny(tasks);
    }

    private void WriteShutdownMessage(IBuildServer server)
    {
        if (server.ProcessId != 0)
        {
            _reporter.WriteLine(
                string.Format(
                    CliCommandStrings.ShuttingDownServerWithPid,
                    server.Name,
                    server.ProcessId));
        }
        else
        {
            _reporter.WriteLine(
                string.Format(
                    CliCommandStrings.ShuttingDownServer,
                    server.Name));
        }
    }

    private void WriteFailureMessage(IBuildServer server, AggregateException exception)
    {
        if (server.ProcessId != 0)
        {
            _reporter.WriteLine(
                string.Format(
                    CliCommandStrings.ShutDownFailedWithPid,
                    server.Name,
                    server.ProcessId,
                    exception.InnerException.Message).Red());
        }
        else
        {
            _reporter.WriteLine(
                string.Format(
                    CliCommandStrings.ShutDownFailed,
                    server.Name,
                    exception.InnerException.Message).Red());
        }

        if (CommandLoggingContext.IsVerbose)
        {
            Reporter.Verbose.WriteLine(exception.ToString().Red());
        }
    }

    private void WriteSuccessMessage(IBuildServer server)
    {
        if (server.ProcessId != 0)
        {
            _reporter.WriteLine(
                string.Format(
                    CliCommandStrings.ShutDownSucceededWithPid,
                    server.Name,
                    server.ProcessId).Green());
        }
        else
        {
            _reporter.WriteLine(
                string.Format(
                    CliCommandStrings.ShutDownSucceeded,
                    server.Name).Green());
        }
    }
}
