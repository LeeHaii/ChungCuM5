import sqlite3
import os

db_dir = os.path.dirname(os.path.abspath(__file__))
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
    SoGCN TEXT,
    TenCanHo TEXT
)''')

can_ho_columns = {row[1] for row in c.execute("PRAGMA table_info(CAN_HO)")}
if "TenCanHo" not in can_ho_columns:
    c.execute("ALTER TABLE CAN_HO ADD COLUMN TenCanHo TEXT")

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

can_ho_insert = """
    INSERT OR IGNORE INTO CAN_HO
        (MaCanHo, DiaChi_ToaNha, DienTich, ChuSoHuu, ThoiHanSoHuu, SoGCN, TenCanHo)
    VALUES (?, ?, ?, ?, ?, ?, ?)
"""
can_ho_rows = [
    ('1', 'Tầng 1 - Chung cư M5', 75.5, 'Nguyen Van A', 'Lâu dài', 'GCN123456', 'Căn 1'),
    ('2', 'Tầng 2 - Chung cư M5', 80.0, 'Tran Thi B', '50 năm', 'GCN654321', 'Căn 2'),
    ('3', 'Tầng 3 - Chung cư M5', 65.0, 'Le Van C', 'Lâu dài', 'GCN111111', 'Căn 3'),
    ('4', 'Tầng 4 - Chung cư M5', 90.0, 'Pham Thi D', 'Lâu dài', 'GCN222222', 'Căn 4'),
    ('5', 'Tầng 5 - Chung cư M5', 55.0, 'Hoang Van E', '50 năm', 'GCN333333', 'Căn 5'),
    ('6', 'Tầng 6 - Chung cư M5', 100.0, 'Vu Thi F', 'Lâu dài', 'GCN444444', 'Căn 6'),
]
c.executemany(can_ho_insert, can_ho_rows)


c.execute("INSERT OR IGNORE INTO CU_DAN VALUES ('CD-01', 'Nguyen Van A', '00123456789', '01/01/1980', '0912345678', 'a@test.com', 'Nam')")
c.execute("INSERT OR IGNORE INTO CU_TRU VALUES ('CT-01', '1', 'CD-01', 'Chủ hộ', 'Thường trú', 'Đang cư trú')")

conn.commit()
conn.close()
print("Database created and seeded successfully.")
