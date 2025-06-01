using DataAccessLibrary.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataAccessLibrary
{
    public class ParticipantData : IParticipantData
    {
        private readonly ISqlDataAccess _db;

        public ParticipantData(ISqlDataAccess db)
        {
            _db = db;
        }

        public Task<List<ParticipantModel>> GetParticipants()
        {
            string sql = "select * from dbo.Participant";

            return _db.LoadData<ParticipantModel, dynamic>(sql, new { });
        }

        public Task InsertParticipant(ParticipantModel giftExchange)
        {
            string sql = @"insert into dbo.Participant (Id, SignificantOtherId, Name)
                           values (@Id, @SignificantOtherId, @Name)";

            return _db.SaveData(sql, giftExchange);
        }
    }
}