using System;

namespace Database
{
    [Serializable]
    public sealed class QuanLyDatabaseSnapshot
    {
        public int schemaVersion = 1;
        public CanHoSnapshotRow[] canHo = Array.Empty<CanHoSnapshotRow>();
        public CuDanSnapshotRow[] cuDan = Array.Empty<CuDanSnapshotRow>();
    }

    [Serializable]
    public sealed class CanHoSnapshotRow
    {
        public string maCanHo;
        public string diaChiToaNha;
        public float dienTich;
        public string chuSoHuu;
        public string thoiHanSoHuu;
        public string soGCN;
        public string tenCanHo;

        public CanHo ToModel()
        {
            return new CanHo
            {
                MaCanHo = maCanHo,
                DiaChi_ToaNha = diaChiToaNha,
                DienTich = dienTich,
                ChuSoHuu = chuSoHuu,
                ThoiHanSoHuu = thoiHanSoHuu,
                SoGCN = soGCN,
                TenCanHo = tenCanHo
            };
        }
    }

    [Serializable]
    public sealed class CuDanSnapshotRow
    {
        public string maCanHo;
        public string maCuDan;
        public string hoTen;
        public string soCCCD;
        public string ngaySinh;
        public string sdt;
        public string email;
        public string gioiTinh;
        public string quanHeVoiChuHo;
        public string loaiCuTru;
        public string trangThai;

        public CuDan ToModel()
        {
            return new CuDan
            {
                MaCuDan = maCuDan,
                HoTen = hoTen,
                SoCCCD = soCCCD,
                NgaySinh = ngaySinh,
                SDT = sdt,
                Email = email,
                GioiTinh = gioiTinh,
                QuanHeVoiChuHo = quanHeVoiChuHo,
                LoaiCuTru = loaiCuTru,
                TrangThai = trangThai
            };
        }
    }
}
