using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Text;
using Database;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

internal static class QuanLyDatabaseSnapshotExporter
{
    internal const string DatabaseAssetPath =
        "Assets/StreamingAssets/Database/ChungCuM5.db";

    internal const string SnapshotAssetPath =
        "Assets/StreamingAssets/Database/ChungCuM5.json";

    [MenuItem("Tools/Database/Export Quản Lý Hộ Dân Snapshot")]
    public static void ExportFromMenu()
    {
        Export(true);
    }

    internal static void Export(bool logSuccess)
    {
        string projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
        if (string.IsNullOrEmpty(projectRoot))
        {
            throw new BuildFailedException("Cannot resolve the Unity project root.");
        }

        string databasePath = Path.Combine(projectRoot, DatabaseAssetPath);
        string snapshotPath = Path.Combine(projectRoot, SnapshotAssetPath);
        if (!File.Exists(databasePath))
        {
            throw new BuildFailedException("SQLite database was not found: " + databasePath);
        }

        QuanLyDatabaseSnapshot snapshot = ReadSnapshot(databasePath);
        string json = JsonUtility.ToJson(snapshot, true) + Environment.NewLine;
        string current = File.Exists(snapshotPath) ? File.ReadAllText(snapshotPath) : null;
        if (!string.Equals(current, json, StringComparison.Ordinal))
        {
            File.WriteAllText(snapshotPath, json, new UTF8Encoding(false));
            AssetDatabase.ImportAsset(SnapshotAssetPath, ImportAssetOptions.ForceUpdate);
            Debug.Log(
                $"[Database] Exported {snapshot.canHo.Length} apartments and " +
                $"{snapshot.cuDan.Length} residents to {SnapshotAssetPath}.");
        }
        else if (logSuccess)
        {
            Debug.Log(
                $"[Database] Snapshot is current: {snapshot.canHo.Length} apartments, " +
                $"{snapshot.cuDan.Length} residents.");
        }
    }

    private static QuanLyDatabaseSnapshot ReadSnapshot(string databasePath)
    {
        Type connectionType = Type.GetType("Mono.Data.Sqlite.SqliteConnection, Mono.Data.Sqlite");
        if (connectionType == null)
        {
            throw new BuildFailedException("Mono.Data.Sqlite.SqliteConnection is unavailable in the Editor.");
        }

        using (IDbConnection connection =
               (IDbConnection)Activator.CreateInstance(connectionType, "URI=file:" + databasePath))
        {
            connection.Open();
            return new QuanLyDatabaseSnapshot
            {
                schemaVersion = 1,
                canHo = ReadApartments(connection),
                cuDan = ReadResidents(connection)
            };
        }
    }

    private static CanHoSnapshotRow[] ReadApartments(IDbConnection connection)
    {
        List<CanHoSnapshotRow> rows = new List<CanHoSnapshotRow>();
        using (IDbCommand command = connection.CreateCommand())
        {
            command.CommandText =
                "SELECT MaCanHo, DiaChi_ToaNha, DienTich, ChuSoHuu, " +
                "ThoiHanSoHuu, SoGCN, TenCanHo FROM CAN_HO ORDER BY TenCanHo";
            using (IDataReader reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    rows.Add(new CanHoSnapshotRow
                    {
                        maCanHo = ReadString(reader, 0),
                        diaChiToaNha = ReadString(reader, 1),
                        dienTich = reader.IsDBNull(2)
                            ? 0f
                            : Convert.ToSingle(reader.GetValue(2), CultureInfo.InvariantCulture),
                        chuSoHuu = ReadString(reader, 3),
                        thoiHanSoHuu = ReadString(reader, 4),
                        soGCN = ReadString(reader, 5),
                        tenCanHo = ReadString(reader, 6)
                    });
                }
            }
        }

        return rows.ToArray();
    }

    private static CuDanSnapshotRow[] ReadResidents(IDbConnection connection)
    {
        List<CuDanSnapshotRow> rows = new List<CuDanSnapshotRow>();
        using (IDbCommand command = connection.CreateCommand())
        {
            command.CommandText =
                "SELECT ct.MaCanHo, cd.MaCuDan, cd.HoTen, cd.SoCCCD, cd.NgaySinh, " +
                "cd.SDT, cd.Email, cd.GioiTinh, ct.QuanHeVoiChuHo, " +
                "ct.LoaiCuTru, ct.TrangThai " +
                "FROM CU_DAN AS cd " +
                "INNER JOIN CU_TRU AS ct ON cd.MaCuDan = ct.MaCuDan " +
                "ORDER BY ct.MaCanHo, cd.HoTen";
            using (IDataReader reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    rows.Add(new CuDanSnapshotRow
                    {
                        maCanHo = ReadString(reader, 0),
                        maCuDan = ReadString(reader, 1),
                        hoTen = ReadString(reader, 2),
                        soCCCD = ReadString(reader, 3),
                        ngaySinh = ReadString(reader, 4),
                        sdt = ReadString(reader, 5),
                        email = ReadString(reader, 6),
                        gioiTinh = ReadString(reader, 7),
                        quanHeVoiChuHo = ReadString(reader, 8),
                        loaiCuTru = ReadString(reader, 9),
                        trangThai = ReadString(reader, 10)
                    });
                }
            }
        }

        return rows.ToArray();
    }

    private static string ReadString(IDataRecord reader, int index)
    {
        return reader.IsDBNull(index) ? null : reader.GetValue(index).ToString();
    }
}

internal sealed class QuanLyDatabaseSnapshotBuildProcessor : IPreprocessBuildWithReport
{
    public int callbackOrder => -1000;

    public void OnPreprocessBuild(BuildReport report)
    {
        QuanLyDatabaseSnapshotExporter.Export(false);
    }
}

internal sealed class QuanLyDatabaseSnapshotAssetPostprocessor : AssetPostprocessor
{
    private static void OnPostprocessAllAssets(
        string[] importedAssets,
        string[] deletedAssets,
        string[] movedAssets,
        string[] movedFromAssetPaths)
    {
        for (int i = 0; i < importedAssets.Length; i++)
        {
            if (string.Equals(
                    importedAssets[i],
                    QuanLyDatabaseSnapshotExporter.DatabaseAssetPath,
                    StringComparison.Ordinal))
            {
                EditorApplication.delayCall += () => QuanLyDatabaseSnapshotExporter.Export(false);
                return;
            }
        }
    }
}
