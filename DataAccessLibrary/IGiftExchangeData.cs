using DataAccessLibrary.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataAccessLibrary
{
    public interface IGiftExchangeData
    {
        Task<List<GiftExchangeModel>> GetGiftExchanges();
        Task InsertGiftExchange(GiftExchangeModel giftExchange);
    }
}