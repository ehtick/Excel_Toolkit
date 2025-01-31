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

using BH.Engine.Excel;
using BH.oM.Adapters.Excel;
using BH.oM.Base;
using BH.oM.Data.Collections;
using ClosedXML.Excel;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BH.Adapter.Excel
{
    public partial class ExcelAdapter
    {
        /***************************************************/
        /**** Public Methods                            ****/
        /***************************************************/

        private bool Create(IXLWorkbook workbook, string sheetName, List<TableRow> data, ExcelPushConfig config)
        {
            if (data == null || data.Count == 0)
            {
                BH.Engine.Base.Compute.RecordError("Creation of a table failed: input table is null or does not contain data.");
                return false;
            }

            string workSheetName = Validation.WorksheetName(sheetName, workbook);

            try
            {
                IXLWorksheet worksheet = workbook.AddWorksheet(workSheetName);

                string startingCell = config?.StartingCell == null ? "A1" : config.StartingCell.ToExcel();
                if (string.IsNullOrWhiteSpace(startingCell))
                    return false;

                worksheet.Cell(startingCell).InsertData(data.Select(x => x.Content.ToArray()).ToList());
                return true;
            }
            catch (Exception e)
            {
                BH.Engine.Base.Compute.RecordError($"Creation of worksheet {sheetName} failed with the following error: {e.Message}");
                return false;
            }
        }
        /***************************************************/
    }
}