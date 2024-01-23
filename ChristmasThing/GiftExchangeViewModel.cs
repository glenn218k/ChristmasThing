namespace ChristmasThing
{
    public class GiftExchangeViewModel
    {
        private GiftExchange _giftExchange;
        public GiftExchangeViewModel(GiftExchange giftExchange)
        {
            _giftExchange = giftExchange;
        }

        public string GiverName { get; set; }
        public string ReceiverName { get; set; }
        public Guid GiverId => _giftExchange.GiverId;
        public Guid ReceiverId => _giftExchange.ReceiverId;
        public int Year => _giftExchange.Year;
        public override string ToString()
        {
            return $"{GiverName} has {ReceiverName}";
        }
    }
}