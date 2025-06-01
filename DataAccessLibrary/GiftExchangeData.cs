using DataAccessLibrary.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataAccessLibrary
{
    public class GiftExchangeData : IGiftExchangeData
    {
        private readonly ISqlDataAccess _db;

        public GiftExchangeData(ISqlDataAccess db)
        {
            _db = db;
        }

        public Task<List<GiftExchangeModel>> GetGiftExchanges()
        {
            string sql = "select * from dbo.GiftExchange";

            return _db.LoadData<GiftExchangeModel, dynamic>(sql, new { });
        }

        public Task InsertGiftExchange(GiftExchangeModel giftExchange)
        {
            string sql = @"insert into dbo.GiftExchange (Year, GiverId, ReceiverId)
                           values (@Year, @GiverId, @ReceiverId)";

            return _db.SaveData(sql, giftExchange);
        }
    }
}