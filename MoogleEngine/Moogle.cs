namespace MoogleEngine;

public static class Moogle
{
    public static SearchResult Query(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return new SearchResult(Array.Empty<SearchItem>());

        SearchItem[] items = MoogleSearchEngine.Busqueda(query);
        return new SearchResult(items);
    }
}
