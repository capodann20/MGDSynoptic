using Unity.Services.Analytics;

public class MatchStartEvent : Event
{
    public MatchStartEvent() : base("match_start") { }
    public int players;
}

public class CardPlayedEvent : Event
{
    public CardPlayedEvent() : base("card_played") { }
    public string player;
    public string card;
}

public class MatchEndEvent : Event
{
    public MatchEndEvent() : base("match_end") { }
    public string result;
}