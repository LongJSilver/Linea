using System.Collections.Generic;

namespace Linea.Interface
{
    public interface IHistoryStorageService
    {
        IEnumerable<string> ReadList();
        void StoreList(List<string> history);
    }
}