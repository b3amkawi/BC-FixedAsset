using ClosedXML.Excel;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;

namespace BC.FixedAsset.Web.FixedAsset.Survey
{
    internal sealed class ExcelSurveyExporter
    {
        private static readonly string[] Headers = { "Photo 1 (ACTUAL)", "Photo 2 (SERIAL)", "Photo 3 (OTHER)", "Survey No", "Survey Date", "Status", "Fixed Asset No", "Asset Name", " W x L x H", "Weight", "Category", "Department", "Custodian", "Condition", "Qty", "UOM", "Building", "Floor", "Room", "Brand", "Model", "Serial Number", "Ownership", "Received Date", "PO No.", "Estimated Value", "Surveyor", "Remark", "Survey ID", "Created UTC", "Modified UTC" };
        private readonly DataTable surveys;
        private readonly DataTable attachments;
        private readonly string uploadRoot;

        public ExcelSurveyExporter(DataSet data,string uploadRoot)
        {
            surveys=data.Tables["Surveys"];
            attachments=data.Tables["Attachments"];
            this.uploadRoot=uploadRoot;
        }

        public byte[] Build(DateTimeOffset exportedAt)
        {
            using(var workbook=new XLWorkbook())
            {
                workbook.Author="BC Fixed Asset";
                BuildSummary(workbook,exportedAt);
                BuildSurveys(workbook,exportedAt);
                workbook.Worksheets.Add("ชีต1");
                using(var output=new MemoryStream()){workbook.SaveAs(output);return output.ToArray();}
            }
        }

        private void BuildSummary(XLWorkbook workbook,DateTimeOffset exportedAt)
        {
            var sheet=workbook.Worksheets.Add("Summary");sheet.ShowGridLines=false;
            sheet.Cell("A2").Value="Asset Survey Export";sheet.Range("A2:F2").Merge();
            sheet.Cell("A3").Value="ข้อมูลจากระบบ BCFixedAsset ณ "+exportedAt.ToString("yyyy-MM-dd HH:mm:ss zzz",CultureInfo.InvariantCulture);sheet.Range("A3:F3").Merge();
            sheet.Range("A2:F2").Style.Fill.BackgroundColor=XLColor.FromHtml("#0B3B70");sheet.Range("A2:F2").Style.Font.FontColor=XLColor.White;sheet.Range("A2:F2").Style.Font.Bold=true;sheet.Range("A2:F2").Style.Font.FontSize=20;
            sheet.Range("A3:F3").Style.Fill.BackgroundColor=XLColor.FromHtml("#DCE8F5");sheet.Range("A3:F3").Style.Font.FontColor=XLColor.FromHtml("#36546F");
            sheet.Row(2).Height=30;sheet.Row(3).Height=22;
            var withPhotos=attachments.AsEnumerable().Select(r=>r.Field<long>("SurveyId")).Distinct().Count();
            var summary=new[]{Tuple.Create("รายการสำรวจทั้งหมด",surveys.Rows.Count),Tuple.Create("รายการที่มีรูป",withPhotos),Tuple.Create("รายการที่ไม่มีรูป",surveys.Rows.Count-withPhotos),Tuple.Create("รูปทั้งหมด",attachments.Rows.Count)};
            sheet.Cell("A6").Value="รายการ";sheet.Cell("B6").Value="จำนวน";sheet.Cell("D6").Value="อาคาร";sheet.Cell("E6").Value="จำนวน";
            StyleSummaryHeader(sheet.Range("A6:B6"));StyleSummaryHeader(sheet.Range("D6:E6"));
            var row=7;foreach(var item in summary){sheet.Cell(row,1).Value=item.Item1;sheet.Cell(row,2).Value=item.Item2;StyleSummaryRow(sheet.Range(row,1,row,2));row++;}
            var buildingRow=7;foreach(var group in surveys.AsEnumerable().GroupBy(r=>Text(r,"BuildingName")).OrderBy(g=>g.Key))
            {if(string.IsNullOrWhiteSpace(group.Key))continue;sheet.Cell(buildingRow,4).Value=group.Key;sheet.Cell(buildingRow,5).Value=group.Count();StyleSummaryRow(sheet.Range(buildingRow,4,buildingRow,5));buildingRow++;}
            row=10;foreach(var group in surveys.AsEnumerable().GroupBy(r=>Text(r,"Status")).OrderBy(g=>g.Key)){sheet.Cell(row,1).Value="สถานะ "+Status(group.Key);sheet.Cell(row,2).Value=group.Count();StyleSummaryRow(sheet.Range(row,1,row,2));row++;}
            sheet.Column(1).Width=29;sheet.Column(2).Width=14;sheet.Column(3).Width=4;sheet.Column(4).Width=18;sheet.Column(5).Width=14;sheet.Column(6).Width=8.63;
            sheet.SheetView.FreezeRows(3);
        }

        private void BuildSurveys(XLWorkbook workbook,DateTimeOffset exportedAt)
        {
            var sheet=workbook.Worksheets.Add("Asset Surveys");sheet.ShowGridLines=false;
            sheet.Cell("A2").Value="Asset Survey Data พร้อมรูป";sheet.Range("A2:AE2").Merge();
            sheet.Cell("A3").Value=string.Format("ส่งออก {0:N0} รายการจาก BCFixedAsset เมื่อ {1}",surveys.Rows.Count,exportedAt.ToString("yyyy-MM-dd HH:mm:ss zzz",CultureInfo.InvariantCulture));sheet.Range("A3:AE3").Merge();
            sheet.Range("A2:AE2").Style.Fill.BackgroundColor=XLColor.FromHtml("#0B3B70");sheet.Range("A2:AE2").Style.Font.FontColor=XLColor.White;sheet.Range("A2:AE2").Style.Font.Bold=true;sheet.Range("A2:AE2").Style.Font.FontSize=18;
            sheet.Range("A3:AE3").Style.Fill.BackgroundColor=XLColor.FromHtml("#DCE8F5");sheet.Range("A3:AE3").Style.Font.FontColor=XLColor.FromHtml("#36546F");
            sheet.Row(2).Height=28;sheet.Row(3).Height=22;
            for(var column=1;column<=Headers.Length;column++)sheet.Cell(5,column).Value=Headers[column-1];
            var header=sheet.Range(5,1,5,Headers.Length);header.Style.Fill.BackgroundColor=XLColor.FromHtml("#15569A");header.Style.Font.FontColor=XLColor.White;header.Style.Font.Bold=true;header.Style.Alignment.Horizontal=XLAlignmentHorizontalValues.Center;header.Style.Alignment.Vertical=XLAlignmentVerticalValues.Center;header.Style.Alignment.WrapText=true;header.Style.Border.BottomBorder=XLBorderStyleValues.Thin;header.Style.Border.BottomBorderColor=XLColor.FromHtml("#0B3B70");sheet.Row(5).Height=28.5;
            var bySurvey=attachments.AsEnumerable().GroupBy(r=>r.Field<long>("SurveyId")).ToDictionary(g=>g.Key,g=>g.ToList());
            var rowNumber=6;
            foreach(DataRow survey in surveys.Rows)
            {
                Set(sheet.Cell(rowNumber,4),survey["SurveyNo"]);SetDate(sheet.Cell(rowNumber,5),survey["SurveyDate"]);Set(sheet.Cell(rowNumber,6),Status(Text(survey,"Status")));Set(sheet.Cell(rowNumber,7),survey["FixedAssetNo"]);Set(sheet.Cell(rowNumber,8),survey["AssetName"]);
                sheet.Cell(rowNumber,9).Value=Dimensions(survey);sheet.Cell(rowNumber,10).Value=Measure(survey["WeightKg"]," kg");sheet.Cell(rowNumber,11).Value=CodeName(survey,"CategoryCode","CategoryName");sheet.Cell(rowNumber,12).Value=CodeName(survey,"DepartmentCode","DepartmentName");
                Set(sheet.Cell(rowNumber,13),survey["Custodian"]);Set(sheet.Cell(rowNumber,14),survey["ConditionName"]);SetNumber(sheet.Cell(rowNumber,15),survey["Quantity"]);Set(sheet.Cell(rowNumber,16),survey["UomCode"]);Set(sheet.Cell(rowNumber,17),survey["BuildingName"]);Set(sheet.Cell(rowNumber,18),survey["FloorName"]);Set(sheet.Cell(rowNumber,19),survey["RoomName"]);
                Set(sheet.Cell(rowNumber,20),survey["Brand"]);Set(sheet.Cell(rowNumber,21),survey["ModelDescription"]);Set(sheet.Cell(rowNumber,22),survey["SerialNumber"]);Set(sheet.Cell(rowNumber,23),survey["OwnershipType"]);SetDate(sheet.Cell(rowNumber,24),survey["ReceivedDate"]);Set(sheet.Cell(rowNumber,25),survey["PurchaseOrderNo"]);SetNumber(sheet.Cell(rowNumber,26),survey["EstimatedValue"]);Set(sheet.Cell(rowNumber,27),survey["Surveyor"]);Set(sheet.Cell(rowNumber,28),survey["Remark"]);SetNumber(sheet.Cell(rowNumber,29),survey["SurveyId"]);SetDateTime(sheet.Cell(rowNumber,30),survey["CreatedUtc"]);SetDateTime(sheet.Cell(rowNumber,31),survey["ModifiedUtc"]);
                var surveyId=Convert.ToInt64(survey["SurveyId"]);List<DataRow> photos;if(bySurvey.TryGetValue(surveyId,out photos))foreach(var photo in photos.GroupBy(p=>Text(p,"AttachmentType")).Select(g=>g.First()))AddPhoto(sheet,rowNumber,PhotoColumn(Text(photo,"AttachmentType")),photo);
                var dataRange=sheet.Range(rowNumber,1,rowNumber,Headers.Length);dataRange.Style.Alignment.Vertical=XLAlignmentVerticalValues.Center;dataRange.Style.Alignment.WrapText=true;dataRange.Style.Border.BottomBorder=XLBorderStyleValues.Hair;dataRange.Style.Border.BottomBorderColor=XLColor.FromHtml("#D9E2EC");
                if(rowNumber%2==1)dataRange.Style.Fill.BackgroundColor=XLColor.FromHtml("#F7FAFD");sheet.Row(rowNumber).Height=64.5;rowNumber++;
            }
            SetWidths(sheet);sheet.SheetView.Freeze(5,4);sheet.Range(5,1,Math.Max(5,rowNumber-1),Headers.Length).SetAutoFilter();
            sheet.PageSetup.PageOrientation=XLPageOrientation.Landscape;sheet.PageSetup.FitToPages(1,0);sheet.PageSetup.Margins.Left=0.7;sheet.PageSetup.Margins.Right=0.7;
        }

        private void AddPhoto(IXLWorksheet sheet,int row,int column,DataRow photo)
        {
            if(column==0)return;byte[] bytes=null;if(photo["FileContent"]!=DBNull.Value)bytes=(byte[])photo["FileContent"];
            if((bytes==null||bytes.Length==0)&&!string.IsNullOrWhiteSpace(uploadRoot)){var relative=Text(photo,"RelativePath").Replace('/',Path.DirectorySeparatorChar);var root=Path.GetFullPath(uploadRoot);var path=Path.GetFullPath(Path.Combine(root,relative));if(path.StartsWith(root,StringComparison.OrdinalIgnoreCase)&&File.Exists(path))bytes=File.ReadAllBytes(path);}
            if(bytes==null||bytes.Length==0)return;
            try{using(var stream=new MemoryStream(bytes)){var picture=sheet.AddPicture(stream);picture.Name="survey_"+Text(photo,"AttachmentId");picture.MoveTo(sheet.Cell(row,column)).WithSize(90,70);}}
            catch(ArgumentException){ }
        }

        private static void SetWidths(IXLWorksheet sheet){double[] widths={15,11.5,11.5,20.88,18,16,18,38.38,28,12,23,24,20,17,10,12,14,10,24,15,20,18,14,13,16,16,20,48,10,20,20};for(var i=0;i<widths.Length;i++)sheet.Column(i+1).Width=widths[i];}
        private static void StyleSummaryHeader(IXLRange range){range.Style.Fill.BackgroundColor=XLColor.FromHtml("#15569A");range.Style.Font.FontColor=XLColor.White;range.Style.Font.Bold=true;range.Style.Alignment.Horizontal=XLAlignmentHorizontalValues.Center;}
        private static void StyleSummaryRow(IXLRange range){range.Style.Fill.BackgroundColor=XLColor.FromHtml("#F1F6FB");range.Style.Border.BottomBorder=XLBorderStyleValues.Thin;range.Style.Border.BottomBorderColor=XLColor.FromHtml("#D9E2EC");}
        private static int PhotoColumn(string type){return type=="ACTUAL"?1:type=="SERIAL"?2:type=="OTHER"?3:0;}
        private static string Status(string value){return value=="ManagerReview"?"Manager Review":value=="FinanceReview"?"Finance Review":value;}
        private static string CodeName(DataRow row,string code,string name){var c=Text(row,code);var n=Text(row,name);return string.IsNullOrWhiteSpace(c)?n:string.IsNullOrWhiteSpace(n)?c:c+" - "+n;}
        private static string Dimensions(DataRow row){var parts=new[]{Value(row["WidthCm"]),Value(row["LengthCm"]),Value(row["HeightCm"])};return parts.All(string.IsNullOrEmpty)?string.Empty:string.Join(" x ",parts.Select(x=>string.IsNullOrEmpty(x)?"-":x))+" cm";}
        private static string Measure(object value,string suffix){var text=Value(value);return text.Length==0?string.Empty:text+suffix;}
        private static string Value(object value){return value==null||value==DBNull.Value?string.Empty:Convert.ToDecimal(value).ToString("0.##",CultureInfo.InvariantCulture);}
        private static string Text(DataRow row,string column){var value=row[column];return value==DBNull.Value?string.Empty:Convert.ToString(value);}
        private static void Set(IXLCell cell,object value){if(value!=null&&value!=DBNull.Value)cell.Value=Convert.ToString(value);}
        private static void SetNumber(IXLCell cell,object value){if(value!=null&&value!=DBNull.Value)cell.Value=Convert.ToDecimal(value);}
        private static void SetDate(IXLCell cell,object value){if(value==null||value==DBNull.Value)return;cell.Value=Convert.ToDateTime(value);cell.Style.DateFormat.Format="yyyy-mm-dd";}
        private static void SetDateTime(IXLCell cell,object value){if(value==null||value==DBNull.Value)return;cell.Value=Convert.ToDateTime(value);cell.Style.DateFormat.Format="yyyy-mm-dd hh:mm:ss";}
    }
}
