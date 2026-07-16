import sqlite3
import random

db_path = r'C:\Users\pc\Codes\ChungCuM5\Assets\StreamingAssets\Database\ChungCuM5.db'
conn = sqlite3.connect(db_path)
cursor = conn.cursor()

# Get existing apartments and their Chu Ho
cursor.execute('''
    SELECT c.MaCanHo, d.HoTen, d.GioiTinh, d.NgaySinh
    FROM CAN_HO c
    JOIN CU_TRU t ON c.MaCanHo = t.MaCanHo
    JOIN CU_DAN d ON t.MaCuDan = d.MaCuDan
    WHERE t.QuanHeVoiChuHo = 'Chủ hộ'
''')
apartments = cursor.fetchall()

cu_dan_data = []
cu_tru_data = []

# Fetch the max current CD_ ID to continue sequence
cursor.execute("SELECT MaCuDan FROM CU_DAN ORDER BY CAST(REPLACE(MaCuDan, 'CD_', '') AS INTEGER) DESC LIMIT 1")
row = cursor.fetchone()
if row:
    cu_dan_id_counter = int(row[0].replace('CD_', '')) + 1
else:
    cu_dan_id_counter = 1
    
# Fetch max CT_ ID
cursor.execute("SELECT MaCuTru FROM CU_TRU ORDER BY CAST(REPLACE(MaCuTru, 'CT_', '') AS INTEGER) DESC LIMIT 1")
row_ct = cursor.fetchone()
if row_ct:
    cu_tru_id_counter = int(row_ct[0].replace('CT_', '')) + 1
else:
    cu_tru_id_counter = 1

def generate_person(ho_ten_chu_ho, relation, tuoi_offset):
    ho = ho_ten_chu_ho.split(' ')[0] # Family name same as chu ho usually
    
    if relation in ["Vợ", "Mẹ", "Con gái"]:
        gioi_tinh = "Nu"
        ten = f"{ho} {random.choice(['Thi', 'Ngoc', 'Thu'])} {random.choice(['Anh', 'Hoa', 'Linh', 'Trang', 'Yen'])}"
    else:
        gioi_tinh = "Nam"
        ten = f"{ho} {random.choice(['Van', 'Duc', 'Minh', 'Thanh'])} {random.choice(['Binh', 'Dung', 'Nam', 'Phong', 'Tuan', 'Vinh'])}"

    so_cccd = f"0012{random.randint(1000000, 9999999)}"
    nam_sinh = 1980 - tuoi_offset
    ngay_sinh = f"{random.randint(1,28):02d}/{random.randint(1,12):02d}/{nam_sinh}"
    sdt = f"09{random.randint(10000000, 99999999)}"
    return ten, so_cccd, ngay_sinh, sdt, gioi_tinh

for apt in apartments:
    ma_can_ho, ho_ten_chu_ho, gioi_tinh_chu_ho, ngay_sinh_chu_ho = apt
    
    # 1 to 3 family members
    num_members = random.randint(1, 3)
    members_to_add = []
    
    if gioi_tinh_chu_ho == 'Nam':
        spouse = "Vợ"
    else:
        spouse = "Chồng"
        
    members_to_add.append((spouse, 0)) # roughly same age
    
    if num_members >= 2:
        members_to_add.append((random.choice(["Con trai", "Con gái"]), -25)) # 25 years younger
    if num_members == 3:
        members_to_add.append((random.choice(["Bố", "Mẹ"]), 25)) # 25 years older
        
    for relation, tuoi_offset in members_to_add:
        ten, so_cccd, ngay_sinh, sdt, gioi_tinh = generate_person(ho_ten_chu_ho, relation, tuoi_offset)
        
        ma_cu_dan = f"CD_{cu_dan_id_counter:03d}"
        email = f"user{cu_dan_id_counter}@test.com"
        
        cu_dan_data.append((ma_cu_dan, ten, so_cccd, ngay_sinh, sdt, email, gioi_tinh))
        
        ma_cu_tru = f"CT_{cu_tru_id_counter:03d}"
        loai_cu_tru = "Thường trú"
        trang_thai = "Đang cư trú"
        
        cu_tru_data.append((ma_cu_tru, ma_can_ho, ma_cu_dan, relation, loai_cu_tru, trang_thai))
        
        cu_dan_id_counter += 1
        cu_tru_id_counter += 1

cursor.executemany("INSERT OR IGNORE INTO CU_DAN (MaCuDan, HoTen, SoCCCD, NgaySinh, SDT, Email, GioiTinh) VALUES (?, ?, ?, ?, ?, ?, ?)", cu_dan_data)
cursor.executemany("INSERT OR REPLACE INTO CU_TRU (MaCuTru, MaCanHo, MaCuDan, QuanHeVoiChuHo, LoaiCuTru, TrangThai) VALUES (?, ?, ?, ?, ?, ?)", cu_tru_data)

conn.commit()
conn.close()

print(f"Successfully added {len(cu_dan_data)} family members.")
