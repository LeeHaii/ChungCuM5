import sqlite3
import os

db_dir = r"C:\Users\pc\Codes\ChungCuM5\Assets\StreamingAssets\Database"
os.makedirs(db_dir, exist_ok=True)
db_path = os.path.join(db_dir, "ChungCuM5.db")

conn = sqlite3.connect(db_path)
c = conn.cursor()

c.execute('''CREATE TABLE IF NOT EXISTS CAN_HO (
    MaCanHo TEXT PRIMARY KEY,
    DiaChi_ToaNha TEXT NOT NULL,
    DienTich REAL NOT NULL,
    ChuSoHuu TEXT,
    ThoiHanSoHuu TEXT,
    SoGCN TEXT
)''')

c.execute('''CREATE TABLE IF NOT EXISTS CU_DAN (
    MaCuDan TEXT PRIMARY KEY,
    HoTen TEXT NOT NULL,
    SoCCCD TEXT UNIQUE,
    NgaySinh TEXT,
    SDT TEXT,
    Email TEXT,
    GioiTinh TEXT
)''')

c.execute('''CREATE TABLE IF NOT EXISTS CU_TRU (
    MaCuTru TEXT PRIMARY KEY,
    MaCanHo TEXT NOT NULL,
    MaCuDan TEXT NOT NULL,
    QuanHeVoiChuHo TEXT,
    LoaiCuTru TEXT,
    TrangThai TEXT,
    FOREIGN KEY (MaCanHo) REFERENCES CAN_HO(MaCanHo),
    FOREIGN KEY (MaCuDan) REFERENCES CU_DAN(MaCuDan)
)''')

c.execute("INSERT OR IGNORE INTO CAN_HO VALUES ('1', 'Tầng 1 - Chung cư M5', 75.5, 'Nguyen Van A', 'Lâu dài', 'GCN123456')")
c.execute("INSERT OR IGNORE INTO CAN_HO VALUES ('2', 'Tầng 2 - Chung cư M5', 80.0, 'Tran Thi B', '50 năm', 'GCN654321')")
c.execute("INSERT OR IGNORE INTO CAN_HO VALUES ('3', 'Tầng 3 - Chung cư M5', 65.0, 'Le Van C', 'Lâu dài', 'GCN111111')")
c.execute("INSERT OR IGNORE INTO CAN_HO VALUES ('4', 'Tầng 4 - Chung cư M5', 90.0, 'Pham Thi D', 'Lâu dài', 'GCN222222')")
c.execute("INSERT OR IGNORE INTO CAN_HO VALUES ('5', 'Tầng 5 - Chung cư M5', 55.0, 'Hoang Van E', '50 năm', 'GCN333333')")
c.execute("INSERT OR IGNORE INTO CAN_HO VALUES ('6', 'Tầng 6 - Chung cư M5', 100.0, 'Vu Thi F', 'Lâu dài', 'GCN444444')")


c.execute("INSERT OR IGNORE INTO CU_DAN VALUES ('CD-01', 'Nguyen Van A', '00123456789', '01/01/1980', '0912345678', 'a@test.com', 'Nam')")
c.execute("INSERT OR IGNORE INTO CU_TRU VALUES ('CT-01', '1', 'CD-01', 'Chủ hộ', 'Thường trú', 'Đang cư trú')")

conn.commit()
conn.close()
print("Database created and seeded successfully.")
