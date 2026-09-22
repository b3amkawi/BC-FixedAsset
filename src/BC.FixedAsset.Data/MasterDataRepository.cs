using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace BC.FixedAsset.Data
{
    public sealed class MasterDataKind
    {
        public string Key, Label, Table, IdColumn, CodeColumn, NameColumn;
        public string ParentColumn, ParentTable, ParentIdColumn, ParentNameColumn;
        public string SecondaryColumn, NumericColumn;
        public int CodeLength, NameLength;
        public bool ParentRequired, HasActive;
    }

    public sealed class MasterDataRecord
    {
        public int Id, NumericValue;
        public int? ParentId;
        public string Code, Name, SecondaryName;
        public bool IsActive;
    }

    public sealed class MasterDataRepository
    {
        private static readonly Dictionary<string, MasterDataKind> Kinds = new Dictionary<string, MasterDataKind>(StringComparer.OrdinalIgnoreCase)
        {
            {"Company", new MasterDataKind { Key="Company", Label="Companies", Table="mst.Companies", IdColumn="CompanyId", CodeColumn="CompanyCode", NameColumn="CompanyName", CodeLength=20, NameLength=200, HasActive=true }},
            {"Division", new MasterDataKind { Key="Division", Label="Divisions", Table="mst.Divisions", IdColumn="DivisionId", CodeColumn="DivisionCode", NameColumn="DivisionName", ParentColumn="CompanyId", ParentTable="mst.Companies", ParentIdColumn="CompanyId", ParentNameColumn="CompanyName", CodeLength=30, NameLength=150, ParentRequired=true, HasActive=true }},
            {"Department", new MasterDataKind { Key="Department", Label="Departments", Table="mst.Departments", IdColumn="DepartmentId", CodeColumn="DepartmentCode", NameColumn="DepartmentName", ParentColumn="DivisionId", ParentTable="mst.Divisions", ParentIdColumn="DivisionId", ParentNameColumn="DivisionName", CodeLength=30, NameLength=150, HasActive=true }},
            {"Category", new MasterDataKind { Key="Category", Label="Asset Categories", Table="mst.AssetCategories", IdColumn="CategoryId", CodeColumn="CategoryCode", NameColumn="CategoryName", NumericColumn="UsefulLifeMonths", CodeLength=20, NameLength=150, HasActive=true }},
            {"Uom", new MasterDataKind { Key="Uom", Label="Units of Measure", Table="mst.Uoms", IdColumn="UomId", CodeColumn="UomCode", NameColumn="UomNameTh", SecondaryColumn="UomNameEn", CodeLength=20, NameLength=100, HasActive=true }},
            {"Condition", new MasterDataKind { Key="Condition", Label="Asset Conditions", Table="mst.AssetConditions", IdColumn="ConditionId", CodeColumn="ConditionCode", NameColumn="ConditionName", NumericColumn="DisplayOrder", CodeLength=30, NameLength=100, HasActive=true }},
            {"Building", new MasterDataKind { Key="Building", Label="Buildings", Table="mst.Buildings", IdColumn="BuildingId", CodeColumn="BuildingCode", NameColumn="BuildingName", CodeLength=30, NameLength=150, HasActive=true }},
            {"Floor", new MasterDataKind { Key="Floor", Label="Floors", Table="mst.Floors", IdColumn="FloorId", CodeColumn="FloorCode", NameColumn="FloorName", ParentColumn="BuildingId", ParentTable="mst.Buildings", ParentIdColumn="BuildingId", ParentNameColumn="BuildingName", NumericColumn="DisplayOrder", CodeLength=30, NameLength=100, ParentRequired=true, HasActive=true }},
            {"Room", new MasterDataKind { Key="Room", Label="Rooms", Table="mst.Rooms", IdColumn="RoomId", CodeColumn="RoomCode", NameColumn="RoomName", ParentColumn="FloorId", ParentTable="mst.Floors", ParentIdColumn="FloorId", ParentNameColumn="FloorName", CodeLength=30, NameLength=150, ParentRequired=true, HasActive=true }}
        };

        public static MasterDataKind GetKind(string key)
        {
            MasterDataKind kind;
            if (!Kinds.TryGetValue(key ?? "", out kind)) throw new ArgumentException("Unknown master data type.");
            return kind;
        }

        public DataTable GetItems(string key)
        {
            var k=GetKind(key);
            var parent=k.ParentColumn==null ? "CAST(NULL AS int) ParentId,CAST(NULL AS nvarchar(200)) ParentName" : "m."+k.ParentColumn+" ParentId,p."+k.ParentNameColumn+" ParentName";
            var secondary=k.SecondaryColumn==null ? "CAST(NULL AS nvarchar(200)) SecondaryName" : "m."+k.SecondaryColumn+" SecondaryName";
            var numeric=k.NumericColumn==null ? "CAST(NULL AS int) NumericValue" : "m."+k.NumericColumn+" NumericValue";
            var active=k.HasActive ? "m.IsActive" : "CAST(1 AS bit) IsActive";
            var join=k.ParentColumn==null ? "" : " LEFT JOIN "+k.ParentTable+" p ON p."+k.ParentIdColumn+"=m."+k.ParentColumn;
            var sql="SELECT m."+k.IdColumn+" Id,m."+k.CodeColumn+" Code,m."+k.NameColumn+" Name,"+parent+","+secondary+","+numeric+","+active+" FROM "+k.Table+" m"+join+" ORDER BY m."+k.CodeColumn;
            using(var c=Db.OpenConnection())using(var cmd=new SqlCommand(sql,c)){var t=new DataTable();using(var a=new SqlDataAdapter(cmd))a.Fill(t);return t;}
        }

        public DataTable GetParents(string key)
        {
            var k=GetKind(key);
            if(k.ParentColumn==null)return new DataTable();
            var sql="SELECT "+k.ParentIdColumn+" Id,"+k.ParentNameColumn+" Name FROM "+k.ParentTable+" ORDER BY "+k.ParentNameColumn;
            using(var c=Db.OpenConnection())using(var cmd=new SqlCommand(sql,c)){var t=new DataTable();using(var a=new SqlDataAdapter(cmd))a.Fill(t);return t;}
        }

        public void Save(string key,MasterDataRecord value)
        {
            var k=GetKind(key);
            if(value==null || string.IsNullOrWhiteSpace(value.Code) || string.IsNullOrWhiteSpace(value.Name))throw new ArgumentException("Code and name are required.");
            if(value.Code.Length>k.CodeLength || value.Name.Length>k.NameLength)throw new ArgumentException("Code or name is too long.");
            if(k.ParentRequired && !value.ParentId.HasValue)throw new ArgumentException("Select a parent record.");
            if(k.SecondaryColumn!=null && string.IsNullOrWhiteSpace(value.SecondaryName))throw new ArgumentException("English name is required.");
            if(k.SecondaryColumn!=null && value.SecondaryName.Length>100)throw new ArgumentException("English name is too long.");
            if(value.NumericValue<0)throw new ArgumentException("The number cannot be negative.");

            var columns=k.CodeColumn+","+k.NameColumn;
            var values="@Code,@Name";
            var updates=k.CodeColumn+"=@Code,"+k.NameColumn+"=@Name";
            if(k.ParentColumn!=null){columns+=","+k.ParentColumn;values+=",@ParentId";updates+=","+k.ParentColumn+"=@ParentId";}
            if(k.SecondaryColumn!=null){columns+=","+k.SecondaryColumn;values+=",@SecondaryName";updates+=","+k.SecondaryColumn+"=@SecondaryName";}
            if(k.NumericColumn!=null){columns+=","+k.NumericColumn;values+=",@NumericValue";updates+=","+k.NumericColumn+"=@NumericValue";}
            if(k.HasActive){columns+=",IsActive";values+=",@IsActive";updates+=",IsActive=@IsActive";}
            var sql=value.Id==0 ? "INSERT "+k.Table+"("+columns+") VALUES("+values+")" : "UPDATE "+k.Table+" SET "+updates+" WHERE "+k.IdColumn+"=@Id";
            using(var c=Db.OpenConnection())using(var cmd=new SqlCommand(sql,c))
            {
                cmd.Parameters.Add(Db.Parameter("@Id",value.Id,SqlDbType.Int));
                cmd.Parameters.Add(Db.Parameter("@Code",value.Code.Trim(),SqlDbType.NVarChar,k.CodeLength));
                cmd.Parameters.Add(Db.Parameter("@Name",value.Name.Trim(),SqlDbType.NVarChar,k.NameLength));
                if(k.ParentColumn!=null)cmd.Parameters.Add(Db.Parameter("@ParentId",value.ParentId,SqlDbType.Int));
                if(k.SecondaryColumn!=null)cmd.Parameters.Add(Db.Parameter("@SecondaryName",value.SecondaryName.Trim(),SqlDbType.NVarChar,100));
                if(k.NumericColumn!=null)cmd.Parameters.Add(Db.Parameter("@NumericValue",value.NumericValue,SqlDbType.Int));
                if(k.HasActive)cmd.Parameters.Add(Db.Parameter("@IsActive",value.IsActive,SqlDbType.Bit));
                try{if(cmd.ExecuteNonQuery()!=1)throw new InvalidOperationException("Master data record was not found.");}
                catch(SqlException ex) when(ex.Number==2601 || ex.Number==2627){throw new InvalidOperationException("This code already exists for the selected parent.",ex);}
                catch(SqlException ex) when(ex.Number==547){throw new InvalidOperationException("The selected parent does not exist or is not valid.",ex);}
            }
        }

        public void Delete(string key,int id)
        {
            var k=GetKind(key);
            if(id<=0)throw new ArgumentException("Invalid master data record.");
            using(var c=Db.OpenConnection())using(var cmd=new SqlCommand("DELETE FROM "+k.Table+" WHERE "+k.IdColumn+"=@Id",c))
            {
                cmd.Parameters.Add(Db.Parameter("@Id",id,SqlDbType.Int));
                try{if(cmd.ExecuteNonQuery()!=1)throw new InvalidOperationException("Master data record was not found.");}
                catch(SqlException ex) when(ex.Number==547){throw new InvalidOperationException("This record is in use and cannot be deleted. Set it to inactive instead.",ex);}
            }
        }
    }
}
