using DataAccessLibrary.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataAccessLibrary
{
    public interface IParticipantData
    {
        Task<List<ParticipantModel>> GetParticipants();
        Task InsertParticipant(ParticipantModel giftExchange);
    }
}