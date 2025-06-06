﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable disable

namespace Microsoft.DotNet.Cli.Commands.Test.Terminal;

/// <summary>
/// Outcome of a test.
/// </summary>
internal enum TestOutcome
{
    /// <summary>
    /// Error.
    /// </summary>
    Error,

    /// <summary>
    /// Fail.
    /// </summary>
    Fail,

    /// <summary>
    /// Passed.
    /// </summary>
    Passed,

    /// <summary>
    /// Skipped.
    /// </summary>
    Skipped,

    /// <summary>
    ///  Timeout.
    /// </summary>
    Timeout,

    /// <summary>
    /// Canceled.
    /// </summary>
    Canceled,
}
