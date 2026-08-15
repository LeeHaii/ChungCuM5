using TMPro;
using UnityEngine;

public sealed class ResidentRowView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI hoTen;
    [SerializeField] private TextMeshProUGUI soCCCD;
    [SerializeField] private TextMeshProUGUI ngaySinh;
    [SerializeField] private TextMeshProUGUI sdt;
    [SerializeField] private TextMeshProUGUI email;
    [SerializeField] private TextMeshProUGUI gioiTinh;
    [SerializeField] private TextMeshProUGUI quanHeVoiChuHo;
    [SerializeField] private TextMeshProUGUI loaiCuTru;
    [SerializeField] private TextMeshProUGUI trangThai;

    public void Bind(Database.CuDan resident)
    {
        CacheReferences();
        SetText(hoTen, resident?.HoTen);
        SetText(soCCCD, resident?.SoCCCD);
        SetText(ngaySinh, resident?.NgaySinh);
        SetText(sdt, resident?.SDT);
        SetText(email, resident?.Email);
        SetText(gioiTinh, resident?.GioiTinh);
        SetText(quanHeVoiChuHo, resident?.QuanHeVoiChuHo);
        SetText(loaiCuTru, resident?.LoaiCuTru);
        SetText(trangThai, resident?.TrangThai);
    }

    private void CacheReferences()
    {
        hoTen = Resolve(hoTen, "HoTen");
        soCCCD = Resolve(soCCCD, "SoCCCD");
        ngaySinh = Resolve(ngaySinh, "NgaySinh");
        sdt = Resolve(sdt, "SDT");
        email = Resolve(email, "Email");
        gioiTinh = Resolve(gioiTinh, "GioiTinh");
        quanHeVoiChuHo = Resolve(quanHeVoiChuHo, "QuanHeVoiChuHo");
        loaiCuTru = Resolve(loaiCuTru, "LoaiCuTru");
        trangThai = Resolve(trangThai, "TrangThai");
    }

    private TextMeshProUGUI Resolve(TextMeshProUGUI cached, string childName)
    {
        if (cached != null) return cached;
        Transform child = transform.Find(childName);
        return child != null ? child.GetComponentInChildren<TextMeshProUGUI>(true) : null;
    }

    private static void SetText(TMP_Text target, string value)
    {
        if (target != null) target.SetText(string.IsNullOrEmpty(value) ? "-" : value);
    }
}
