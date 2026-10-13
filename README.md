# Moogle

Motor de búsqueda textual desarrollado como proyecto académico de Programación I en la Facultad de Matemática y Computación de la Universidad de La Habana (cursos 2021–2022).

Moogle permite consultar un conjunto local de documentos de texto y ordena los resultados según su similitud con la consulta. La aplicación incluye una interfaz web basada en Blazor y un motor de recuperación implementado en C#.

![Interfaz y logotipo de Moogle](moogle.png)

## Características

- Indexación de documentos `.txt` ubicados en `MoogleServer/Content`.
- Normalización de términos y construcción de un vocabulario a partir de los documentos.
- Representación de documentos y consultas mediante vectores TF-IDF.
- Ordenamiento de resultados por similitud del coseno.
- Tolerancia limitada a errores tipográficos mediante distancia de Levenshtein.
- Generación de fragmentos de texto (*snippets*) alrededor de términos relacionados con la consulta.
- Presentación de los documentos más relevantes en una interfaz web.
- Manejo de consultas vacías, documentos vacíos y ausencia de resultados.

## Requisitos

- .NET SDK 9.0.
- Terminal compatible con los comandos de .NET.
- Un navegador web moderno.

Para comprobar la instalación del SDK:

```bash
dotnet --version
```

La versión principal del SDK debe ser `9.x`, ya que ambos proyectos apuntan a `net9.0`.

## Estructura del proyecto

```text
moogle-main/
├── Moogle.sln
├── MoogleEngine/
│   ├── MoogleEngine.csproj
│   ├── Moogle.cs
│   ├── MoogleSearchEngine.cs
│   ├── SearchItem.cs
│   └── SearchResult.cs
├── MoogleServer/
│   ├── MoogleServer.csproj
│   ├── Program.cs
│   ├── Pages/
│   │   └── Index.razor
│   ├── Shared/
│   ├── wwwroot/
│   └── Content/
│       ├── hola.txt
│       ├── saludo.txt
│       └── ...
├── Evaluation.md
├── informe de Moglee.pdf
├── LICENSE
├── Readme.md
└── makefile
```

### Responsabilidad de los componentes

- **`MoogleEngine/Moogle.cs`**: expone el punto de entrada `Moogle.Query` que utiliza la interfaz.
- **`MoogleEngine/MoogleSearchEngine.cs`**: carga los documentos, construye el vocabulario y la matriz TF-IDF, calcula similitudes, gestiona coincidencias aproximadas y prepara los fragmentos de los resultados.
- **`MoogleEngine/SearchItem.cs`**: representa un resultado individual con título, fragmento y puntuación.
- **`MoogleEngine/SearchResult.cs`**: encapsula la colección de resultados y la consulta asociada al resultado.
- **`MoogleServer/Pages/Index.razor`**: implementa la pantalla de búsqueda.
- **`MoogleServer/Content/`**: contiene los documentos que se indexan. Se pueden añadir nuevos archivos `.txt` a esta carpeta.

## Ejecución

Ejecute los comandos desde la carpeta raíz del repositorio, donde se encuentra `Moogle.sln`.

### Iniciar la aplicación en modo desarrollo

```bash
dotnet watch run --project MoogleServer
```

Al iniciar, la terminal indicará la dirección local asignada por ASP.NET Core. Abra esa dirección en el navegador.

### Compilar la solución

```bash
dotnet build Moogle.sln
```

También se puede utilizar el objetivo `build` del archivo `makefile`:

```bash
make build
```

En entornos con `make` instalado, el objetivo `dev` inicia la aplicación en modo de desarrollo:

```bash
make dev
```

En Windows, los comandos `dotnet` pueden ejecutarse directamente desde PowerShell o desde la terminal integrada de Visual Studio Code; no es obligatorio utilizar WSL.

## Añadir documentos al índice

1. Guarde cada documento como archivo de texto con extensión `.txt`.
2. Colóquelo en `MoogleServer/Content/`.
3. Reinicie la aplicación para que el motor vuelva a cargar el conjunto de documentos.
4. Introduzca una consulta en la página principal.

El motor lee los archivos al inicializarse. Por tanto, los cambios realizados en los documentos después de la inicialización no se incorporan hasta que la aplicación se reinicie. Los resultados dependen del contenido disponible en esa carpeta.

## Funcionamiento del motor de búsqueda

### 1. Carga y normalización

El motor lee los archivos `.txt`, conserva el nombre de cada archivo como título del resultado y normaliza el texto para obtener sus términos. La normalización convierte el texto a minúsculas y separa los términos por caracteres que no sean letras, dígitos o guiones bajos.

### 2. Vocabulario y frecuencia documental

Se construye un vocabulario con los términos encontrados y se registra en cuántos documentos aparece cada término. Esta información se utiliza para calcular el peso de cada palabra.

### 3. Representación TF-IDF

Cada documento se representa mediante un vector cuyos componentes se calculan con:

- **TF (*Term Frequency*)**: frecuencia del término en el documento dividida entre la cantidad de términos del documento.
- **IDF (*Inverse Document Frequency*)**: `log(N / df) + 1`, donde `N` es la cantidad de documentos cargados y `df` es la cantidad de documentos que contienen el término.

El peso final es `TF × IDF`. El suavizado aditivo evita que los términos presentes en todos los documentos reciban un peso nulo.

La consulta se transforma a un vector del mismo espacio. Para errores tipográficos se busca una coincidencia aproximada limitada mediante la distancia de Levenshtein; cada término de la consulta se asocia, como máximo, a un término del vocabulario.

### 4. Ranking mediante similitud del coseno

La similitud del coseno compara el vector de la consulta con el vector de cada documento:

`similitud(A, B) = (A · B) / (||A|| × ||B||)`

Los documentos sin coincidencia efectiva se excluyen. Los restantes se ordenan de mayor a menor similitud y se muestran hasta cinco resultados.

### 5. Fragmentos de resultados

Para cada documento seleccionado se intenta localizar un término de la consulta o uno aproximado. El resultado incluye un fragmento de hasta aproximadamente 150 caracteres alrededor de esa coincidencia. Si no se encuentra un término relacionado, se utiliza el comienzo del documento.

## Consideraciones y límites conocidos

- La indexación se realiza al inicializar el motor; no existe actualización automática del índice en ejecución.
- La búsqueda aproximada se basa en distancia ortográfica y no equivale a una búsqueda semántica.
- La puntuación es una medida de similitud relativa dentro del conjunto indexado, no una probabilidad de relevancia.
- El sistema trabaja con documentos de texto plano y no extrae contenido de PDF, Word u otros formatos.
- El número máximo de resultados está fijado en cinco.
- El sistema no implementa actualmente un mecanismo independiente de sugerencias de consulta; no se muestran sugerencias de corrección que no hayan sido calculadas.

## Documentación académica

El repositorio conserva `Evaluation.md`, con los criterios de evaluación del proyecto, y el informe PDF entregado como parte de la documentación académica. Estos materiales complementan la descripción técnica de este archivo.

## Licencia

Consulte el archivo `LICENSE` para conocer los términos de uso y distribución.
