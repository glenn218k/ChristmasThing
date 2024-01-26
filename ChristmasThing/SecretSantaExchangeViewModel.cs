namespace ChristmasThing
{
    public class SecretSantaExchangeViewModel
    {
        public readonly IList<GiftExchangeViewModel> GiftExchanges = new List<GiftExchangeViewModel>();
        public SecretSantaExchangeViewModel(IList<GiftExchangeViewModel> giftExchanges, int id)
        {
            foreach (var giftExchange in giftExchanges)
            {
                GiftExchanges.Add(giftExchange);
            }

            Id = id;
        }

        public string DisplayText => ToString();

        public int Id { get; init; }

        public override string ToString()
        {
            return string.Join(Environment.NewLine, GiftExchanges.Select(a => a.ToString()));
        }
    }
}