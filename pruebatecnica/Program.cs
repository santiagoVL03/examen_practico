using System.Text.Json;

var processor = new program_to_process_tasks();
var processed_tasks = processor.process_tasks("./pruebatecnica/test.json");
if (processed_tasks.error == "Todo ha sido procesado")
{
    Console.WriteLine("Tareas procesadas en el siguiente orden:");
    var formatted = "[" + string.Join(", ", processed_tasks.Item1.Select(t => $"\"{t}\"")) + "]";
    Console.WriteLine(formatted);
}
else
{
    Console.WriteLine(processed_tasks.error);
}

public class task
{
    public string id { get; set; }
    public int prioridad { get; set; }
    public int duracion { get; set; }
    public List<string> dependencias { get; set; } // Lista de IDs de tareas de las que depende esta tarea
}

//Para este ejercicio no podemos hacer uso de librerias externas osea no orders by's mas bien se puede usar el json serializer para leer el archivo json y luego ordenar las tareas manualmente usando ciclos for o while, y luego procesar las tareas en el orden correcto.
public class program_to_process_tasks
{
    private List<task> tasks = new List<task>();
    public List<task> read_entrance(string json_file)
    {
        try
        {
            string jsonContent = File.ReadAllText(json_file);
            List<task> jsonObject = JsonSerializer.Deserialize<List<task>>(jsonContent);
            return jsonObject;
        }
        catch (FileNotFoundException)
        {
            Console.WriteLine($"Error: El archivo '{json_file}' no fue encontrado.");
            return null;
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"Error: El archivo JSON no es válido. {ex.Message}");
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error inesperado: {ex.Message}");
            return null;
        }
    }
    // Array para marcar tareas ya procesadas
    private bool[] task_processed;
    // Array con contador de dependencias pendientes para cada tarea
    private int[] remaining_dependencies;

    // Obtiene la siguiente tarea a procesar basándose en:
    // 1. Tareas sin dependencias pendientes
    // 2. Mayor prioridad (número mayor)
    // 3. Menor duración (número menor)
    // 4. Orden alfabético del ID
    // Complejidad: O(n)
    private int get_next_task_index()
    {
        int best_index = -1;

        for (int i = 0; i < tasks.Count; i++)
        {
            // Solo consideramos tareas no procesadas con 0 dependencias pendientes
            if (task_processed[i] || remaining_dependencies[i] > 0)
                continue;

            // Primera tarea válida encontrada
            if (best_index == -1)
            {
                best_index = i;
                continue;
            }

            // Comparar con la mejor tarea encontrada hasta ahora
            task current = tasks[i];
            task best = tasks[best_index];

            // 1. Mayor prioridad primero
            if (current.prioridad != best.prioridad)
            {
                if (current.prioridad > best.prioridad)
                    best_index = i;
                continue;
            }

            // 2. Menor duración
            if (current.duracion != best.duracion)
            {
                if (current.duracion < best.duracion)
                    best_index = i;
                continue;
            }

            // 3. Orden alfabético (A viene antes que B)
            if (string.Compare(current.id, best.id) < 0)
                best_index = i;
        }

        return best_index;
    }

    // Valida que todas las dependencias existan en la lista de tareas
    // Complejidad: O(n^2)
    private bool validate_dependencies()
    {
        // Crear array de IDs existentes para búsqueda lineal simple
        string[] existing_task_ids = new string[tasks.Count];
        for (int i = 0; i < tasks.Count; i++)
        {
            existing_task_ids[i] = tasks[i].id;
        }

        // Verificar que cada dependencia exista en la lista de tareas
        for (int i = 0; i < tasks.Count; i++)
        {
            task current = tasks[i];
            for (int j = 0; j < current.dependencias.Count; j++)
            {
                string dependency_id = current.dependencias[j];
                bool found = false;

                // Búsqueda lineal para verificar si la dependencia existe
                for (int k = 0; k < existing_task_ids.Length; k++)
                {
                    if (existing_task_ids[k] == dependency_id)
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    return false; // Dependencia no encontrada
                }
            }
        }

        return true; // Todas las dependencias son válidas
    }

    // Inicializa los arrays de control
    // Complejidad: O(n²) para construir el array de contadores
    private void initialize_control_arrays()
    {
        task_processed = new bool[tasks.Count];
        remaining_dependencies = new int[tasks.Count];

        // Construir array de índices por ID para búsqueda rápida
        int[] task_index = new int[tasks.Count];
        for (int i = 0; i < tasks.Count; i++)
        {
            task_index[i] = i;
        }

        // Inicializar contador de dependencias para cada tarea
        for (int i = 0; i < tasks.Count; i++)
        {
            remaining_dependencies[i] = tasks[i].dependencias.Count;
        }
    }

    public (List<string>, string error) process_tasks(string json_file)
    {
        var result = new List<string>();
        this.tasks = read_entrance(json_file);

        if (this.tasks == null || this.tasks.Count == 0)
        {
            return (new List<string>(), "[]");
        }

        // Validar que todas las dependencias existan
        if (!validate_dependencies())
        {
            return (new List<string>(), "Error: Dependencia inexistente.");
        }

        // Inicializar arrays de control (booleano de procesadas + contador de dependencias)
        initialize_control_arrays();

        // Procesar tareas mientras haya tareas pendientes
        while (result.Count < tasks.Count)
        {
            // Obtener la siguiente tarea a procesar (O(n))
            int next_index = get_next_task_index();

            if (next_index == -1)
            {
                // No hay tareas sin dependencias (posible ciclo de dependencias)
                return (new List<string>(), "Error: Dependencia circular detectada.");
            }

            task current_task = tasks[next_index];

            // Procesar la tarea
            result.Add(current_task.id);
            task_processed[next_index] = true;

            // Decrementar contador de dependencias de las tareas que dependían de esta
            // Complejidad: O(n²) en peor caso, pero O(n×d) típicamente
            for (int i = 0; i < tasks.Count; i++)
            {
                if (!task_processed[i])
                {
                    // Verificar si esta tarea depende de la tarea procesada
                    for (int j = 0; j < tasks[i].dependencias.Count; j++)
                    {
                        if (tasks[i].dependencias[j] == current_task.id)
                        {
                            remaining_dependencies[i]--;
                            break;
                        }
                    }
                }
            }
        }

        return (result, "Todo ha sido procesado");
    }
}
