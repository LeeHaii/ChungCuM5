#if UNITY_INCLUDE_TESTS
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace Database.Tests
{
    public sealed class QuanLyDatabaseSnapshotTests
    {
        [Test]
        public void SnapshotContainsAllDatabaseRows()
        {
            string path = Path.Combine(
                Application.dataPath,
                "StreamingAssets",
                "Database",
                "ChungCuM5.json");
            QuanLyDatabaseSnapshot snapshot =
                JsonUtility.FromJson<QuanLyDatabaseSnapshot>(File.ReadAllText(path));

            Assert.That(snapshot, Is.Not.Null);
            Assert.That(snapshot.schemaVersion, Is.EqualTo(1));
            Assert.That(snapshot.canHo, Has.Length.EqualTo(40));
            Assert.That(snapshot.cuDan, Has.Length.EqualTo(126));

            int p101Residents = 0;
            for (int i = 0; i < snapshot.cuDan.Length; i++)
            {
                if (snapshot.cuDan[i].maCanHo == "CH_P101") p101Residents++;
            }

            Assert.That(p101Residents, Is.EqualTo(4));
        }

        [Test]
        public void ServiceReadsActualSqliteRowsInEditor()
        {
            string path = Path.Combine(
                Application.streamingAssetsPath,
                "Database",
                "ChungCuM5.db");
            SqliteQuanLyService service = new SqliteQuanLyService(path);
            List<CanHo> apartments = null;
            List<CuDan> residents = null;
            string error = null;

            service.GetDanhSachCanHo(rows => apartments = rows, value => error = value);
            service.GetCuDanTheoCanHo("CH_P101", rows => residents = rows, value => error = value);

            Assert.That(error, Is.Null);
            Assert.That(apartments, Has.Count.EqualTo(40));
            Assert.That(residents, Has.Count.EqualTo(4));
            Assert.That(apartments[0].TenCanHo, Is.EqualTo("P101"));
            Assert.That(residents[0].HoTen, Is.EqualTo("Nguyen Ngoc Yen"));
        }
    }
}
#endif
