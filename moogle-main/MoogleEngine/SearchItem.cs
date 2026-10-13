namespace MoogleEngine;

public class SearchItem
{
    public SearchItem(string title, string snippet, float score)
    {
        this.Title = title ?? throw new ArgumentNullException(nameof(title));
        this.Snippet = snippet ?? throw new ArgumentNullException(nameof(snippet));
        this.Score = score;
    }

    public string Title { get; private set; }
    public string Snippet { get; private set; }
    public float Score { get; private set; }
}
