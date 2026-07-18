// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using Grasshopper.Kernel;
using SAM.Core.Grasshopper.Psychrometrics.Properties;
using System;
using System.Collections.Generic;

namespace SAM.Core.Grasshopper.Psychrometrics
{
    public class SAMPsychrometricsDewPointTemperature : GH_SAMVariableOutputParameterComponent
    {
        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid => new Guid("e9bf471d-4ca5-4847-b5b9-bb3fbf712e41");

        /// <summary>
        /// The latest version of this component
        /// </summary>
        public override string LatestComponentVersion => "1.0.1";

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Resources.SAM_Psychrometrics;

        /// <summary>
        /// Initializes a new instance of the SAM_point3D class.
        /// </summary>
        public SAMPsychrometricsDewPointTemperature()
          : base("SAMPsychrometrics.DewPointTemperature", "SAMPsychrometrics.DewPointTemperature",
              "Calculates DewPointTemperature by Relative Humidity(0 - 100)[%] and Dry Bulb Temperature [°C] optionally Atmospheric Pressure [Pa] |101325 Pa ",
              "SAM", "Psychrometrics")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override GH_SAMParam[] Inputs
        {
            get
            {
                List<GH_SAMParam> result = new List<GH_SAMParam>();
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_Number() { Name = "_dryBulbTemperature", NickName = "_dryBulbTemperature", Description = "Dry Bulb Temperature [°C]", Access = GH_ParamAccess.item }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_Number() { Name = "_relativeHumidity", NickName = "_relativeHumidity", Description = "Relative Humidity (0 - 100) [%]", Access = GH_ParamAccess.item }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_Number() { Name = "_pressure_", NickName = "_pressure_", Description = "optional Atmospheric Pressure [Pa]", Access = GH_ParamAccess.item, Optional = true }, ParamVisibility.Binding));
                return result.ToArray();
            }
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override GH_SAMParam[] Outputs
        {
            get
            {
                List<GH_SAMParam> result = new List<GH_SAMParam>();
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_Number() { Name = "dewPointTemperature", NickName = "dewPointTemperature", Description = "Dew Point Temperature [°C]", Access = GH_ParamAccess.item }, ParamVisibility.Binding));
                return result.ToArray();
            }
        }

        protected override void SolveInstance(IGH_DataAccess dataAccess)
        {
            //double DewPointTemperature(double dryBulbTemperature, double relativeHumidity, double pressure)

            double dryBulbTemperature = double.NaN;
            double relativeHumidity = double.NaN;
            double pressure = double.NaN;

            int index = Params.IndexOfInputParam("_dryBulbTemperature");
            if (index == -1 || !dataAccess.GetData(index, ref dryBulbTemperature) || double.IsNaN(dryBulbTemperature))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            index = Params.IndexOfInputParam("_relativeHumidity");
            if (index == -1 || !dataAccess.GetData(index, ref relativeHumidity) || double.IsNaN(relativeHumidity))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            double dewPointTemperature = double.NaN;
            index = Params.IndexOfInputParam("_pressure_");
            if (index != -1 && dataAccess.GetData(index, ref pressure) && !double.IsNaN(pressure))
            {
                dewPointTemperature = Core.Psychrometrics.Query.DewPointTemperature(dryBulbTemperature, relativeHumidity, pressure);
            }
            else
            {
                dewPointTemperature = Core.Psychrometrics.Query.DewPointTemperature(dryBulbTemperature, relativeHumidity);
            }

            index = Params.IndexOfOutputParam("dewPointTemperature");
            if (index != -1)
            {
                dataAccess.SetData(index, dewPointTemperature);
            }
        }
    }
}
