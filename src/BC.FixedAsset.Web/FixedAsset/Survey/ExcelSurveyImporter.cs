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
using System.Xml.Linq;

namespace BC.FixedAsset.Web.FixedAsset.Survey
{
    internal sealed class ExcelImportResult { public int Imported,Images; }
    internal sealed class ExcelPreviewResult { public List<ExcelPreviewRow> Rows=new List<ExcelPreviewRow>(); public int Ready,Images; public bool HasErrors; }
    internal sealed class ExcelPreviewRow
    {
        public int RowNumber { get; set; }
        public int ImageCount { get; set; }
        public string AssetName { get; set; }
        public string SurveyDate { get; set; }
        public string Location { get; set; }
        public string Status { get; set; }
        public string Error { get; set; }
    }
    internal sealed class ImportImage { public string Type,Name,ContentType; public byte[] Data; }
    internal sealed class PreparedImport { public ExcelPreviewResult Preview=new ExcelPreviewResult(); public List<Tuple<AssetSurvey,string,List<ImportImage>>> Rows=new List<Tuple<AssetSurvey,string,List<ImportImage>>>(); }

    internal sealed class ExcelSurveyImporter
    {
        private readonly AssetSurveyService service; private readonly SurveyAccessContext access;
        private static readonly XNamespace M="http://schemas.openxmlformats.org/spreadsheetml/2006/main",R="http://schemas.openxmlformats.org/officeDocument/2006/relationships",PR="http://schemas.openxmlformats.org/package/2006/relationships",Xdr="http://schemas.openxmlformats.org/drawingml/2006/spreadsheetDrawing",A="http://schemas.openxmlformats.org/drawingml/2006/main";
        public ExcelSurveyImporter(AssetSurveyService s,SurveyAccessContext a){service=s;access=a;}
        public ExcelPreviewResult Preview(string path){return Prepare(path).Preview;}
        public ExcelImportResult Import(string path){var p=Prepare(path);if(p.Preview.HasErrors)throw new ArgumentException("พบข้อมูลไม่ถูกต้อง กรุณาแก้ไฟล์แล้วตรวจสอบอีกครั้ง");var result=new ExcelImportResult();foreach(var x in p.Rows){var id=service.SaveDraft(x.Item1,access);foreach(var image in x.Item3.GroupBy(i=>i.Type).Select(g=>g.First())){service.SaveAttachmentBytes(id,image.Type,image.Name,image.ContentType,image.Data,access);result.Images++;}result.Imported++;}return result;}

        private PreparedImport Prepare(string path)
        {
            WorkbookData book;using(var stream=File.OpenRead(path))using(var zip=new ZipArchive(stream,ZipArchiveMode.Read))book=ReadBook(zip);
            var output=new PreparedImport();var refs=References();
            foreach(var pair in book.Rows)
            {
                var n=pair.Key;var r=pair.Value;var name=Text(r,"Asset Name");if(string.IsNullOrWhiteSpace(name))continue;var surveyDate=Date(r,"Survey Date")??DateTime.Today;var view=new ExcelPreviewRow{RowNumber=n,AssetName=name,SurveyDate=surveyDate.ToString("dd/MM/yyyy",CultureInfo.InvariantCulture)};
                try{var b=Text(r,"Building");var f=Text(r,"Floor","คอลัมน์ 1");var room=Text(r,"Room");var loc=ResolveLocation(refs,b,f,room);view.Location=string.Join(" / ",new[]{b,f,room});decimal? w,l,h;Dimensions(Text(r,"W x L x H"),out w,out l,out h);var s=new AssetSurvey{SurveyDate=surveyDate,DepartmentId=Resolve(refs["Department"],Text(r,"Department"),"Department"),CustodianName=Text(r,"Custodian"),AssetName=name,CategoryId=Resolve(refs["Category"],Text(r,"Category"),"Category"),Brand=Text(r,"Brand"),ModelDescription=Text(r,"Model"),SerialNumber=Text(r,"Serial Number"),OwnershipType=Text(r,"Ownership"),ConditionId=Resolve(refs["Condition"],Text(r,"Condition"),"Condition"),WidthCm=w,LengthCm=l,HeightCm=h,WeightKg=Number(r,"Weight"),Quantity=Number(r,"Qty")??1,UomId=Resolve(refs["Uom"],Text(r,"UOM"),"UOM"),BuildingId=loc.Item1,FloorId=loc.Item2,RoomId=loc.Item3,ReceivedDate=Date(r,"Received Date"),PurchaseOrderNo=Text(r,"PO No."),EstimatedValue=Number(r,"Estimated Value"),Remark=Text(r,"Remark")};if(s.Quantity<=0)throw new ArgumentException("Qty ต้องมากกว่า 0");List<ImportImage> images;if(!book.Images.TryGetValue(n,out images))images=new List<ImportImage>();view.ImageCount=images.Select(i=>i.Type).Distinct().Count();output.Preview.Images+=view.ImageCount;view.Status="พร้อมนำเข้า";output.Preview.Ready++;output.Rows.Add(Tuple.Create(s,string.Empty,images));}
                catch(Exception ex){view.Status="ข้อมูลไม่ถูกต้อง";view.Error=ex.Message;output.Preview.HasErrors=true;}output.Preview.Rows.Add(view);
            }if(output.Preview.Rows.Count==0)throw new ArgumentException("ไม่พบข้อมูลในชีต Asset Surveys");return output;
        }

        private Dictionary<string,List<RefItem>> References()=>new[]{"Department","Category","Condition","Uom","Building","Floor","Room"}.ToDictionary(x=>x,x=>service.GetReference(x).AsEnumerable().Select(r=>new RefItem{Id=Convert.ToInt32(r["Id"]),Name=Convert.ToString(r["Name"]),Parent=r.Table.Columns.Contains("ParentId")&&r["ParentId"]!=DBNull.Value?(int?)Convert.ToInt32(r["ParentId"]):null}).ToList(),StringComparer.OrdinalIgnoreCase);
        private Tuple<int,int,int> ResolveLocation(Dictionary<string,List<RefItem>> x,string b,string f,string r){var bi=Resolve(x["Building"],b,"Building");var fi=Resolve(x["Floor"].Where(i=>i.Parent==bi).ToList(),f,"Floor");var ri=Resolve(x["Room"].Where(i=>i.Parent==fi).ToList(),r,"Room");return Tuple.Create(bi,fi,ri);}
        private sealed class RefItem{public int Id;public string Name;public int? Parent;}
        private static int Resolve(List<RefItem> x,string value,string label){var key=Norm(value);var found=x.FirstOrDefault(i=>Norm(i.Name)==key||Norm(CodePart(value))==Norm(i.Name));if(found==null)throw new ArgumentException(label+" '"+value+"' ไม่ตรงกับ Master Data");return found.Id;}
        private static string CodePart(string s){var i=(s??"").IndexOf(" - ",StringComparison.Ordinal);return i<0?s:s.Substring(i+3);}private static string Norm(string s)=>Regex.Replace((s??"").Trim().ToUpperInvariant(),@"\s+"," ");
        private static string Text(Dictionary<string,string> r,params string[] names){foreach(var n in names){string v;if(r.TryGetValue(Norm(n),out v))return(v??"").Trim();}return"";}
        private static decimal? Number(Dictionary<string,string> r,string n){decimal v;return decimal.TryParse(Text(r,n),NumberStyles.Any,CultureInfo.InvariantCulture,out v)?v:(decimal?)null;}
        private static DateTime? Date(Dictionary<string,string> r,string n){var s=Text(r,n);double v;DateTime d;if(double.TryParse(s,NumberStyles.Any,CultureInfo.InvariantCulture,out v))return DateTime.FromOADate(v);return DateTime.TryParse(s,CultureInfo.InvariantCulture,DateTimeStyles.None,out d)||DateTime.TryParse(s,new CultureInfo("th-TH"),DateTimeStyles.None,out d)?d:(DateTime?)null;}
        private static void Dimensions(string s,out decimal? w,out decimal? l,out decimal? h){w=l=h=null;var p=Regex.Split(s??"",@"\s*[xX×]\s*");decimal v;if(p.Length>0&&decimal.TryParse(p[0],NumberStyles.Any,CultureInfo.InvariantCulture,out v))w=v;if(p.Length>1&&decimal.TryParse(p[1],NumberStyles.Any,CultureInfo.InvariantCulture,out v))l=v;if(p.Length>2&&decimal.TryParse(p[2],NumberStyles.Any,CultureInfo.InvariantCulture,out v))h=v;}

        private sealed class WorkbookData{public SortedDictionary<int,Dictionary<string,string>> Rows=new SortedDictionary<int,Dictionary<string,string>>();public Dictionary<int,List<ImportImage>> Images=new Dictionary<int,List<ImportImage>>();}
        private static WorkbookData ReadBook(ZipArchive z)
        {
            var wb=Xml(z,"xl/workbook.xml");var rels=Xml(z,"xl/_rels/workbook.xml.rels");var sheet=wb.Descendants(M+"sheet").FirstOrDefault(x=>string.Equals((string)x.Attribute("name"),"Asset Surveys",StringComparison.OrdinalIgnoreCase));if(sheet==null)throw new ArgumentException("ไม่พบชีต Asset Surveys");var id=(string)sheet.Attribute(R+"id");var target=(string)rels.Descendants(PR+"Relationship").First(x=>(string)x.Attribute("Id")==id).Attribute("Target");var sheetPath=ResolvePart("xl/workbook.xml",target);var doc=Xml(z,sheetPath);var shared=Shared(z);var rows=doc.Descendants(M+"row").ToList();var head=rows.FirstOrDefault(x=>(int?)x.Attribute("r")==5);if(head==null)throw new ArgumentException("ไม่พบหัวตารางแถว 5");var headers=Cells(head,shared);var book=new WorkbookData();foreach(var row in rows.Where(x=>(int?)x.Attribute("r")>5)){var n=(int)row.Attribute("r");var cells=Cells(row,shared);var data=new Dictionary<string,string>();foreach(var h in headers)if(!string.IsNullOrWhiteSpace(h.Value)&&cells.ContainsKey(h.Key))data[Norm(h.Value)]=cells[h.Key];book.Rows[n]=data;}book.Images=Images(z,sheetPath,doc);return book;
        }
        private static Dictionary<int,List<ImportImage>> Images(ZipArchive z,string sheetPath,XDocument sheet)
        {
            var result=new Dictionary<int,List<ImportImage>>();var drawing=sheet.Descendants(M+"drawing").FirstOrDefault();if(drawing==null)return result;var srels=Xml(z,RelsPath(sheetPath));var rid=(string)drawing.Attribute(R+"id");var target=(string)srels.Descendants(PR+"Relationship").First(x=>(string)x.Attribute("Id")==rid).Attribute("Target");var dpath=ResolvePart(sheetPath,target);var ddoc=Xml(z,dpath);var drels=Xml(z,RelsPath(dpath));foreach(var anchor in ddoc.Descendants().Where(x=>x.Name==Xdr+"oneCellAnchor"||x.Name==Xdr+"twoCellAnchor")){var from=anchor.Element(Xdr+"from");var blip=anchor.Descendants(A+"blip").FirstOrDefault();if(from==null||blip==null)continue;var row=(int)from.Element(Xdr+"row")+1;var col=(int)from.Element(Xdr+"col")+1;if(col<1||col>3)continue;var erid=(string)blip.Attribute(R+"embed");var mt=(string)drels.Descendants(PR+"Relationship").First(x=>(string)x.Attribute("Id")==erid).Attribute("Target");var mpath=ResolvePart(dpath,mt);var entry=z.GetEntry(mpath);if(entry==null)continue;byte[] bytes;using(var s=entry.Open())using(var ms=new MemoryStream()){s.CopyTo(ms);bytes=ms.ToArray();}if(bytes.Length>5*1024*1024)continue;List<ImportImage> list;if(!result.TryGetValue(row,out list))result[row]=list=new List<ImportImage>();var ext=Path.GetExtension(mpath).ToLowerInvariant();list.Add(new ImportImage{Type=col==1?"ACTUAL":col==2?"SERIAL":"OTHER",Name=Path.GetFileName(mpath),ContentType=ext==".png"?"image/png":ext==".webp"?"image/webp":"image/jpeg",Data=bytes});}return result;
        }
        private static string RelsPath(string p)=>Path.GetDirectoryName(p).Replace('\\','/')+"/_rels/"+Path.GetFileName(p)+".rels";private static string ResolvePart(string source,string target)=>new Uri(new Uri("http://local/"+source),target).AbsolutePath.TrimStart('/');
        private static Dictionary<int,string> Cells(XElement row,List<string> shared){var d=new Dictionary<int,string>();foreach(var c in row.Elements(M+"c")){var r=(string)c.Attribute("r");var col=0;foreach(var ch in r.TakeWhile(char.IsLetter))col=col*26+ch-'A'+1;var type=(string)c.Attribute("t");var raw=(string)c.Element(M+"v")??string.Concat(c.Descendants(M+"t").Select(x=>(string)x));int i;if(type=="s"&&int.TryParse(raw,out i)&&i<shared.Count)raw=shared[i];d[col]=raw??"";}return d;}
        private static List<string> Shared(ZipArchive z){var e=z.GetEntry("xl/sharedStrings.xml");if(e==null)return new List<string>();using(var s=e.Open()){var d=XDocument.Load(s);return d.Descendants(M+"si").Select(x=>string.Concat(x.Descendants(M+"t").Select(t=>(string)t))).ToList();}}private static XDocument Xml(ZipArchive z,string p){var e=z.GetEntry(p.Replace('\\','/'));if(e==null)throw new InvalidDataException(p);using(var s=e.Open())return XDocument.Load(s);}
    }
}
