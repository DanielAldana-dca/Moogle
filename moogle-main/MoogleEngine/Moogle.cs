namespace MoogleEngine;

public static class Moogle
{
    public static SearchResult Query(string query)
    {
        // Si la query está vacía, retornar resultado vacío
        if (string.IsNullOrWhiteSpace(query))
        {
            return new SearchResult(new SearchItem[0], query);
        }

        SearchItem[] items = ElAprobado.Busqueda(query);

        return new SearchResult(items, query);
    }
}