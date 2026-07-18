// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using Grasshopper.Kernel;
using SAM.Core.Grasshopper.Psychrometrics.Properties;
using System;
using System.Collections.Generic;

namespace SAM.Core.Grasshopper.Psychrometrics
{
    public class SAMPsychrometricsDryBulbTemperature : GH_SAMVariableOutputParameterComponent
    {
        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid => new Guid("7a795583-cabe-452e-864f-bce79135fd5c");

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
        public SAMPsychrometricsDryBulbTemperature()
          : base("SAMPsychrometrics.DryBulbTemperature", "SAMPsychrometrics.DryBulbTemperature",
              "Calculates dry bulb temperature [°C]",
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
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_Number() { Name = "_density", NickName = "_density", Description = "Density [kg/m3]", Access = GH_ParamAccess.item }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_Number() { Name = "_humidityRatio_", NickName = "_humidityRatio_", Description = "Humidty Ratio [kg/kg]", Access = GH_ParamAccess.item, Optional = true }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_Number() { Name = "_relativeHumidity_", NickName = "_relativeHumidity_", Description = "Relative Humidity (0 - 100) [%]", Access = GH_ParamAccess.item, Optional = true }, ParamVisibility.Binding));

                global::Grasshopper.Kernel.Parameters.Param_Number param_Number = new global::Grasshopper.Kernel.Parameters.Param_Number() { Name = "_pressure_", NickName = "_pressure_", Description = "Atmospheric pressure [Pa]", Access = GH_ParamAccess.item, Optional = true };
                param_Number.SetPersistentData(101325);
                result.Add(new GH_SAMParam(param_Number, ParamVisibility.Binding));

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
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_Number() { Name = "dryBulbTemperature", NickName = "dryBulbTemperature", Description = "Dry Bulb Temperature [°C]", Access = GH_ParamAccess.item }, ParamVisibility.Binding));
                return result.ToArray();
            }
        }

        protected override void SolveInstance(IGH_DataAccess dataAccess)
        {
            int index = -1;

            double density = double.NaN;
            double humidityRatio = double.NaN;
            double relativeHumidity = double.NaN;
            double pressure = double.NaN;

            index = Params.IndexOfInputParam("_density");
            if (index == -1 || !dataAccess.GetData(index, ref density) || double.IsNaN(density))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            index = Params.IndexOfInputParam("_humidityRatio_");
            if (index != -1)
            {
                dataAccess.GetData(index, ref humidityRatio);
            }

            index = Params.IndexOfInputParam("_relativeHumidity_");
            if (index != -1)
            {
                dataAccess.GetData(index, ref relativeHumidity);
            }

            if(double.IsNaN(relativeHumidity) && double.IsNaN(humidityRatio))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            index = Params.IndexOfInputParam("_pressure_");
            if (index == -1 || !dataAccess.GetData(index, ref pressure) || double.IsNaN(pressure))
            {
                pressure = 101325;
            }

            double dryBulbTemperature = double.NaN;
            if (!double.IsNaN(relativeHumidity))
            {
                dryBulbTemperature = Core.Psychrometrics.Query.DryBulbTemperature_ByDensityAndRelativeHumidity(density, relativeHumidity, pressure);
            }
            else
            {
                dryBulbTemperature = Core.Psychrometrics.Query.DryBulbTemperature_ByDensityAndHumidityRatio(density, humidityRatio, pressure);
            }

            index = Params.IndexOfOutputParam("dryBulbTemperature");
            if (index != -1)
            {
                dataAccess.SetData(index, dryBulbTemperature);
            }
        }
    }
}
