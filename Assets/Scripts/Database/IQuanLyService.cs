using System;
using System.Collections.Generic;

namespace Database
{
    public interface IQuanLyService
    {
        void GetDanhSachCanHo(Action<List<CanHo>> onSuccess, Action<string> onError);
        void GetCuDanTheoCanHo(string maCanHo, Action<List<CuDan>> onSuccess, Action<string> onError);
    }
}
