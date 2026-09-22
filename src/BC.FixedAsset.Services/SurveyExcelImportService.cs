using BC.FixedAsset.Core.Models;
using BC.FixedAsset.Data;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace BC.FixedAsset.Services
{
    public sealed class SurveyExcelImportService
    {
        private static readonly string[] Headers = {
            "AssetName", "CategoryCode", "SurveyDate", "Brand", "ModelDescription", "SerialNumber",
            "OwnershipType", "ConditionCode", "DepartmentCode", "CustodianName", "BuildingCode",
            "FloorCode", "RoomCode", "Quantity", "UomCode", "WidthCm", "LengthCm", "HeightCm",
            "WeightKg", "MissingDimensionReason", "ReceivedDate", "PurchaseOrderNo", "EstimatedValue", "Remark"
        };
        private readonly FixedAssetRepository repository = new FixedAssetRepository();

        public int Import(Stream input, SurveyAccessContext access)
        {
            if (input == null || access == null || access.UserId <= 0) throw new ArgumentException("Invalid import request.");
            var rows = ReadRows(input);
            if (rows.Count == 0) throw new ArgumentException("ไฟล์ไม่มีข้อมูลในชีต Surveys");
            var references = new Dictionary<string, DataTable>(StringComparer.OrdinalIgnoreCase);
            foreach (var type in new[] { "Category", "Condition", "Department", "Building", "Floor", "Room", "Uom" })
                references[type] = repository.GetReference(type);
            var surveys = rows.Select(row => Map(row, references)).ToList();
            return repository.ImportDrafts(surveys, access);
        }

        private static List<ImportRow> ReadRows(Stream input)
        {
            using (var buffer = new MemoryStream())
            {
                input.CopyTo(buffer);
                if (buffer.Length > 2 * 1024 * 1024) throw new ArgumentException("ไฟล์ Excel ต้องไม่เกิน 2 MB");
                buffer.Position = 0;
                using (var archive = new ZipArchive(buffer, ZipArchiveMode.Read))
                {
                    var workbook = Load(archive, "xl/workbook.xml");
                    XNamespace bookNs = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
                    var firstSheet = workbook.Descendants(bookNs + "sheet").FirstOrDefault();
                    if (firstSheet == null || (string)firstSheet.Attribute("name") != "Surveys")
                        throw new ArgumentException("ชีตแรกของไฟล์ต้องชื่อ Surveys ตามเทมเพลต");
                    var sheet = Load(archive, "xl/worksheets/sheet1.xml");
                    var shared = archive.GetEntry("xl/sharedStrings.xml") == null ? new List<string>() :
                        Load(archive, "xl/sharedStrings.xml").Descendants(bookNs + "si")
                            .Select(si => string.Concat(si.Descendants(bookNs + "t").Select(t => (string)t))).ToList();
                    var data = sheet.Descendants(bookNs + "sheetData").Elements(bookNs + "row").ToList();
                    if (data.Count == 0) throw new ArgumentException("ไม่พบหัวคอลัมน์ในชีต Surveys");
                    var header = Cells(data[0], shared, bookNs);
                    for (var i = 0; i < Headers.Length; i++)
                        if (header[i] != Headers[i]) throw new ArgumentException("คอลัมน์ " + (i + 1) + " ต้องเป็น " + Headers[i] + " กรุณาใช้ไฟล์เทมเพลตล่าสุด");
                    var rows = new List<ImportRow>();
                    foreach (var row in data.Skip(1))
                    {
                        var values = Cells(row, shared, bookNs);
                        if (values.All(string.IsNullOrWhiteSpace)) continue;
                        if (rows.Count == 500) throw new ArgumentException("นำเข้าได้สูงสุด 500 รายการต่อไฟล์");
                        var number = (int?)row.Attribute("r") ?? rows.Count + 2;
                        rows.Add(new ImportRow { Number = number, Values = values });
                    }
                    return rows;
                }
            }
        }

        private static XDocument Load(ZipArchive archive, string path)
        {
            var entry = archive.GetEntry(path);
            if (entry == null || entry.Length > 8 * 1024 * 1024) throw new ArgumentException("ไฟล์ Excel ไม่ถูกต้องหรือมีขนาดเกินกำหนด");
            using (var stream = entry.Open())
            using (var reader = XmlReader.Create(stream, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = 8 * 1024 * 1024 }))
                return XDocument.Load(reader);
        }

        private static string[] Cells(XElement row, IList<string> shared, XNamespace ns)
        {
            var values = new string[Headers.Length];
            foreach (var cell in row.Elements(ns + "c"))
            {
                var address = (string)cell.Attribute("r") ?? "";
                var index = 0;
                foreach (var letter in address.TakeWhile(char.IsLetter)) index = index * 26 + char.ToUpperInvariant(letter) - 'A' + 1;
                index--;
                if (index < 0 || index >= values.Length) continue;
                if (cell.Element(ns + "f") != null) throw new ArgumentException("ไม่รองรับสูตรในไฟล์นำเข้า");
                var kind = (string)cell.Attribute("t");
                var value = kind == "inlineStr" ? string.Concat(cell.Descendants(ns + "t").Select(t => (string)t)) : (string)cell.Element(ns + "v") ?? "";
                if (kind == "s")
                {
                    int sharedIndex;
                    if (!int.TryParse(value, out sharedIndex) || sharedIndex < 0 || sharedIndex >= shared.Count) throw new ArgumentException("Shared string ในไฟล์ Excel ไม่ถูกต้อง");
                    value = shared[sharedIndex];
                }
                values[index] = value.Trim();
            }
            return values;
        }

        private static AssetSurvey Map(ImportRow row, IDictionary<string, DataTable> refs)
        {
            try
            {
                var v = row.Values;
                if (string.IsNullOrWhiteSpace(v[0]) || v[0].Length > 250) throw new ArgumentException("AssetName ต้องระบุและไม่เกิน 250 ตัวอักษร");
                var building = Id(refs["Building"], v[10], "BuildingCode");
                var floor = Id(refs["Floor"], v[11], "FloorCode", building);
                var room = Id(refs["Room"], v[12], "RoomCode", floor);
                var quantity = Number(v[13], "Quantity", true).Value;
                if (quantity <= 0) throw new ArgumentException("Quantity ต้องมากกว่า 0");
                var owner = string.IsNullOrEmpty(v[6]) ? "Company Owned" : v[6];
                if (!new[] { "Company Owned", "Leased", "Customer Owned" }.Contains(owner)) throw new ArgumentException("OwnershipType ไม่ถูกต้อง");
                Limit(v[3], 120, "Brand"); Limit(v[4], 300, "ModelDescription"); Limit(v[5], 150, "SerialNumber");
                Limit(v[9], 250, "CustodianName"); Limit(v[19], 500, "MissingDimensionReason"); Limit(v[21], 100, "PurchaseOrderNo"); Limit(v[23], 1000, "Remark");
                return new AssetSurvey {
                    SurveyId = 0, AssetName = v[0], CategoryId = Id(refs["Category"], v[1], "CategoryCode"),
                    SurveyDate = Date(v[2], "SurveyDate", true).Value, Brand = v[3], ModelDescription = v[4], SerialNumber = v[5],
                    OwnershipType = owner, ConditionId = Id(refs["Condition"], v[7], "ConditionCode"),
                    DepartmentId = Id(refs["Department"], v[8], "DepartmentCode"), CustodianName = v[9],
                    BuildingId = building, FloorId = floor, RoomId = room, Quantity = quantity,
                    UomId = Id(refs["Uom"], v[14], "UomCode"), WidthCm = Number(v[15], "WidthCm"),
                    LengthCm = Number(v[16], "LengthCm"), HeightCm = Number(v[17], "HeightCm"), WeightKg = Number(v[18], "WeightKg"),
                    MissingDimensionReason = v[19], ReceivedDate = Date(v[20], "ReceivedDate"), PurchaseOrderNo = v[21],
                    EstimatedValue = Number(v[22], "EstimatedValue"), Remark = v[23]
                };
            }
            catch (ArgumentException ex) { throw new ArgumentException("แถว " + row.Number + ": " + ex.Message, ex); }
        }

        private static int Id(DataTable table, string code, string label, int? parent = null)
        {
            foreach (DataRow row in table.Rows)
                if (string.Equals(Convert.ToString(row["Code"]), code, StringComparison.OrdinalIgnoreCase) &&
                    (!parent.HasValue || Convert.ToInt32(row["ParentId"]) == parent.Value)) return Convert.ToInt32(row["Id"]);
            throw new ArgumentException(label + " '" + code + "' ไม่พบหรือไม่ตรงกับสถานที่ที่เลือก");
        }
        private static DateTime? Date(string value, string label, bool required = false)
        {
            if (string.IsNullOrWhiteSpace(value)) { if (required) throw new ArgumentException(label + " ต้องระบุ"); return null; }
            double serial;
            if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out serial) && serial >= 1 && serial <= 2958465)
                return DateTime.FromOADate(serial).Date;
            DateTime date;
            if (DateTime.TryParseExact(value, new[] { "yyyy-MM-dd", "dd/MM/yyyy" }, CultureInfo.InvariantCulture, DateTimeStyles.None, out date)) return date;
            throw new ArgumentException(label + " ต้องเป็นวันที่ เช่น 2026-09-22");
        }
        private static decimal? Number(string value, string label, bool required = false)
        {
            if (string.IsNullOrWhiteSpace(value)) { if (required) throw new ArgumentException(label + " ต้องระบุ"); return null; }
            decimal number;
            if (!decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out number) || number < 0 || number > 9999999999999999m)
                throw new ArgumentException(label + " ต้องเป็นตัวเลขที่ไม่ติดลบ");
            return number;
        }
        private static void Limit(string value, int length, string label)
        {
            if (value != null && value.Length > length) throw new ArgumentException(label + " ยาวเกิน " + length + " ตัวอักษร");
        }
        private sealed class ImportRow { public int Number; public string[] Values; }
    }
}
