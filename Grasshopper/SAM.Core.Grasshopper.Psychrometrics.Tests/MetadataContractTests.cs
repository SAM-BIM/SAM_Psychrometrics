// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;
using System.Linq;
using global::Grasshopper.Kernel;
using NUnit.Framework;
using SAM.Analytical.Grasshopper;

namespace SAM.Core.Grasshopper.Psychrometrics.Tests
{
    /// <summary>
    /// <b>The published parameter contract of the SAM_Psychrometrics Grasshopper components.</b>
    /// <para>
    /// A Grasshopper definition stores each component's parameters by name in its own archive, and - as
    /// <see cref="ParameterNamePersistenceTests"/> shows - a reopened component keeps the names found in the
    /// file rather than the names its class now declares. A renamed parameter therefore turns every
    /// <c>Params.IndexOfInputParam</c>/<c>IndexOfOutputParam</c> lookup in <c>SolveInstance</c> into -1 for
    /// every existing definition, with no compile error to warn anyone. These tests pin the names, order,
    /// access and optionality so that such a change cannot be made silently.
    /// </para>
    /// <para>
    /// The components' declared <c>Name</c> values are treated as a frozen legacy contract: several use
    /// camelCase outputs or <c>_name_</c>/<c>name</c> inputs that the SAM-BIM naming standard would spell
    /// differently. Normalising them is a breaking change and is deferred - see
    /// <c>docs/GH-Component-Metadata-Standard.md</c>. The descriptions are not frozen and may be improved.
    /// </para>
    /// </summary>
    [TestFixture]
    public class MetadataContractTests
    {
        private static readonly string[] PsychrometricsInputs =
        {
            "_dryBulbTemperature", "_relativeHumidity_", "_humidityRatio_", "_wetBulbTemperature_", "_dewPointTemperature_"
        };

        private static readonly string[] PsychrometricsOutputs =
        {
            "dryBulbTemperature", "relativeHumidity", "humidityRatio", "saturationHumidityRatio", "wetBulbTemperature",
            "dewPointTemperature", "saturationVapourPressure", "partialVapourPressure", "partialDryAirPressure",
            "enthalpy", "specificVolume", "density"
        };

        private static readonly string[] DryBulbInputs = { "_density", "_humidityRatio_", "_relativeHumidity_", "_pressure_" };
        private static readonly string[] DewPointInputs = { "_dryBulbTemperature", "_relativeHumidity", "_pressure_" };

        [Test]
        public void ComponentGuids_AreUnchanged()
        {
            Assert.That(new SAMPsychrometricsPsychrometrics().ComponentGuid, Is.EqualTo(new Guid("064dc9f2-a368-459a-9e97-b60588a269f1")));
            Assert.That(new SAMPsychrometricsDryBulbTemperature().ComponentGuid, Is.EqualTo(new Guid("7a795583-cabe-452e-864f-bce79135fd5c")));
            Assert.That(new SAMPsychrometricsDewPointTemperature().ComponentGuid, Is.EqualTo(new Guid("e9bf471d-4ca5-4847-b5b9-bb3fbf712e41")));
        }

        [Test]
        public void Psychrometrics_ParameterContract()
        {
            AssertContract(new SAMPsychrometricsPsychrometrics(), "1.0.6", PsychrometricsInputs, PsychrometricsOutputs,
                optionalInputs: new[] { "_relativeHumidity_", "_humidityRatio_", "_wetBulbTemperature_", "_dewPointTemperature_" });
        }

        [Test]
        public void DryBulbTemperature_ParameterContract()
        {
            AssertContract(new SAMPsychrometricsDryBulbTemperature(), "1.0.2", DryBulbInputs, new[] { "dryBulbTemperature" },
                optionalInputs: new[] { "_humidityRatio_", "_relativeHumidity_", "_pressure_" });
        }

        [Test]
        public void DewPointTemperature_ParameterContract()
        {
            AssertContract(new SAMPsychrometricsDewPointTemperature(), "1.0.2", DewPointInputs, new[] { "dewPointTemperature" },
                optionalInputs: new[] { "_pressure_" });
        }

        /// <summary>Voluntary parameters stay insertable, at their declared position, with their declared name.</summary>
        [Test]
        public void Psychrometrics_VoluntaryParameters_StayInsertableAtTheirDeclaredPosition()
        {
            SAMPsychrometricsPsychrometrics component = new SAMPsychrometricsPsychrometrics();

            Assert.That(component.CanInsertParameter(GH_ParameterSide.Input, 5), Is.True);
            Assert.That(component.CreateParameter(GH_ParameterSide.Input, 5).Name, Is.EqualTo("_pressure_"));

            // the two voluntary outputs are declared last, so they are inserted in turn, as the canvas does
            Assert.That(component.CanInsertParameter(GH_ParameterSide.Output, 12), Is.True);
            IGH_Param degreeSaturation = component.CreateParameter(GH_ParameterSide.Output, 12);
            Assert.That(degreeSaturation.Name, Is.EqualTo("degreeSaturation"));
            component.Params.RegisterOutputParam(degreeSaturation, 12);

            Assert.That(component.CanInsertParameter(GH_ParameterSide.Output, 13), Is.True);
            Assert.That(component.CreateParameter(GH_ParameterSide.Output, 13).Name, Is.EqualTo("pressure"));
        }

        private static void AssertContract(GH_SAMComponent component, string latestVersion, string[] inputs, string[] outputs, string[] optionalInputs)
        {
            Assert.That(component.LatestComponentVersion, Is.EqualTo(latestVersion), component.Name + " version");
            Assert.That(component.Params.Input.Select(p => p.Name), Is.EqualTo(inputs), component.Name + " inputs");
            Assert.That(component.Params.Output.Select(p => p.Name), Is.EqualTo(outputs), component.Name + " outputs");

            foreach (IGH_Param param in component.Params.Input.Concat(component.Params.Output))
            {
                Assert.That(param.Access, Is.EqualTo(GH_ParamAccess.item), param.Name + " access");
                Assert.That(param.NickName, Is.EqualTo(param.Name), param.Name + " nickname must equal name");
                AssertDescription(param);
            }

            foreach (IGH_Param param in component.Params.Input)
            {
                bool expected = optionalInputs.Contains(param.Name);
                Assert.That(param.Optional, Is.EqualTo(expected), param.Name + " optional");
            }

            foreach (IGH_Param param in component.Params.Output)
            {
                Assert.That(param.Optional, Is.False, param.Name + " output optional");
            }
        }

        private static void AssertDescription(IGH_Param param)
        {
            string description = param.Description ?? string.Empty;
            Assert.That(description.Trim(), Is.Not.Empty, param.Name + " has no description");
            Assert.That(char.IsUpper(description.TrimStart()[0]), Is.True, param.Name + " description must begin with a capital letter");
            Assert.That(description, Does.Not.Contain("TODO").IgnoreCase, param.Name + " description has a placeholder");
            Assert.That(description, Does.Not.Contain("TBD").IgnoreCase, param.Name + " description has a placeholder");
            Assert.That(description, Does.Not.Contain("Humidty"), param.Name + " description has a known typo");
        }
    }
}
