// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Drawing;
using System.IO;
using global::Grasshopper.Kernel;
using global::Grasshopper.Kernel.Parameters;
using NUnit.Framework;
using SAM.Analytical.Grasshopper;

namespace SAM.Core.Grasshopper.Psychrometrics.Tests
{
    /// <summary>
    /// Regenerates <c>Fixtures/Psychrometrics-Legacy.ghx</c> - a definition captured with a pre-pilot build,
    /// used by <see cref="SavedDefinitionCompatibilityTests"/>. Explicit: it writes into the source tree and
    /// must only be run deliberately, with a build whose components are the intended legacy baseline.
    /// </summary>
    [TestFixture]
    [Explicit("Regenerates the committed compatibility fixture; run deliberately.")]
    public class FixtureGenerator
    {
        [Test]
        public void Generate()
        {
            string dir = Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "Fixtures");
            dir = Path.GetFullPath(dir);
            Directory.CreateDirectory(dir);
            string path = Path.Combine(dir, "Psychrometrics-Legacy.ghx");

            GH_Document document = new GH_Document();

            SAMPsychrometricsPsychrometrics psychrometrics = new SAMPsychrometricsPsychrometrics();
            document.AddObject(psychrometrics, false);
            psychrometrics.CreateAttributes();
            psychrometrics.Attributes.Pivot = new PointF(100, 100);
            ((Param_Number)psychrometrics.Params.Input[0]).SetPersistentData(20.0);      // _dryBulbTemperature
            ((Param_Number)psychrometrics.Params.Input[1]).SetPersistentData(50.0);      // _relativeHumidity_

            // _pressure_ is declared Voluntary, so a fresh instance does not carry it. Insert it the way
            // the canvas does, then type a value into it.
            Assert.That(psychrometrics.CanInsertParameter(GH_ParameterSide.Input, 5), Is.True);
            IGH_Param pressure = psychrometrics.CreateParameter(GH_ParameterSide.Input, 5);
            Assert.That(pressure, Is.Not.Null);
            Assert.That(pressure.Name, Is.EqualTo("_pressure_"));
            psychrometrics.Params.RegisterInputParam(pressure, 5);
            psychrometrics.Params.OnParametersChanged();
            psychrometrics.VariableParameterMaintenance();
            ((Param_Number)psychrometrics.Params.Input[5]).SetPersistentData(101325.0);  // _pressure_ (voluntary)

            Param_Number enthalpy = new Param_Number();
            document.AddObject(enthalpy, false);
            enthalpy.CreateAttributes();
            enthalpy.NickName = "enthalpy";
            enthalpy.Attributes.Pivot = new PointF(600, 100);
            enthalpy.AddSource(psychrometrics.Params.Output[9]);   // enthalpy

            SAMPsychrometricsDewPointTemperature dewPoint = new SAMPsychrometricsDewPointTemperature();
            document.AddObject(dewPoint, false);
            dewPoint.CreateAttributes();
            dewPoint.Attributes.Pivot = new PointF(100, 400);
            ((Param_Number)dewPoint.Params.Input[0]).SetPersistentData(20.0);   // _dryBulbTemperature
            ((Param_Number)dewPoint.Params.Input[1]).SetPersistentData(50.0);   // _relativeHumidity
            ((Param_Number)dewPoint.Params.Input[2]).SetPersistentData(101325.0); // _pressure_

            Param_Number dewPointRecipient = new Param_Number();
            document.AddObject(dewPointRecipient, false);
            dewPointRecipient.CreateAttributes();
            dewPointRecipient.NickName = "dewPointTemperature";
            dewPointRecipient.Attributes.Pivot = new PointF(600, 400);
            dewPointRecipient.AddSource(dewPoint.Params.Output[0]);

            GH_DocumentIO io = new GH_DocumentIO(document);
            Assert.That(io.SaveQuiet(path), Is.True);
            TestContext.Progress.WriteLine("wrote " + path);
            TestContext.Progress.WriteLine(File.ReadAllText(path).Length + " bytes");
        }
    }
}
