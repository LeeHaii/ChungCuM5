import sqlite3
import random

db_path = r'C:\Users\pc\Codes\ChungCuM5\Assets\StreamingAssets\Database\ChungCuM5.db'
conn = sqlite3.connect(db_path)
cursor = conn.cursor()

# 1. Add column to CAN_HO
try:
    cursor.execute("ALTER TABLE CAN_HO ADD COLUMN TenCanHo TEXT")
except sqlite3.OperationalError as e:
    if "duplicate column name" in str(e).lower():
        pass
    else:
        print("Column might already exist or other error:", e)

# 2. Clear old data and populate 5 floors x 8 apartments
cursor.execute("DELETE FROM CU_TRU")
cursor.execute("DELETE FROM CU_DAN")
cursor.execute("DELETE FROM CAN_HO")

floors = 5
apts_per_floor = 8

last_names = ["Nguyen", "Tran", "Le", "Pham", "Hoang", "Huynh", "Phan", "Vu", "Vo", "Dang", "Bui", "Do", "Ho", "Ngo", "Duong"]
middle_names = ["Van", "Thi", "Duc", "Ngoc", "Minh", "Thu", "Thanh", "Hai"]
first_names = ["Anh", "Binh", "Chau", "Dung", "Hoa", "Linh", "Minh", "Nam", "Phong", "Quan", "Son", "Trang", "Tuan", "Yen", "Vinh"]

can_ho_data = []
cu_dan_data = []
cu_tru_data = []

cu_dan_id_counter = 1

for floor in range(1, floors + 1):
    for apt in range(1, apts_per_floor + 1):
        ten_can_ho = f"P{floor}{apt:02d}"
        ma_can_ho = f"CH_{ten_can_ho}"
        
        # Apartment data
        dien_tich = random.choice([65.0, 75.5, 80.0, 95.0, 110.0])
        
        # Resident data (Chu so huu / Chu ho)
        ho_ten = f"{random.choice(last_names)} {random.choice(middle_names)} {random.choice(first_names)}"
        ma_cu_dan = f"CD_{cu_dan_id_counter:03d}"
        so_cccd = f"0012{random.randint(1000000, 9999999)}"
        ngay_sinh = f"{random.randint(1,28):02d}/{random.randint(1,12):02d}/{random.randint(1960, 2000)}"
        sdt = f"09{random.randint(10000000, 99999999)}"
        email = f"user{cu_dan_id_counter}@test.com"
        gioi_tinh = random.choice(["Nam", "Nu"])
        
        chu_so_huu = ho_ten
        thoi_han = "Lâu dài"
        so_gcn = f"GCN{random.randint(100000, 999999)}"
        
        can_ho_data.append((ma_can_ho, f"Tầng {floor} - Chung cư M5", dien_tich, chu_so_huu, thoi_han, so_gcn, ten_can_ho))
        cu_dan_data.append((ma_cu_dan, ho_ten, so_cccd, ngay_sinh, sdt, email, gioi_tinh))
        
        # Residency data
        ma_cu_tru = f"CT_{cu_dan_id_counter:03d}"
        quan_he = "Chủ hộ"
        loai_cu_tru = "Thường trú"
        trang_thai = "Đang cư trú"
        
        cu_tru_data.append((ma_cu_tru, ma_can_ho, ma_cu_dan, quan_he, loai_cu_tru, trang_thai))
        
        cu_dan_id_counter += 1

cursor.executemany("INSERT OR REPLACE INTO CAN_HO (MaCanHo, DiaChi_ToaNha, DienTich, ChuSoHuu, ThoiHanSoHuu, SoGCN, TenCanHo) VALUES (?, ?, ?, ?, ?, ?, ?)", can_ho_data)
cursor.executemany("INSERT OR IGNORE INTO CU_DAN (MaCuDan, HoTen, SoCCCD, NgaySinh, SDT, Email, GioiTinh) VALUES (?, ?, ?, ?, ?, ?, ?)", cu_dan_data)
cursor.executemany("INSERT OR REPLACE INTO CU_TRU (MaCuTru, MaCanHo, MaCuDan, QuanHeVoiChuHo, LoaiCuTru, TrangThai) VALUES (?, ?, ?, ?, ?, ?)", cu_tru_data)

conn.commit()
conn.close()

print("Successfully updated database and populated data.")
