// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using global::Grasshopper.Kernel;
using global::Grasshopper.Kernel.Parameters;
using global::Grasshopper.Kernel.Special;
using global::Grasshopper.Kernel.Types;
using NUnit.Framework;
using SAM.Analytical.Grasshopper;

namespace SAM.Core.Grasshopper.Psychrometrics.Tests
{
    /// <summary>
    /// <b>The controlled rename pilot: <c>SAMPsychrometrics.Psychrometrics</c>'s <c>enthalpy</c> output is now
    /// <c>Enthalpy</c>, and an older definition is migrated safely.</b>
    /// <para>
    /// The migrated definition is the committed <c>Fixtures/Psychrometrics-Legacy.ghx</c>, saved by the
    /// pre-pilot build (component version 1.0.5): 20 °C / 50 %rh, an inserted voluntary <c>_pressure_</c> with
    /// a typed-in value, an <c>enthalpy</c> output wired to a number recipient, and a DewPoint component. The
    /// tests here drive the SAM updater over a copy of it and check that the rename keeps the wire, the
    /// persistent values and every other parameter, and that the upgraded definition saves, reopens and
    /// recomputes identically.
    /// </para>
    /// </summary>
    [TestFixture]
    public class ParameterRenameMigrationTests
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

        /// <summary>
        /// The declaration is the whole migration: exactly one rename, output <c>enthalpy</c> → <c>Enthalpy</c>,
        /// with nothing else changed.
        /// </summary>
        [Test]
        public void Component_DeclaresExactlyOneRename()
        {
            SAMPsychrometricsPsychrometrics component = new SAMPsychrometricsPsychrometrics();

            Assert.That(component.LatestComponentVersion, Is.EqualTo("1.0.7"));
            Assert.That(component.ParameterRenames, Is.Not.Null);
            Assert.That(component.ParameterRenames.Count, Is.EqualTo(1));

            ParameterRename rename = component.ParameterRenames[0];
            Assert.That(rename.Side, Is.EqualTo(GH_ParameterSide.Output));
            Assert.That(rename.LegacyName, Is.EqualTo("enthalpy"));
            Assert.That(rename.Name, Is.EqualTo("Enthalpy"));
        }

        /// <summary>
        /// An unmodified legacy definition still opens and computes before anything is updated: the declared
        /// lookup falls back to the name the file carries.
        /// </summary>
        [Test]
        public void LegacyDefinition_ComputesBeforeAnyUpdate()
        {
            GH_Document document = OpenFixture();
            SAMPsychrometricsPsychrometrics component = document.Objects.OfType<SAMPsychrometricsPsychrometrics>().Single();

            // the file's name wins on reopen, and the declared name still resolves through the fallback
            Assert.That(component.Params.Output[9].Name, Is.EqualTo("enthalpy"));
            Assert.That(component.Params.IndexOfOutputParam("Enthalpy"), Is.EqualTo(-1));
            Assert.That(component.IndexOfOutputParameter("Enthalpy"), Is.EqualTo(9));

            document.Enabled = true;
            document.NewSolution(false, GH_SolutionMode.Silent);
            Assert.That(Number(Recipient(document, "enthalpy")), Is.EqualTo(FreshEnthalpy()).Within(1e-9));
        }

        // ---- The upgrade -------------------------------------------------------------------------------

        [Test]
        public void Update_MigratesTheRenamedOutputAndKeepsValuesWiresAndGroup()
        {
            GH_Document document = OpenFixtureWithGroup();

            SAMPsychrometricsPsychrometrics component = document.Objects.OfType<SAMPsychrometricsPsychrometrics>().Single();
            GH_Group group = document.Objects.OfType<GH_Group>().Single();
            Guid instanceGuid_Old = component.InstanceGuid;
            Assert.That(group.ObjectIDs, Does.Contain(instanceGuid_Old));

            // the explicit updater sees the older published version
            Assert.That(component.ComponentVersion, Is.EqualTo("1.0.5"));
            Assert.That(component.Obsolete, Is.True);

            List<GH_SAMComponent> updated = Modify.UpdateComponents(new GH_SAMComponent[] { component }, out Log log, out List<ManualReconnectionIssue> issues);

            Assert.That(updated, Has.Count.EqualTo(1));
            SAMPsychrometricsPsychrometrics component_New = updated[0] as SAMPsychrometricsPsychrometrics;
            Assert.That(component_New, Is.Not.Null);
            Assert.That(issues, Is.Empty, string.Join("; ", issues.Select(x => x.ToString())));

            // the replacement is the live object in the document, under its own identity
            Assert.That(document.FindObject(component_New.InstanceGuid, true), Is.SameAs(component_New));
            Assert.That(component_New.Obsolete, Is.False);

            // and the component guid is unchanged
            Assert.That(component_New.ComponentGuid, Is.EqualTo(component.ComponentGuid));

            // renamed output, and no other output changed
            Assert.That(component_New.Params.Output.Select(x => x.Name), Does.Contain("Enthalpy"));
            Assert.That(component_New.Params.Output.Select(x => x.Name), Does.Not.Contain("enthalpy"));
            Assert.That(component_New.Params.Output.Count, Is.EqualTo(12));

            // the wire survived the rename and lands on the renamed output
            Param_Number enthalpyRecipient = Recipient(document, "enthalpy");
            Assert.That(enthalpyRecipient.Sources.Count, Is.EqualTo(1));
            Assert.That(enthalpyRecipient.Sources[0].Name, Is.EqualTo("Enthalpy"));

            // every typed-in value survived, including the voluntary _pressure_
            Assert.That(PersistentNumber(component_New, "_dryBulbTemperature"), Is.EqualTo(20.0));
            Assert.That(PersistentNumber(component_New, "_relativeHumidity_"), Is.EqualTo(50.0));
            Assert.That(PersistentNumber(component_New, "_pressure_"), Is.EqualTo(101325.0));

            // group membership moved to the replacement
            Assert.That(group.ObjectIDs, Does.Contain(component_New.InstanceGuid));
            Assert.That(group.ObjectIDs, Does.Not.Contain(instanceGuid_Old));

            // and it still computes the same number
            document.Enabled = true;
            document.NewSolution(false, GH_SolutionMode.Silent);
            Assert.That(Number(enthalpyRecipient), Is.EqualTo(FreshEnthalpy()).Within(1e-9));

            TestContext.Progress.WriteLine(log.ToString());
        }

        [Test]
        public void UpgradedDefinition_SavesReopensAndRecomputesIdentically()
        {
            GH_Document document = OpenFixtureWithGroup();
            SAMPsychrometricsPsychrometrics component = document.Objects.OfType<SAMPsychrometricsPsychrometrics>().Single();

            List<GH_SAMComponent> updated = Modify.UpdateComponents(new GH_SAMComponent[] { component }, out Log log, out List<ManualReconnectionIssue> issues);
            Assert.That(updated, Has.Count.EqualTo(1));
            SAMPsychrometricsPsychrometrics component_New = updated[0] as SAMPsychrometricsPsychrometrics;
            Assert.That(component_New, Is.Not.Null);

            string path = TempPath("upgraded.ghx");
            GH_DocumentIO io = new GH_DocumentIO(document);
            Assert.That(io.SaveQuiet(path), Is.True);

            GH_DocumentIO reopened = new GH_DocumentIO();
            Assert.That(reopened.Open(path), Is.True);
            SAMPsychrometricsPsychrometrics reloaded = reopened.Document.Objects.OfType<SAMPsychrometricsPsychrometrics>().Single();

            Assert.That(reloaded, Is.Not.Null);
            Assert.That(reloaded.Params.Output.Select(x => x.Name), Does.Contain("Enthalpy"));
            Assert.That(reloaded.Params.Output.Count, Is.EqualTo(12));
            Assert.That(reloaded.Params.IndexOfOutputParam("Enthalpy"), Is.EqualTo(9));

            Param_Number enthalpyRecipient = Recipient(reopened.Document, "enthalpy");
            Assert.That(enthalpyRecipient, Is.Not.Null);
            Assert.That(enthalpyRecipient.Sources.Count, Is.EqualTo(1));
            Assert.That(enthalpyRecipient.Sources[0].Name, Is.EqualTo("Enthalpy"));

            // the group survived the round trip, on the replacement's identity
            GH_Group group = reopened.Document.Objects.OfType<GH_Group>().Single();
            Assert.That(group.ObjectIDs, Does.Contain(reloaded.InstanceGuid));

            reopened.Document.Enabled = true;
            reopened.Document.NewSolution(false, GH_SolutionMode.Silent);
            Assert.That(Number(enthalpyRecipient), Is.EqualTo(FreshEnthalpy()).Within(1e-9));
        }

        [Test]
        public void Update_IsIdempotent()
        {
            GH_Document document = OpenFixtureWithGroup();
            SAMPsychrometricsPsychrometrics component = document.Objects.OfType<SAMPsychrometricsPsychrometrics>().Single();

            List<GH_SAMComponent> updated = Modify.UpdateComponents(new GH_SAMComponent[] { component }, out Log log, out List<ManualReconnectionIssue> issues);
            Assert.That(updated, Has.Count.EqualTo(1));
            SAMPsychrometricsPsychrometrics component_New = updated[0] as SAMPsychrometricsPsychrometrics;

            List<GH_SAMComponent> updated_Again = Modify.UpdateComponents(new GH_SAMComponent[] { component_New }, out Log log_Again, out List<ManualReconnectionIssue> issues_Again);

            Assert.That(updated_Again, Is.Null);
            Assert.That(component_New.Obsolete, Is.False);
            Assert.That(Recipient(document, "enthalpy").Sources[0], Is.EqualTo(component_New.Params.Output[9]));
        }

        /// <summary>
        /// A name the component does not declare a rename for is genuinely gone: it is reported as dropped,
        /// not silently rerouted to some other output.
        /// </summary>
        [Test]
        public void UndeclaredLegacyName_IsReportedAndNotFabricated()
        {
            GH_Document document = OpenFixture();
            SAMPsychrometricsPsychrometrics component = document.Objects.OfType<SAMPsychrometricsPsychrometrics>().Single();

            // stand in for a definition saved under a name this version neither declares nor maps
            component.Params.Output[11].Name = "legacyDensity";
            component.Params.Output[11].NickName = "legacyDensity";

            Param_Number legacyRecipient = new Param_Number();
            document.AddObject(legacyRecipient, false);
            legacyRecipient.CreateAttributes();
            legacyRecipient.NickName = "legacyDensity";
            legacyRecipient.AddSource(component.Params.Output[11]);

            List<GH_SAMComponent> updated = Modify.UpdateComponents(new GH_SAMComponent[] { component }, out Log log, out List<ManualReconnectionIssue> issues);

            Assert.That(updated, Has.Count.EqualTo(1));
            SAMPsychrometricsPsychrometrics component_New = updated[0] as SAMPsychrometricsPsychrometrics;
            Assert.That(component_New.Params.Output.Select(x => x.Name), Does.Not.Contain("legacyDensity"));
            Assert.That(component_New.Params.Output.Select(x => x.Name), Does.Contain("density"));

            Assert.That(issues.Count, Is.EqualTo(1));
            Assert.That(issues[0].MissingOutputNames, Does.Contain("legacyDensity"));
            Assert.That(log.Select(x => x.Text), Has.Some.Contains("legacyDensity"));
        }

        // ---- Helpers -----------------------------------------------------------------------------------

        private static GH_Document OpenFixture()
        {
            GH_DocumentIO io = new GH_DocumentIO();
            Assert.That(io.Open(FixturePath), Is.True, "could not open the fixture definition");
            Assert.That(io.Document, Is.Not.Null);
            return io.Document;
        }

        /// <summary>Opens the committed legacy definition and adds a group around its Psychrometrics
        /// component, saving and reopening so the group is exercised through serialization exactly as a
        /// grouped definition would be.</summary>
        private static GH_Document OpenFixtureWithGroup()
        {
            GH_Document document = OpenFixture();
            SAMPsychrometricsPsychrometrics component = document.Objects.OfType<SAMPsychrometricsPsychrometrics>().Single();

            GH_Group group = new GH_Group();
            group.CreateAttributes();
            group.NickName = "Psychrometrics group";
            document.AddObject(group, false);
            group.AddObject(component.InstanceGuid);

            string path = TempPath("legacy-with-group.ghx");
            GH_DocumentIO io = new GH_DocumentIO(document);
            Assert.That(io.SaveQuiet(path), Is.True);

            GH_DocumentIO reopened = new GH_DocumentIO();
            Assert.That(reopened.Open(path), Is.True);
            Assert.That(reopened.Document.Objects.OfType<GH_Group>().Count(), Is.EqualTo(1));
            return reopened.Document;
        }

        private static string TempPath(string fileName)
        {
            string dir = Path.Combine(Path.GetTempPath(), "sam-psychrometrics-rename");
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, fileName);
        }

        private static Param_Number Recipient(GH_Document document, string nickname)
        {
            return document.Objects.OfType<Param_Number>().FirstOrDefault(p => p.NickName == nickname);
        }

        private static double Number(Param_Number param)
        {
            Assert.That(param, Is.Not.Null, "recipient not found");
            GH_Number number = param.VolatileData.AllData(true).OfType<GH_Number>().FirstOrDefault();
            Assert.That(number, Is.Not.Null, param.NickName + " produced no number");
            return number.Value;
        }

        private static double? PersistentNumber(IGH_Component component, string name)
        {
            Param_Number param = component.Params.Input.OfType<Param_Number>().FirstOrDefault(p => p.Name == name);
            if (param == null)
            {
                return null;
            }

            GH_Number number = param.PersistentData.AllData(true).OfType<GH_Number>().FirstOrDefault();
            return number?.Value;
        }

        private static double FreshEnthalpy()
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
            enthalpy.AddSource(component.Params.Output[9]); // Enthalpy

            document.Enabled = true;
            document.NewSolution(false, GH_SolutionMode.Silent);
            return Number(enthalpy);
        }
    }
}
