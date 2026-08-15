using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class UnitSearchRowView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI tenCanHo;
    [SerializeField] private TextMeshProUGUI diaChiToaNha;
    [SerializeField] private TextMeshProUGUI dienTich;
    [SerializeField] private TextMeshProUGUI chuSoHuu;
    [SerializeField] private TextMeshProUGUI soGCN;
    [SerializeField] private TextMeshProUGUI thoiHanSoHuu;
    [SerializeField] private Button viewDataButton;
    private FamilyDataViewController familyDataController;
    private string unitId;
    private bool listenerRegistered;

    public void Bind(Database.CanHo unit, FamilyDataViewController familyController)
    {
        CacheReferences();
        familyDataController = familyController;
        unitId = unit?.MaCanHo;
        string displayName = string.IsNullOrEmpty(unit?.TenCanHo) ? unit?.MaCanHo : unit.TenCanHo;
        SetText(tenCanHo, displayName);
        SetText(diaChiToaNha, unit?.DiaChi_ToaNha);
        SetText(dienTich, unit != null ? unit.DienTich.ToString() : null);
        SetText(chuSoHuu, unit?.ChuSoHuu);
        SetText(soGCN, unit?.SoGCN);
        SetText(thoiHanSoHuu, unit?.ThoiHanSoHuu);

        if (viewDataButton != null)
        {
            if (!listenerRegistered)
            {
                viewDataButton.onClick.AddListener(ViewFamilyData);
                listenerRegistered = true;
            }

            viewDataButton.interactable = familyDataController != null && !string.IsNullOrEmpty(unitId);
        }
    }

    private void OnDestroy()
    {
        if (listenerRegistered && viewDataButton != null)
        {
            viewDataButton.onClick.RemoveListener(ViewFamilyData);
        }
    }

    private void ViewFamilyData()
    {
        if (familyDataController != null && !string.IsNullOrEmpty(unitId))
        {
            familyDataController.OnViewFamilyData(unitId);
        }
    }

    private void CacheReferences()
    {
        tenCanHo = Resolve(tenCanHo, "TenCanHo");
        diaChiToaNha = Resolve(diaChiToaNha, "DiaChi_ToaNha");
        dienTich = Resolve(dienTich, "DienTich");
        chuSoHuu = Resolve(chuSoHuu, "ChuSoHuu");
        soGCN = Resolve(soGCN, "SoGCN");
        thoiHanSoHuu = Resolve(thoiHanSoHuu, "ThoiHanSoHuu");

        if (viewDataButton == null)
        {
            Transform button = transform.Find("ButtonViewData");
            if (button != null) viewDataButton = button.GetComponent<Button>();
        }
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
