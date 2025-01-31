/*
 * This file is part of the Buildings and Habitats object Model (BHoM)
 * Copyright (c) 2015 - 2022, the respective contributors. All rights reserved.
 *
 * Each contributor holds copyright over their respective contributions.
 * The project versioning (Git) records all such contribution source information.
 *                                           
 *                                                                              
 * The BHoM is free software: you can redistribute it and/or modify         
 * it under the terms of the GNU Lesser General Public License as published by  
 * the Free Software Foundation, either version 3.0 of the License, or          
 * (at your option) any later version.                                          
 *                                                                              
 * The BHoM is distributed in the hope that it will be useful,              
 * but WITHOUT ANY WARRANTY; without even the implied warranty of               
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the                 
 * GNU Lesser General Public License for more details.                          
 *                                                                            
 * You should have received a copy of the GNU Lesser General Public License     
 * along with this code. If not, see <https://www.gnu.org/licenses/lgpl-3.0.html>.      
 */

using BH.Engine.Adapter;
using BH.Engine.Reflection;
using BH.oM.Adapter;
using BH.oM.Adapters.Excel;
using BH.oM.Base;
using BH.oM.Data.Collections;
using ClosedXML.Excel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BH.Adapter.Excel
{
    public partial class ExcelAdapter : BHoMAdapter
    {
        /***************************************************/
        /**** Public Overrides                          ****/
        /***************************************************/

        public override List<object> Push(IEnumerable<object> objects, string tag = "", PushType pushType = PushType.AdapterDefault, ActionConfig actionConfig = null)
        {
            if (objects == null || !objects.Any())
            {
                BH.Engine.Base.Compute.RecordError("No objects were provided for Push action.");
                return new List<object>();
            }
            objects = objects.Where(x => x != null).ToList();

            // If unset, set the pushType to AdapterSettings' value (base AdapterSettings default is FullCRUD).
            if (pushType == PushType.AdapterDefault)
                pushType = PushType.DeleteThenCreate;

            // Cast action config to ExcelPushConfig, create new if null.
            ExcelPushConfig config = actionConfig as ExcelPushConfig;
            if (config == null)
            {
                BH.Engine.Base.Compute.RecordNote($"{nameof(ExcelPushConfig)} has not been provided, default config is used.");
                config = new ExcelPushConfig();
            }

            // Make sure that a single type of objects are pushed
            List<Type> objectTypes = objects.Select(x => x.GetType()).Distinct().ToList();
            if (objectTypes.Count != 1)
            {
                string message = "The Excel adapter only allows to push objects of a single type to a table."
                    + "\nRight now you are providing objects of the following types: "
                    + objectTypes.Select(x => x.ToString()).Aggregate((a, b) => a + ", " + b);
                Engine.Base.Compute.RecordError(message);
                return new List<object>();
            }

            // Check if the workbook exists and create it if not.
            string fileName = m_FileSettings.GetFullFileName();
            XLWorkbook workbook;
            if (!File.Exists(fileName))
            {
                if (pushType == PushType.UpdateOnly)
                {
                    BH.Engine.Base.Compute.RecordError($"There is no workbook to update under {fileName}");
                    return new List<object>();
                }

                workbook = new XLWorkbook();
            }
            else
            {
                try
                {
                    workbook = new XLWorkbook(fileName);
                }
                catch (Exception e)
                {
                    BH.Engine.Base.Compute.RecordError($"The existing workbook could not be accessed due to the following error: {e.Message}");
                    return new List<object>();
                }
            }

            // Split the tables into collections to delete, create and update.
            bool success = true;
            string sheetName = config.Worksheet;

            List<TableRow> data = new List<TableRow>();
            if (objects.All(x => x is TableRow))
                data = objects.OfType<TableRow>().ToList();
            else
                data = ToTableRows(objects.ToList(), config.ObjectProperties);

            switch (pushType)
            {
                case PushType.CreateNonExisting:
                    {
                        if (workbook.Worksheets.All(x => x.Name != sheetName))
                            success &= Create(workbook, sheetName, data, config);
                        break;
                    }
                case PushType.DeleteThenCreate:
                    {
                        if (workbook.Worksheets.Any(x => x.Name == sheetName))
                            success &= Delete(workbook, sheetName);
                        success &= Create(workbook, sheetName, data, config);
                        break;
                    }
                case PushType.UpdateOnly:
                    {
                        success &= Update(workbook, sheetName, data, config);
                        break;
                    }
                case PushType.UpdateOrCreateOnly:
                    {
                        if (workbook.Worksheets.All(x => x.Name != sheetName))
                            success &= Create(workbook, sheetName, data, config);
                        else
                            success &= Update(workbook, sheetName, data, config);
                        break;
                    }
                default:
                    {
                        BH.Engine.Base.Compute.RecordError($"Currently Excel adapter does not supports {nameof(PushType)} equal to {pushType}");
                        return new List<object>();
                    }
            }

            // Try to update the workbook properties and then save it.
            try
            {
                Update(workbook, config.WorkbookProperties);
                workbook.SaveAs(fileName);
                return success ? objects.ToList() : new List<object>();
            }
            catch (Exception e)
            {
                BH.Engine.Base.Compute.RecordError($"Finalisation and saving of the workbook failed with the following error: {e.Message}");
                return new List<object>();
            }
        }

        /***************************************************/
        /**** Private Methods                           ****/
        /***************************************************/

        private List<TableRow> ToTableRows(List<object> objects, List<string> properties)
        {
            List<Dictionary<string, object>> content = objects.Where(x => x != null).Select(x => x.PropertyDictionary()).ToList();

            List<string> ignore = new List<string> { "Tags", "CustomData", "Fragments" };
            if (properties == null || properties.Count == 0)
                properties = content.SelectMany(x => x.Keys).Distinct().Where(x => !ignore.Contains(x)).ToList();

            List<TableRow> values = content
                .Select(dic => properties.Select(p => dic.ContainsKey(p) ? dic[p].ToString() : ""))
                .Select(x => new TableRow { Content = x.ToList<object>() })
                .ToList();

            return new List<TableRow> { new TableRow { Content = properties.ToList<object>() } }.Concat(values).ToList();
        }

        /***************************************************/
    }
}


