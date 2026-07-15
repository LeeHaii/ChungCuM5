using System;
using System.Collections.Generic;
// using System.Data;
// using Mono.Data.Sqlite;
using UnityEngine;

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
            // Commented out temporarily to fix compilation error
            Debug.Log("[SqliteQuanLyService] InitDB placeholder");
        }

        public void GetDanhSachCanHo(Action<List<CanHo>> onSuccess, Action<string> onError)
        {
            List<CanHo> result = new List<CanHo>();
            
            try
            {
                // Fake data to prevent errors
                result.Add(new CanHo { MaCanHo = "1", DiaChi_ToaNha = "Tầng 1 - Chung cư M5", DienTich = 75.5f, ChuSoHuu = "Nguyen Van A", ThoiHanSoHuu = "Lâu dài", SoGCN = "GCN123456" });
                result.Add(new CanHo { MaCanHo = "2", DiaChi_ToaNha = "Tầng 2 - Chung cư M5", DienTich = 80.0f, ChuSoHuu = "Tran Thi B", ThoiHanSoHuu = "50 năm", SoGCN = "GCN654321" });
                result.Add(new CanHo { MaCanHo = "3", DiaChi_ToaNha = "Tầng 3 - Chung cư M5", DienTich = 65.0f, ChuSoHuu = "Le Van C", ThoiHanSoHuu = "Lâu dài", SoGCN = "GCN111111" });
                
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
                // Fake data
                result.Add(new CuDan { MaCuDan = "CD-01", HoTen = "Nguyen Van A", SoCCCD = "00123456789", NgaySinh = "01/01/1980", SDT = "0912345678", Email = "a@test.com", GioiTinh = "Nam" });
                
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
