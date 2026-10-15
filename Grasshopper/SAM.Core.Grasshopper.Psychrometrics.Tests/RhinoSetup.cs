// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using NUnit.Framework;

namespace SAM.Core.Grasshopper.Psychrometrics.Tests
{
    /// <summary>
    /// Hosts a real, headless Rhino 8 + Grasshopper runtime in-process (McNeel's Rhino.Testing / RhinoCore,
    /// configured by <c>Rhino.Testing.Configs.xml</c>) for the whole test assembly, exactly as
    /// <c>SAM.Core.Grasshopper.Tests</c> and the repository's <c>GhIconTest</c> do.
    /// </summary>
    [SetUpFixture]
    public sealed class RhinoSetup : global::Rhino.Testing.Fixtures.RhinoSetupFixture
    {
    }
}
