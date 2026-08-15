using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

#if USE_SQLITE || UNITY_EDITOR
using System.Data;
#endif

namespace Database
{
    public class SqliteQuanLyService : IQuanLyService
    {
        private readonly string dbPath;
        private readonly string connectionString;
        private readonly string snapshotPath;

        private sealed class SnapshotCallback
        {
            public Action<QuanLyDatabaseSnapshot> onSuccess;
            public Action<string> onError;
        }

        private static readonly List<SnapshotCallback> PendingSnapshotCallbacks =
            new List<SnapshotCallback>(4);

        private static QuanLyDatabaseSnapshot cachedSnapshot;
        private static string cachedSnapshotUri;
        private static UnityWebRequest activeSnapshotRequest;

        public SqliteQuanLyService(string dbPath)
        {
            if (string.IsNullOrWhiteSpace(dbPath))
            {
                throw new ArgumentException("A database path is required.", nameof(dbPath));
            }

            this.dbPath = dbPath.Contains("://") ? dbPath : Path.GetFullPath(dbPath);
            connectionString = "URI=file:" + this.dbPath;
            snapshotPath = Path.ChangeExtension(dbPath, ".json");
        }

#if USE_SQLITE || UNITY_EDITOR
        private IDbConnection CreateConnection()
        {
            if (!File.Exists(dbPath))
            {
                throw new FileNotFoundException("SQLite database file was not found.", dbPath);
            }

            Type connectionType = Type.GetType("Mono.Data.Sqlite.SqliteConnection, Mono.Data.Sqlite");
            if (connectionType == null)
            {
                throw new InvalidOperationException("Mono.Data.Sqlite.SqliteConnection is unavailable.");
            }

            return (IDbConnection)Activator.CreateInstance(connectionType, connectionString);
        }
#endif

        private static string GetRootErrorMessage(Exception exception)
        {
            Exception current = exception;
            while (current.InnerException != null)
            {
                current = current.InnerException;
            }

            return current.Message;
        }

        public void GetDanhSachCanHo(Action<List<CanHo>> onSuccess, Action<string> onError)
        {
            string sqliteError = null;

#if USE_SQLITE || UNITY_EDITOR
            try
            {
                List<CanHo> result = new List<CanHo>();
                using (IDbConnection connection = CreateConnection())
                {
                    connection.Open();
                    using (IDbCommand command = connection.CreateCommand())
                    {
                        command.CommandText =
                            "SELECT MaCanHo, DiaChi_ToaNha, DienTich, ChuSoHuu, " +
                            "ThoiHanSoHuu, SoGCN, TenCanHo FROM CAN_HO ORDER BY TenCanHo";
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
                                        if (val != null)
                                        {
                                            canHo.DienTich = Convert.ToSingle(val, CultureInfo.InvariantCulture);
                                        }
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

                onSuccess?.Invoke(result);
                return;
            }
            catch (Exception ex)
            {
                sqliteError = GetRootErrorMessage(ex);
                Debug.LogWarning(
                    $"[SqliteQuanLyService] Không đọc được SQLite trực tiếp; đang dùng bản dữ liệu Web: {sqliteError}");
            }
#endif

            LoadSnapshot(
                snapshot =>
                {
                    CanHoSnapshotRow[] rows = snapshot.canHo ?? Array.Empty<CanHoSnapshotRow>();
                    List<CanHo> result = new List<CanHo>(rows.Length);
                    for (int i = 0; i < rows.Length; i++)
                    {
                        if (rows[i] != null) result.Add(rows[i].ToModel());
                    }

                    onSuccess?.Invoke(result);
                },
                error => ReportFailure("danh sách căn hộ", sqliteError, error, onError));
        }

        public void GetCuDanTheoCanHo(string maCanHo, Action<List<CuDan>> onSuccess, Action<string> onError)
        {
            string sqliteError = null;

#if USE_SQLITE || UNITY_EDITOR
            try
            {
                List<CuDan> result = new List<CuDan>();
                using (IDbConnection connection = CreateConnection())
                {
                    connection.Open();
                    using (IDbCommand command = connection.CreateCommand())
                    {
                        command.CommandText =
                            "SELECT cd.MaCuDan, cd.HoTen, cd.SoCCCD, cd.NgaySinh, " +
                            "cd.SDT, cd.Email, cd.GioiTinh, " +
                            "ct.QuanHeVoiChuHo, ct.LoaiCuTru, ct.TrangThai " +
                            "FROM CU_DAN AS cd " +
                            "INNER JOIN CU_TRU AS ct ON cd.MaCuDan = ct.MaCuDan " +
                            "WHERE ct.MaCanHo = @maCanHo";
                        
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
                                    else if (colName == "QuanHeVoiChuHo") cd.QuanHeVoiChuHo = val?.ToString();
                                    else if (colName == "LoaiCuTru") cd.LoaiCuTru = val?.ToString();
                                    else if (colName == "TrangThai") cd.TrangThai = val?.ToString();
                                }
                                result.Add(cd);
                            }
                        }
                    }
                }

                onSuccess?.Invoke(result);
                return;
            }
            catch (Exception ex)
            {
                sqliteError = GetRootErrorMessage(ex);
                Debug.LogWarning(
                    $"[SqliteQuanLyService] Không đọc được SQLite trực tiếp; đang dùng bản dữ liệu Web: {sqliteError}");
            }
#endif

            LoadSnapshot(
                snapshot =>
                {
                    CuDanSnapshotRow[] rows = snapshot.cuDan ?? Array.Empty<CuDanSnapshotRow>();
                    List<CuDan> result = new List<CuDan>();
                    for (int i = 0; i < rows.Length; i++)
                    {
                        CuDanSnapshotRow row = rows[i];
                        if (row != null && string.Equals(row.maCanHo, maCanHo, StringComparison.Ordinal))
                        {
                            result.Add(row.ToModel());
                        }
                    }

                    onSuccess?.Invoke(result);
                },
                error => ReportFailure("cư dân của căn hộ " + maCanHo, sqliteError, error, onError));
        }

        private void LoadSnapshot(
            Action<QuanLyDatabaseSnapshot> onSuccess,
            Action<string> onError)
        {
            string requestUri = ToRequestUri(snapshotPath);
            if (cachedSnapshot != null && string.Equals(cachedSnapshotUri, requestUri, StringComparison.Ordinal))
            {
                onSuccess?.Invoke(cachedSnapshot);
                return;
            }

            PendingSnapshotCallbacks.Add(new SnapshotCallback
            {
                onSuccess = onSuccess,
                onError = onError
            });

            if (activeSnapshotRequest != null) return;

            activeSnapshotRequest = UnityWebRequest.Get(requestUri);
            UnityWebRequestAsyncOperation operation;
            try
            {
                operation = activeSnapshotRequest.SendWebRequest();
            }
            catch (Exception ex)
            {
                CompleteSnapshotLoad(null, GetRootErrorMessage(ex), requestUri);
                return;
            }

            operation.completed += _ =>
            {
                QuanLyDatabaseSnapshot snapshot = null;
                string error = null;

                if (activeSnapshotRequest.result != UnityWebRequest.Result.Success)
                {
                    error = activeSnapshotRequest.error;
                }
                else
                {
                    try
                    {
                        snapshot = JsonUtility.FromJson<QuanLyDatabaseSnapshot>(
                            activeSnapshotRequest.downloadHandler.text);
                        if (snapshot == null || snapshot.schemaVersion != 1)
                        {
                            error = "Database snapshot is missing or has an unsupported schema version.";
                            snapshot = null;
                        }
                    }
                    catch (Exception ex)
                    {
                        error = GetRootErrorMessage(ex);
                    }
                }

                CompleteSnapshotLoad(snapshot, error, requestUri);
            };
        }

        private static void CompleteSnapshotLoad(
            QuanLyDatabaseSnapshot snapshot,
            string error,
            string requestUri)
        {
            if (activeSnapshotRequest != null)
            {
                activeSnapshotRequest.Dispose();
                activeSnapshotRequest = null;
            }

            if (snapshot != null)
            {
                cachedSnapshot = snapshot;
                cachedSnapshotUri = requestUri;
            }

            SnapshotCallback[] callbacks = PendingSnapshotCallbacks.ToArray();
            PendingSnapshotCallbacks.Clear();
            for (int i = 0; i < callbacks.Length; i++)
            {
                if (snapshot != null)
                {
                    callbacks[i].onSuccess?.Invoke(snapshot);
                }
                else
                {
                    callbacks[i].onError?.Invoke(error ?? "Unknown database snapshot error.");
                }
            }
        }

        private static string ToRequestUri(string path)
        {
            string normalized = path.Replace('\\', '/');
            if (normalized.Contains("://")) return normalized;
            return "file:///" + normalized.TrimStart('/');
        }

        private static void ReportFailure(
            string operation,
            string sqliteError,
            string snapshotError,
            Action<string> onError)
        {
            string errorMessage = string.IsNullOrEmpty(sqliteError)
                ? snapshotError
                : sqliteError + " | Web snapshot: " + snapshotError;
            Debug.LogError($"[SqliteQuanLyService] Lỗi lấy {operation}: {errorMessage}");
            onError?.Invoke(errorMessage);
        }
    }
}
