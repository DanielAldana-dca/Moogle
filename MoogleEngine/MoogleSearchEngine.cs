using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace MoogleEngine
{
    public static class MoogleSearchEngine
    {
        private static string[] contenido = Array.Empty<string>();
        private static string[] nombres = Array.Empty<string>();
        private static double[,] matriz = new double[0, 0];
        private static Dictionary<string, List<string>> frecuenciaGlobal = new Dictionary<string, List<string>>();
        private static string[] vocabulario = Array.Empty<string>();

        static MoogleSearchEngine()
        {
            try
            {
                // Leer y procesar los documentos
                (string[] contenidoTupla, string[] nombresTupla) = LeerArchivosDeTexto();
                contenido = contenidoTupla;
                nombres = nombresTupla;

                // Si hay documentos, construir el vocabulario y calcular matriz
                if (contenido.Length > 0 && nombres.Length > 0)
                {
                    ConstruirVocabulario();
                    matriz = CalcularMatrizTfIdf();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error durante la inicialización: {ex.Message}");
                // Inicializar con valores por defecto para evitar errores
                contenido = Array.Empty<string>();
                nombres = Array.Empty<string>();
                matriz = new double[0, 0];
                frecuenciaGlobal = new Dictionary<string, List<string>>();
                vocabulario = Array.Empty<string>();
            }
        }

        private static (string[], string[]) LeerArchivosDeTexto()
        {
            // La aplicación puede iniciarse desde la raíz del repositorio o desde MoogleServer.
            // Se prueban ambas ubicaciones para no depender del directorio de trabajo.
            string[] rutasCandidatas =
            {
                Path.Combine(Directory.GetCurrentDirectory(), "Content"),
                Path.Combine(Directory.GetCurrentDirectory(), "MoogleServer", "Content"),
                Path.Combine(AppContext.BaseDirectory, "Content")
            };

            string? rutaCarpeta = rutasCandidatas.FirstOrDefault(Directory.Exists);

            if (rutaCarpeta is null)
            {
                Console.WriteLine(
                    "No se encontró la carpeta Content. " +
                    "Coloque los documentos .txt en MoogleServer/Content.");
                return (Array.Empty<string>(), Array.Empty<string>());
            }

            string[] archivos = Directory.GetFiles(rutaCarpeta, "*.txt")
                .OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (archivos.Length == 0)
            {
                Console.WriteLine("Advertencia: No se encontraron archivos .txt en la carpeta Content");
                return (Array.Empty<string>(), Array.Empty<string>());
            }

            string[] contenidoArchivos = new string[archivos.Length];
            string[] nombresArchivos = new string[archivos.Length];

            for (int i = 0; i < archivos.Length; i++)
            {
                try
                {
                    contenidoArchivos[i] = File.ReadAllText(archivos[i], Encoding.UTF8);
                    nombresArchivos[i] = Path.GetFileName(archivos[i]);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error leyendo archivo {archivos[i]}: {ex.Message}");
                    contenidoArchivos[i] = string.Empty;
                    nombresArchivos[i] = Path.GetFileName(archivos[i]);
                }
            }

            return (contenidoArchivos, nombresArchivos);
        }

        private static string[] Normalizar(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
                return Array.Empty<string>();

            // Usar regex más robusto para separar palabras
            return Regex.Split(texto.ToLower(), @"[^\wáéíóúñ]+")
                       .Where(palabra => !string.IsNullOrEmpty(palabra) && palabra.Length > 1)
                       .ToArray();
        }

        private static void ConstruirVocabulario()
        {
            frecuenciaGlobal = new Dictionary<string, List<string>>();
            HashSet<string> vocabularioSet = new HashSet<string>();

            for (int i = 0; i < contenido.Length; i++)
            {
                if (string.IsNullOrEmpty(contenido[i]))
                    continue;

                string[] palabras = Normalizar(contenido[i]);

                foreach (string palabra in palabras)
                {
                    vocabularioSet.Add(palabra);

                    if (!frecuenciaGlobal.ContainsKey(palabra))
                    {
                        frecuenciaGlobal[palabra] = new List<string>();
                    }

                    if (!frecuenciaGlobal[palabra].Contains(nombres[i]))
                    {
                        frecuenciaGlobal[palabra].Add(nombres[i]);
                    }
                }
            }

            vocabulario = vocabularioSet.ToArray();
        }

        private static double[,] CalcularMatrizTfIdf()
        {
            if (vocabulario.Length == 0 || contenido.Length == 0)
                return new double[0, 0];

            double[,] matrizTfIdf = new double[contenido.Length, vocabulario.Length];
            int totalDocumentos = contenido.Length;

            for (int docIndex = 0; docIndex < contenido.Length; docIndex++)
            {
                if (string.IsNullOrEmpty(contenido[docIndex]))
                    continue;

                string[] palabrasDocumento = Normalizar(contenido[docIndex]);
                double[] vectorTfIdf = CalcularVectorTfIdfDocumento(palabrasDocumento, totalDocumentos);

                for (int vocabIndex = 0; vocabIndex < vocabulario.Length; vocabIndex++)
                {
                    matrizTfIdf[docIndex, vocabIndex] = vectorTfIdf[vocabIndex];
                }
            }

            return matrizTfIdf;
        }

        private static double[] CalcularVectorTfIdfDocumento(string[] palabrasDocumento, int totalDocumentos)
        {
            double[] vector = new double[vocabulario.Length];
            int totalPalabrasDoc = palabrasDocumento.Length;

            if (totalPalabrasDoc == 0)
                return vector;

            for (int i = 0; i < vocabulario.Length; i++)
            {
                string palabra = vocabulario[i];

                // Calcular TF (Term Frequency)
                int frecuencia = palabrasDocumento.Count(p => p == palabra);
                double tf = (double)frecuencia / totalPalabrasDoc;

                // Calcular IDF (Inverse Document Frequency)
                double idf = 0;
                if (frecuenciaGlobal.ContainsKey(palabra) && frecuenciaGlobal[palabra].Count > 0)
                {
                    int docsConPalabra = frecuenciaGlobal[palabra].Count;
                    idf = Math.Log((double)totalDocumentos / docsConPalabra) + 1; // Suavizado
                }

                vector[i] = tf * idf;
            }

            return vector;
        }

        private static double[] CalcularVectorTfIdfQuery(string[] palabrasQuery)
        {
            double[] vector = new double[vocabulario.Length];
            if (palabrasQuery.Length == 0 || vocabulario.Length == 0)
                return vector;

            // Cada término de la consulta se asigna a una sola palabra del vocabulario:
            // primero coincidencia exacta y, si no existe, la mejor coincidencia aproximada.
            // Así se evita que un único error tipográfico active muchos términos a la vez.
            var frecuenciasConsulta = new Dictionary<string, int>(StringComparer.Ordinal);

            foreach (string termino in palabrasQuery)
            {
                string? coincidencia = EncontrarTerminoDeVocabulario(termino);
                if (coincidencia is null)
                    continue;

                frecuenciasConsulta[coincidencia] =
                    frecuenciasConsulta.GetValueOrDefault(coincidencia) + 1;
            }

            for (int i = 0; i < vocabulario.Length; i++)
            {
                string termino = vocabulario[i];
                if (!frecuenciasConsulta.TryGetValue(termino, out int frecuencia))
                    continue;

                double tf = (double)frecuencia / palabrasQuery.Length;
                int documentosConTermino = frecuenciaGlobal[termino].Count;
                double idf = Math.Log((double)contenido.Length / documentosConTermino) + 1;
                vector[i] = tf * idf;
            }

            return vector;
        }

        private static string? EncontrarTerminoDeVocabulario(string terminoConsulta)
        {
            if (frecuenciaGlobal.ContainsKey(terminoConsulta))
                return terminoConsulta;

            // El umbral depende de la longitud: evita asociar palabras cortas con
            // términos semánticamente distintos por una distancia demasiado permisiva.
            int distanciaMaxima = terminoConsulta.Length <= 4 ? 1 :
                terminoConsulta.Length <= 7 ? 2 : 3;

            string? mejorTermino = null;
            int mejorDistancia = int.MaxValue;

            foreach (string termino in vocabulario)
            {
                if (Math.Abs(termino.Length - terminoConsulta.Length) > distanciaMaxima)
                    continue;

                int distancia = CalcularDistanciaLevenshtein(termino, terminoConsulta);
                if (distancia < mejorDistancia)
                {
                    mejorDistancia = distancia;
                    mejorTermino = termino;
                }
            }

            return mejorDistancia <= distanciaMaxima ? mejorTermino : null;
        }

        public static int CalcularDistanciaLevenshtein(string a, string b)
        {
            if (string.IsNullOrEmpty(a)) return string.IsNullOrEmpty(b) ? 0 : b.Length;
            if (string.IsNullOrEmpty(b)) return a.Length;

            int[,] matriz = new int[a.Length + 1, b.Length + 1];

            for (int i = 0; i <= a.Length; i++)
                matriz[i, 0] = i;
            for (int j = 0; j <= b.Length; j++)
                matriz[0, j] = j;

            for (int i = 1; i <= a.Length; i++)
            {
                for (int j = 1; j <= b.Length; j++)
                {
                    int costo = (a[i - 1] == b[j - 1]) ? 0 : 1;
                    matriz[i, j] = Math.Min(
                        Math.Min(matriz[i - 1, j] + 1, matriz[i, j - 1] + 1),
                        matriz[i - 1, j - 1] + costo);
                }
            }

            return matriz[a.Length, b.Length];
        }

        private static double CalcularSimilitudCoseno(double[] vector1, double[] vector2)
        {
            if (vector1.Length != vector2.Length)
                return 0;

            double productoPunto = 0;
            double magnitud1 = 0;
            double magnitud2 = 0;

            for (int i = 0; i < vector1.Length; i++)
            {
                productoPunto += vector1[i] * vector2[i];
                magnitud1 += vector1[i] * vector1[i];
                magnitud2 += vector2[i] * vector2[i];
            }

            if (magnitud1 == 0 || magnitud2 == 0)
                return 0;

            return productoPunto / (Math.Sqrt(magnitud1) * Math.Sqrt(magnitud2));
        }

        private static string ObtenerSnippet(string nombreDocumento, string query)
        {
            try
            {
                int docIndex = Array.IndexOf(nombres, nombreDocumento);
                if (docIndex == -1 || docIndex >= contenido.Length)
                    return "Fragmento no disponible";

                string textoCompleto = contenido[docIndex];
                if (string.IsNullOrEmpty(textoCompleto))
                    return "Documento vacío";

                string[] palabrasQuery = Normalizar(query);

                if (palabrasQuery.Length == 0)
                {
                    // Si no hay palabras en la query, tomar el inicio del documento
                    return textoCompleto.Length <= 150 ? textoCompleto : textoCompleto.Substring(0, 150) + "...";
                }

                // Buscar la primera ocurrencia de cualquier palabra de la query
                foreach (string palabraQuery in palabrasQuery)
                {
                    string palabraBuscar = palabraQuery;

                    // Si no encuentra la palabra exacta, buscar la más similar
                    if (textoCompleto.IndexOf(palabraQuery, StringComparison.OrdinalIgnoreCase) < 0)
                    {
                        string[] palabrasTexto = Normalizar(textoCompleto);
                        palabraBuscar = palabrasTexto.FirstOrDefault(p =>
                            CalcularDistanciaLevenshtein(p, palabraQuery) <= 2);

                        if (palabraBuscar == null) continue;
                    }

                    int indice = textoCompleto.IndexOf(palabraBuscar, StringComparison.OrdinalIgnoreCase);
                    if (indice >= 0)
                    {
                        int inicio = Math.Max(0, indice - 50);
                        int longitud = Math.Min(150, textoCompleto.Length - inicio);
                        string snippet = textoCompleto.Substring(inicio, longitud);

                        // Resaltar la palabra encontrada
                        snippet = ResaltarPalabraEnSnippet(snippet, palabraBuscar);

                        return snippet + "...";
                    }
                }

                // Si no encuentra ninguna palabra, tomar el inicio del documento
                return textoCompleto.Length <= 150 ? textoCompleto : textoCompleto.Substring(0, 150) + "...";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al generar el fragmento: {ex.Message}");
                return "Error generando fragmento";
            }
        }

        private static string ResaltarPalabraEnSnippet(string snippet, string palabra)
        {
            try
            {
                // Simple resaltado para la palabra encontrada
                return snippet.Replace(palabra, $"**{palabra}**", StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return snippet;
            }
        }

        public static SearchItem[] Busqueda(string query)
        {
            if (string.IsNullOrWhiteSpace(query) || contenido.Length == 0)
                return Array.Empty<SearchItem>();

            try
            {
                string[] palabrasQuery = Normalizar(query);

                if (palabrasQuery.Length == 0)
                    return Array.Empty<SearchItem>();

                double[] vectorQuery = CalcularVectorTfIdfQuery(palabrasQuery);
                double[] similitudes = new double[contenido.Length];

                // Calcular similitudes para cada documento
                for (int i = 0; i < contenido.Length; i++)
                {
                    if (string.IsNullOrEmpty(contenido[i]))
                    {
                        similitudes[i] = 0;
                        continue;
                    }

                    double[] vectorDocumento = new double[vocabulario.Length];
                    for (int j = 0; j < vocabulario.Length; j++)
                    {
                        vectorDocumento[j] = matriz[i, j];
                    }
                    similitudes[i] = CalcularSimilitudCoseno(vectorQuery, vectorDocumento);
                }

                // Ordenar resultados por similitud descendente
                var resultadosOrdenados = nombres.Select((nombre, index) => new
                {
                    Nombre = nombre,
                    Similitud = similitudes[index],
                    Indice = index
                })
                .Where(x => x.Similitud > 0.0) // Excluir documentos sin coincidencia
                .OrderByDescending(x => x.Similitud)
                .Take(5) // Tomar solo los 5 mejores
                .ToArray();

                SearchItem[] items = new SearchItem[resultadosOrdenados.Length];

                for (int i = 0; i < resultadosOrdenados.Length; i++)
                {
                    string snippet = ObtenerSnippet(resultadosOrdenados[i].Nombre, query);
                    items[i] = new SearchItem(
                        resultadosOrdenados[i].Nombre,
                        snippet,
                        (float)resultadosOrdenados[i].Similitud
                    );
                }

                return items;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error durante la búsqueda: {ex.Message}");
                return Array.Empty<SearchItem>();
            }
        }
    }
}