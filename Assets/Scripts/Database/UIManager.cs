using System.Collections.Generic;
using UnityEngine;
using System.IO;

namespace Database
{
    public class UIManager : MonoBehaviour
    {
        private IQuanLyService _quanLyService;

        void Start()
        {
            // Set up DB path
            string dbFileName = "ChungCuM5.db";
            string dbPath = Path.Combine(Application.streamingAssetsPath, "Database", dbFileName);

            // Instantiate service via Interface
            _quanLyService = new SqliteQuanLyService(dbPath);

            // Diagnostic data enumeration is excluded from shipping builds.
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            FetchAndLogCanHoData();
#endif
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private void FetchAndLogCanHoData()
        {
            Debug.Log("[UIManager] Đang tải danh sách căn hộ từ SQLite...");

            _quanLyService.GetDanhSachCanHo(
                onSuccess: (List<CanHo> danhSachCanHo) =>
                {
                    Debug.Log($"[UIManager] Tải thành công! Tìm thấy {danhSachCanHo.Count} căn hộ.");
                    foreach (var canHo in danhSachCanHo)
                    {
                        Debug.Log($"- Căn hộ: {canHo.MaCanHo} | Diện tích: {canHo.DienTich}m2 | Chủ sở hữu: {canHo.ChuSoHuu}");
                    }
                },
                onError: (string errorMsg) =>
                {
                    Debug.LogError($"[UIManager] Lỗi khi tải dữ liệu căn hộ: {errorMsg}");
                }
            );
        }
#endif
    }
}
