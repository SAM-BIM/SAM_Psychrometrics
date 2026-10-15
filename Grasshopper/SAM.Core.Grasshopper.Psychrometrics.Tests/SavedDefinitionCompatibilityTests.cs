// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using global::Grasshopper.Kernel;
using global::Grasshopper.Kernel.Parameters;
using global::Grasshopper.Kernel.Types;
using NUnit.Framework;
using SAM.Analytical.Grasshopper;

namespace SAM.Core.Grasshopper.Psychrometrics.Tests
{
    /// <summary>
    /// <b>A definition saved by the previous release still opens, keeps its wires and typed-in values, and
    /// recomputes the same numbers.</b>
    /// <para>
    /// <c>Fixtures/Psychrometrics-Legacy.ghx</c> was saved by the pre-pilot build (component versions
    /// 1.0.5 / 1.0.1 / 1.0.1): a <c>SAMPsychrometrics.Psychrometrics</c> with 20 °C / 50 %rh and an inserted
    /// voluntary <c>_pressure_</c>, a <c>SAMPsychrometrics.DewPointTemperature</c>, and two wired number
    /// recipients. Reopening it in a real Rhino 8 / Grasshopper and solving it is the backward-compatibility
    /// acceptance test for this pilot.
    /// </para>
    /// </summary>
    [TestFixture]
    public class SavedDefinitionCompatibilityTests
    {
        private static string FixturePath
        {
            get
            {
                string path = Path.Combine(TestContext.CurrentContext.TestDirectory, "Fixtures", "Psychrometrics-Legacy.ghx");
                Assert.That(File.Exists(path), Is.True, "fixture not found: " + path);
                return path;
            }
        }

        [Test]
        public void SavedDefinition_ReopensWithItsStructureWiresAndValues()
        {
            GH_Document document = OpenFixture();

            SAMPsychrometricsPsychrometrics psychrometrics = document.Objects.OfType<SAMPsychrometricsPsychrometrics>().SingleOrDefault();
            SAMPsychrometricsDewPointTemperature dewPoint = document.Objects.OfType<SAMPsychrometricsDewPointTemperature>().SingleOrDefault();
            Assert.That(psychrometrics, Is.Not.Null, "SAMPsychrometrics.Psychrometrics did not reload");
            Assert.That(dewPoint, Is.Not.Null, "SAMPsychrometrics.DewPointTemperature did not reload");

            // the voluntary parameter the person inserted, with its typed-in value, survived
            Assert.That(psychrometrics.Params.Input.Select(p => p.Name), Is.EqualTo(new[]
            {
                "_dryBulbTemperature", "_relativeHumidity_", "_humidityRatio_", "_wetBulbTemperature_", "_dewPointTemperature_", "_pressure_"
            }));
            Assert.That(PersistentNumber(psychrometrics, "_dryBulbTemperature"), Is.EqualTo(20.0));
            Assert.That(PersistentNumber(psychrometrics, "_relativeHumidity_"), Is.EqualTo(50.0));
            Assert.That(PersistentNumber(psychrometrics, "_pressure_"), Is.EqualTo(101325.0));

            Assert.That(psychrometrics.Params.Output.Count, Is.EqualTo(12));
            Assert.That(dewPoint.Params.Input.Select(p => p.Name), Is.EqualTo(new[] { "_dryBulbTemperature", "_relativeHumidity", "_pressure_" }));

            // the wires are intact and attached to the outputs they were attached to
            Param_Number enthalpy = Recipient(document, "enthalpy");
            Param_Number dewPointRecipient = Recipient(document, "dewPointTemperature");
            Assert.That(enthalpy.Sources.Count, Is.EqualTo(1));
            Assert.That(enthalpy.Sources[0].Name, Is.EqualTo("enthalpy"));
            Assert.That(dewPointRecipient.Sources.Count, Is.EqualTo(1));
            Assert.That(dewPointRecipient.Sources[0].Name, Is.EqualTo("dewPointTemperature"));

            // and the reopened definition finds every parameter by the name its code looks up
            foreach (IGH_Param param in psychrometrics.Params.Input)
            {
                Assert.That(psychrometrics.Params.IndexOfInputParam(param.Name), Is.GreaterThanOrEqualTo(0), param.Name);
            }
            foreach (IGH_Param param in psychrometrics.Params.Output)
            {
                Assert.That(psychrometrics.Params.IndexOfOutputParam(param.Name), Is.GreaterThanOrEqualTo(0), param.Name);
            }
        }

        [Test]
        public void SavedDefinition_RecomputesTheSameNumbers()
        {
            GH_Document document = OpenFixture();
            document.Enabled = true;
            document.NewSolution(false, GH_SolutionMode.Silent);

            Param_Number enthalpy = Recipient(document, "enthalpy");
            Param_Number dewPoint = Recipient(document, "dewPointTemperature");
            SAMPsychrometricsPsychrometrics psychrometrics = document.Objects.OfType<SAMPsychrometricsPsychrometrics>().Single();
            SAMPsychrometricsDewPointTemperature dewPointComponent = document.Objects.OfType<SAMPsychrometricsDewPointTemperature>().Single();

            double enthalpyValue = Number(enthalpy);
            double dewPointValue = Number(dewPoint);

            // a freshly built component with the same inputs, as the unchanged-algorithm reference
            Assert.That(enthalpyValue, Is.EqualTo(FreshPsychrometricsEnthalpy()).Within(1e-9));
            Assert.That(dewPointValue, Is.EqualTo(Core.Psychrometrics.Query.DewPointTemperature(20.0, 50.0, 101325.0)).Within(1e-9));

            TestContext.Progress.WriteLine("reopened definition enthalpy = " + enthalpyValue);
            TestContext.Progress.WriteLine("reopened definition dewPointTemperature = " + dewPointValue);
        }

        /// <summary>
        /// <b>Why the parameter names above are frozen.</b>
        /// <para>
        /// Grasshopper restores a variable-parameter component from the parameter archive stored in the
        /// definition, so a reopened component keeps the names the FILE has, not the names the class now
        /// declares. After a rename, <c>Params.IndexOfInputParam("&lt;new name&gt;")</c> - every lookup in
        /// <c>SolveInstance</c> - returns -1 for every existing definition, with no compile error to warn
        /// anyone. This test reproduces that mechanism, so the constraint is recorded as behaviour rather
        /// than as a comment.
        /// </para>
        /// </summary>
        [Test]
        public void AChangedDeclaredNameNoLongerResolvesInAnExistingDefinition()
        {
            string dir = Path.Combine(Path.GetTempPath(), "sam-psychrometrics-metadata");
            Directory.CreateDirectory(dir);
            string original = Path.Combine(dir, "names-original.ghx");
            string changed = Path.Combine(dir, "names-changed.ghx");

            GH_Document document = new GH_Document();
            SAMPsychrometricsDewPointTemperature component = new SAMPsychrometricsDewPointTemperature();
            document.AddObject(component, false);
            component.CreateAttributes();
            GH_DocumentIO io = new GH_DocumentIO(document);
            Assert.That(io.SaveQuiet(original), Is.True);

            string xml = File.ReadAllText(original);
            foreach (IGH_Param p in component.Params.Input.Concat(component.Params.Output))
            {
                xml = xml.Replace(">" + p.Name + "<", ">CHANGED_" + p.Name + "<");
            }
            File.WriteAllText(changed, xml);

            GH_DocumentIO reopened = new GH_DocumentIO();
            Assert.That(reopened.Open(changed), Is.True);
            SAMPsychrometricsDewPointTemperature reloaded = reopened.Document.Objects.OfType<SAMPsychrometricsDewPointTemperature>().Single();

            Assert.That(reloaded.Params.Input.Count, Is.EqualTo(3), "parameter count must not change");
            Assert.That(reloaded.Params.Input[0].GetType(), Is.EqualTo(typeof(Param_Number)), "parameter type must not change");
            Assert.That(reloaded.Params.Input[0].Name, Is.EqualTo("CHANGED__dryBulbTemperature"), "the file's name wins");
            Assert.That(reloaded.Params.IndexOfInputParam("_dryBulbTemperature"), Is.EqualTo(-1), "the declared name no longer resolves");
        }

        private static GH_Document OpenFixture()
        {
            GH_DocumentIO io = new GH_DocumentIO();
            Assert.That(io.Open(FixturePath), Is.True, "could not open the fixture definition");
            Assert.That(io.Document, Is.Not.Null);
            return io.Document;
        }

        private static Param_Number Recipient(GH_Document document, string nickname)
        {
            return document.Objects.OfType<Param_Number>().FirstOrDefault(p => p.NickName == nickname);
        }

        private static double Number(Param_Number param)
        {
            GH_Number number = param.VolatileData.AllData(true).OfType<GH_Number>().FirstOrDefault();
            Assert.That(number, Is.Not.Null, param.NickName + " produced no number");
            return number.Value;
        }

        private static double? PersistentNumber(IGH_Component component, string name)
        {
            Param_Number param = component.Params.Input.OfType<Param_Number>().FirstOrDefault(p => p.Name == name);
            if (param == null) return null;
            GH_Number number = param.PersistentData.AllData(true).OfType<GH_Number>().FirstOrDefault();
            return number?.Value;
        }

        private static double FreshPsychrometricsEnthalpy()
        {
            GH_Document document = new GH_Document();
            SAMPsychrometricsPsychrometrics component = new SAMPsychrometricsPsychrometrics();
            document.AddObject(component, false);
            component.CreateAttributes();

            IGH_Param pressure = component.CreateParameter(GH_ParameterSide.Input, 5);
            component.Params.RegisterInputParam(pressure, 5);
            component.Params.OnParametersChanged();

            ((Param_Number)component.Params.Input[0]).SetPersistentData(20.0);
            ((Param_Number)component.Params.Input[1]).SetPersistentData(50.0);
            ((Param_Number)component.Params.Input[5]).SetPersistentData(101325.0);

            Param_Number enthalpy = new Param_Number();
            document.AddObject(enthalpy, false);
            enthalpy.CreateAttributes();
            enthalpy.AddSource(component.Params.Output[9]);

            document.Enabled = true;
            document.NewSolution(false, GH_SolutionMode.Silent);
            return Number(enthalpy);
        }
    }
}
