using System;
using System.Collections.Generic;
using UnityEngine;

#if USE_SQLITE
using System.Data;
using System.Reflection;
#endif

namespace Database
{
    public class SqliteQuanLyService : IQuanLyService
    {
        private string connectionString;

        public SqliteQuanLyService(string dbPath)
        {
            connectionString = "URI=file:" + dbPath;
            InitDB();
        }

        private void InitDB()
        {
            Debug.Log("[SqliteQuanLyService] InitDB placeholder");
        }

        public void GetDanhSachCanHo(Action<List<CanHo>> onSuccess, Action<string> onError)
        {
            List<CanHo> result = new List<CanHo>();
            
            try
            {
#if USE_SQLITE
                // Dynamically load the SQLite connection to avoid compiler errors with missing Mono.Data.Sqlite assemblies
                Type connectionType = Type.GetType("Mono.Data.Sqlite.SqliteConnection, Mono.Data.Sqlite");
                if (connectionType == null)
                {
                    throw new Exception("SqliteConnection type not found. SQLite might not be supported in this environment.");
                }

                using (IDbConnection connection = (IDbConnection)Activator.CreateInstance(connectionType, connectionString))
                {
                    connection.Open();
                    using (IDbCommand command = connection.CreateCommand())
                    {
                        command.CommandText = "SELECT * FROM CanHo";
                        using (IDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                CanHo canHo = new CanHo();
                                
                                for (int i = 0; i < reader.FieldCount; i++)
                                {
                                    string colName = reader.GetName(i);
                                    object val = reader.GetValue(i);
                                    if (val == DBNull.Value) val = null;
                                    
                                    if (colName == "MaCanHo") canHo.MaCanHo = val?.ToString();
                                    else if (colName == "DiaChi_ToaNha") canHo.DiaChi_ToaNha = val?.ToString();
                                    else if (colName == "DienTich")
                                    {
                                        if (val != null && float.TryParse(val.ToString(), out float f)) canHo.DienTich = f;
                                    }
                                    else if (colName == "ChuSoHuu") canHo.ChuSoHuu = val?.ToString();
                                    else if (colName == "ThoiHanSoHuu") canHo.ThoiHanSoHuu = val?.ToString();
                                    else if (colName == "SoGCN") canHo.SoGCN = val?.ToString();
                                    else if (colName == "TenCanHo") canHo.TenCanHo = val?.ToString();
                                }
                                
                                result.Add(canHo);
                            }
                        }
                    }
                }
#else
                // Fake data fallback
                result.Add(new CanHo { MaCanHo = "1", DiaChi_ToaNha = "Tầng 1 - Chung cư M5", DienTich = 75.5f, ChuSoHuu = "Nguyen Van A", ThoiHanSoHuu = "Lâu dài", SoGCN = "GCN123456", TenCanHo="Căn 1" });
                result.Add(new CanHo { MaCanHo = "2", DiaChi_ToaNha = "Tầng 2 - Chung cư M5", DienTich = 80.0f, ChuSoHuu = "Tran Thi B", ThoiHanSoHuu = "50 năm", SoGCN = "GCN654321", TenCanHo="Căn 2" });
                result.Add(new CanHo { MaCanHo = "3", DiaChi_ToaNha = "Tầng 3 - Chung cư M5", DienTich = 65.0f, ChuSoHuu = "Le Van C", ThoiHanSoHuu = "Lâu dài", SoGCN = "GCN111111", TenCanHo="Căn 3" });
#endif
                onSuccess?.Invoke(result);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SqliteQuanLyService] Lỗi lấy danh sách căn hộ: {ex.Message}");
                onError?.Invoke(ex.Message);
            }
        }

        public void GetCuDanTheoCanHo(string maCanHo, Action<List<CuDan>> onSuccess, Action<string> onError)
        {
            List<CuDan> result = new List<CuDan>();
            
            try
            {
#if USE_SQLITE
                Type connectionType = Type.GetType("Mono.Data.Sqlite.SqliteConnection, Mono.Data.Sqlite");
                if (connectionType == null)
                {
                    throw new Exception("SqliteConnection type not found.");
                }

                using (IDbConnection connection = (IDbConnection)Activator.CreateInstance(connectionType, connectionString))
                {
                    connection.Open();
                    using (IDbCommand command = connection.CreateCommand())
                    {
                        command.CommandText = "SELECT * FROM CuDan cd JOIN CuTru ct ON cd.MaCuDan = ct.MaCuDan WHERE ct.MaCanHo = @maCanHo";
                        
                        // We must create parameters via the command object since we don't have the SqliteParameter type statically
                        IDbDataParameter param = command.CreateParameter();
                        param.ParameterName = "@maCanHo";
                        param.Value = maCanHo;
                        command.Parameters.Add(param);
                        
                        using (IDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                CuDan cd = new CuDan();
                                for (int i = 0; i < reader.FieldCount; i++)
                                {
                                    string colName = reader.GetName(i);
                                    object val = reader.GetValue(i);
                                    if (val == DBNull.Value) val = null;
                                    
                                    if (colName == "MaCuDan") cd.MaCuDan = val?.ToString();
                                    else if (colName == "HoTen") cd.HoTen = val?.ToString();
                                    else if (colName == "SoCCCD") cd.SoCCCD = val?.ToString();
                                    else if (colName == "NgaySinh") cd.NgaySinh = val?.ToString();
                                    else if (colName == "SDT") cd.SDT = val?.ToString();
                                    else if (colName == "Email") cd.Email = val?.ToString();
                                    else if (colName == "GioiTinh") cd.GioiTinh = val?.ToString();
                                }
                                result.Add(cd);
                            }
                        }
                    }
                }
#else
                // Fake data fallback
                result.Add(new CuDan { MaCuDan = "CD-01", HoTen = "Nguyen Van A", SoCCCD = "00123456789", NgaySinh = "01/01/1980", SDT = "0912345678", Email = "a@test.com", GioiTinh = "Nam" });
#endif
                onSuccess?.Invoke(result);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SqliteQuanLyService] Lỗi lấy cư dân của căn hộ {maCanHo}: {ex.Message}");
                onError?.Invoke(ex.Message);
            }
        }
    }
}
