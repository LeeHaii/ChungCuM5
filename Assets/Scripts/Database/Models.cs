using System;

namespace Database
{
    [Serializable]
    public class CanHo
    {
        public string MaCanHo { get; set; }
        public string DiaChi_ToaNha { get; set; }
        public float DienTich { get; set; }
        public string ChuSoHuu { get; set; }
        public string ThoiHanSoHuu { get; set; }
        public string SoGCN { get; set; }
        public string TenCanHo { get; set; }
    }

    [Serializable]
    public class CuDan
    {
        public string MaCuDan { get; set; }
        public string HoTen { get; set; }
        public string SoCCCD { get; set; }
        public string NgaySinh { get; set; }
        public string SDT { get; set; }
        public string Email { get; set; }
        public string GioiTinh { get; set; }
    }

    [Serializable]
    public class CuTru
    {
        public string MaCuTru { get; set; }
        public string MaCanHo { get; set; }
        public string MaCuDan { get; set; }
        public string QuanHeVoiChuHo { get; set; }
        public string LoaiCuTru { get; set; }
        public string TrangThai { get; set; }
    }
}
