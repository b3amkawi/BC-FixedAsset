using BC.FixedAsset.Core.Models;
using BC.FixedAsset.Services;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml.Linq;

namespace BC.FixedAsset.Web.FixedAsset.Survey
{
    internal sealed class ExcelImportResult { public int Imported; public int Skipped; }

    internal sealed class ExcelSurveyImporter
    {
        private readonly AssetSurveyService service; private readonly SurveyAccessContext access;
        private static readonly XNamespace Main="http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        private static readonly XNamespace Rel="http://schemas.openxmlformats.org/officeDocument/2006/relationships";
        private static readonly XNamespace PkgRel="http://schemas.openxmlformats.org/package/2006/relationships";
        public ExcelSurveyImporter(AssetSurveyService service, SurveyAccessContext access){this.service=service;this.access=access;}

        public ExcelImportResult Import(HttpPostedFile file)
        {
            var ext=Path.GetExtension(file.FileName).ToLowerInvariant();
            if(ext!=".xlsx"&&ext!=".xlsm")throw new ArgumentException("รองรับเฉพาะไฟล์ .xlsx และ .xlsm");
            if(file.ContentLength<=0||file.ContentLength>25*1024*1024)throw new ArgumentException("ไฟล์ Excel ต้องมีขนาดไม่เกิน 25 MB");
            List<Dictionary<string,string>> rows;
            try{using(var zip=new ZipArchive(file.InputStream,ZipArchiveMode.Read,true))rows=ReadRows(zip);}
            catch(InvalidDataException){throw new ArgumentException("ไฟล์ Excel ไม่ถูกต้องหรือเสียหาย");}
            var refs=References(); var prepared=new List<Tuple<int,AssetSurvey,string>>(); var errors=new List<string>();
            foreach(var item in rows.Select((r,i)=>new{Row=r,Number=i+6}))
            {
                try
                {
                    var r=item.Row; var asset=Text(r,"Asset Name"); if(string.IsNullOrWhiteSpace(asset))continue;
                    var location=ResolveLocation(refs,Text(r,"Building"),Text(r,"Floor","คอลัมน์ 1"),Text(r,"Room"));
                    decimal? w=null,l=null,h=null; ParseDimensions(Text(r,"W x L x H"),out w,out l,out h);
                    var survey=new AssetSurvey{SurveyDate=Date(r,"Survey Date")??DateTime.Today,SurveyorUserId=access.UserId,DepartmentId=Resolve(refs["Department"],Text(r,"Department"),"Department",item.Number),CustodianName=Text(r,"Custodian"),AssetName=asset,CategoryId=Resolve(refs["Category"],Text(r,"Category"),"Category",item.Number),Brand=Text(r,"Brand"),ModelDescription=Text(r,"Model"),SerialNumber=Text(r,"Serial Number"),OwnershipType=Text(r,"Ownership"),ConditionId=Resolve(refs["Condition"],Text(r,"Condition"),"Condition",item.Number),WidthCm=w,LengthCm=l,HeightCm=h,WeightKg=Decimal(r,"Weight"),Quantity=Decimal(r,"Qty")??1,UomId=Resolve(refs["Uom"],Text(r,"UOM"),"UOM",item.Number),BuildingId=location.Item1,FloorId=location.Item2,RoomId=location.Item3,ReceivedDate=Date(r,"Received Date"),PurchaseOrderNo=Text(r,"PO No."),EstimatedValue=Decimal(r,"Estimated Value"),Remark=Text(r,"Remark")};
                    if(survey.Quantity<=0)throw new ArgumentException("Qty ต้องมากกว่า 0");
                    prepared.Add(Tuple.Create(item.Number,survey,Text(r,"Survey No")));
                }catch(Exception ex){errors.Add("แถว "+item.Number+": "+ex.Message);if(errors.Count>=10)break;}
            }
            if(errors.Count>0)throw new ArgumentException("ไม่สามารถนำเข้าได้: "+string.Join(" | ",errors));
            var result=new ExcelImportResult();
            foreach(var item in prepared){if(!string.IsNullOrWhiteSpace(item.Item3)&&service.SurveyNumberExists(item.Item3)){result.Skipped++;continue;}service.SaveDraft(item.Item2,access);result.Imported++;}
            if(result.Imported==0&&result.Skipped==0)throw new ArgumentException("ไม่พบข้อมูลในชีต Asset Surveys");
            return result;
        }

        private Dictionary<string,List<RefItem>> References()=>new[]{"Department","Category","Condition","Uom","Building","Floor","Room"}.ToDictionary(x=>x,x=>service.GetReference(x).AsEnumerable().Select(r=>new RefItem{Id=Convert.ToInt32(r["Id"]),Name=Convert.ToString(r["Name"]),Parent=r.Table.Columns.Contains("ParentId")&&r["ParentId"]!=DBNull.Value?(int?)Convert.ToInt32(r["ParentId"]):null}).ToList(),StringComparer.OrdinalIgnoreCase);
        private Tuple<int,int,int> ResolveLocation(Dictionary<string,List<RefItem>> refs,string building,string floor,string room){var b=Resolve(refs["Building"],building,"Building",0);var f=Resolve(refs["Floor"].Where(x=>x.Parent==b).ToList(),floor,"Floor",0);var rm=Resolve(refs["Room"].Where(x=>x.Parent==f).ToList(),room,"Room",0);return Tuple.Create(b,f,rm);}
        private static int Resolve(List<RefItem> list,string value,string label,int row){var key=Normalize(value);var found=list.FirstOrDefault(x=>Normalize(x.Name)==key||Normalize(CodePart(value))==Normalize(x.Name));if(found==null)throw new ArgumentException(label+" '"+value+"' ไม่ตรงกับ Master Data");return found.Id;}
        private sealed class RefItem{public int Id;public string Name;public int? Parent;}
        private static string CodePart(string s){var i=(s??"").IndexOf(" - ",StringComparison.Ordinal);return i<0?s:s.Substring(i+3);}
        private static string Normalize(string s)=>Regex.Replace((s??"").Trim().ToUpperInvariant(),@"\s+"," ");
        private static string Text(Dictionary<string,string> r,params string[] names){foreach(var n in names){string v;if(r.TryGetValue(Normalize(n),out v))return (v??"").Trim();}return "";}
        private static decimal? Decimal(Dictionary<string,string> r,string name){decimal v;var s=Text(r,name);return decimal.TryParse(s,NumberStyles.Any,CultureInfo.InvariantCulture,out v)?v:(decimal?)null;}
        private static DateTime? Date(Dictionary<string,string> r,string name){var s=Text(r,name);double serial;DateTime d;if(double.TryParse(s,NumberStyles.Any,CultureInfo.InvariantCulture,out serial))return DateTime.FromOADate(serial);return DateTime.TryParse(s,CultureInfo.InvariantCulture,DateTimeStyles.None,out d)||DateTime.TryParse(s,new CultureInfo("th-TH"),DateTimeStyles.None,out d)?d:(DateTime?)null;}
        private static void ParseDimensions(string s,out decimal? w,out decimal? l,out decimal? h){w=l=h=null;if(string.IsNullOrWhiteSpace(s))return;var p=Regex.Split(s,@"\s*[xX×]\s*");decimal v;if(p.Length>0&&decimal.TryParse(p[0],NumberStyles.Any,CultureInfo.InvariantCulture,out v))w=v;if(p.Length>1&&decimal.TryParse(p[1],NumberStyles.Any,CultureInfo.InvariantCulture,out v))l=v;if(p.Length>2&&decimal.TryParse(p[2],NumberStyles.Any,CultureInfo.InvariantCulture,out v))h=v;}

        private static List<Dictionary<string,string>> ReadRows(ZipArchive zip)
        {
            var wb=Load(zip,"xl/workbook.xml");var rels=Load(zip,"xl/_rels/workbook.xml.rels");var sheet=wb.Descendants(Main+"sheet").FirstOrDefault(x=>string.Equals((string)x.Attribute("name"),"Asset Surveys",StringComparison.OrdinalIgnoreCase));if(sheet==null)throw new ArgumentException("ไม่พบชีต Asset Surveys");
            var id=(string)sheet.Attribute(Rel+"id");var target=(string)rels.Descendants(PkgRel+"Relationship").First(x=>(string)x.Attribute("Id")==id).Attribute("Target");var path="xl/"+target.TrimStart('/').Replace("../","");var doc=Load(zip,path);var shared=ReadShared(zip);var all=doc.Descendants(Main+"row").ToList();var headerRow=all.FirstOrDefault(x=>(int?)x.Attribute("r")==5);if(headerRow==null)throw new ArgumentException("ไม่พบหัวตารางแถว 5");var headers=Cells(headerRow,shared);var result=new List<Dictionary<string,string>>();
            foreach(var row in all.Where(x=>(int?)x.Attribute("r")>5)){var cells=Cells(row,shared);var d=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);foreach(var h in headers)if(!string.IsNullOrWhiteSpace(h.Value)&&cells.ContainsKey(h.Key))d[Normalize(h.Value)]=cells[h.Key];result.Add(d);}return result;
        }
        private static Dictionary<int,string> Cells(XElement row,List<string> shared){var d=new Dictionary<int,string>();foreach(var c in row.Elements(Main+"c")){var r=(string)c.Attribute("r");var col=0;foreach(var ch in r.TakeWhile(char.IsLetter))col=col*26+(ch-'A'+1);var type=(string)c.Attribute("t");var raw=(string)c.Element(Main+"v")??string.Concat(c.Descendants(Main+"t").Select(x=>(string)x));int i;if(type=="s"&&int.TryParse(raw,out i)&&i<shared.Count)raw=shared[i];d[col]=raw??"";}return d;}
        private static List<string> ReadShared(ZipArchive z){var e=z.GetEntry("xl/sharedStrings.xml");if(e==null)return new List<string>();using(var s=e.Open()){var d=XDocument.Load(s);return d.Descendants(Main+"si").Select(x=>string.Concat(x.Descendants(Main+"t").Select(t=>(string)t))).ToList();}}
        private static XDocument Load(ZipArchive z,string path){var e=z.GetEntry(path.Replace('\\','/'));if(e==null)throw new InvalidDataException(path);using(var s=e.Open())return XDocument.Load(s);}
    }
}
