using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

Console.OutputEncoding = Encoding.UTF8;

const string Url = "https://api.first.org/data/v1/countries";

using HttpClient client = new HttpClient();

try
{
    Apresentacao.Titulo("PAÍSES DO MUNDO - API First.org");
    Console.WriteLine("A obter dados da API...");

    HttpResponseMessage resposta = await client.GetAsync(Url);
    resposta.EnsureSuccessStatusCode();

    string json = await resposta.Content.ReadAsStringAsync();

    RespostaPaises? resultado = JsonSerializer.Deserialize<RespostaPaises>(json);

    if (resultado == null)
    {
        Console.WriteLine("Não foi possível ler os dados.");
        return;
    }

    Console.WriteLine($"Dados recebidos: {resultado.Data.Count} países (total na API: {resultado.Total}).");

    bool continuar = true;

    while (continuar)
    {
        Apresentacao.Menu();
        string? opcao = Console.ReadLine();
        Console.WriteLine();

        switch (opcao)
        {
            case "1":
                {
                    var todos = resultado.Data.OrderBy(p => p.Value.Nome).ToList();
                    Apresentacao.TabelaPaginada(todos);
                    break;
                }

            case "2":
                {
                    Apresentacao.GraficoRegioes(resultado.Data);
                    break;
                }

            case "3":
                {
                    Console.Write("Escreva a região (ex: Africa, Asia, Europe): ");
                    string regiaoEscolhida = (Console.ReadLine() ?? "").Trim();

                    var daRegiao = resultado.Data
                        .Where(p => string.Equals(p.Value.Regiao, regiaoEscolhida, StringComparison.OrdinalIgnoreCase))
                        .OrderBy(p => p.Value.Nome)
                        .ToList();

                    if (daRegiao.Count == 0)
                        Apresentacao.Aviso("Nenhuma região encontrada com esse nome.");
                    else
                        Apresentacao.TabelaPaginada(daRegiao);
                    break;
                }

            case "4":
                {
                    Console.Write("Escreva parte do nome do país: ");
                    string termo = (Console.ReadLine() ?? "").Trim();

                    if (termo.Length == 0)
                    {
                        Apresentacao.Aviso("Não escreveu nada.");
                        break;
                    }

                    var encontrados = resultado.Data
                        .Where(p => p.Value.Nome.Contains(termo, StringComparison.OrdinalIgnoreCase))
                        .OrderBy(p => p.Value.Nome)
                        .ToList();

                    if (encontrados.Count == 0)
                        Apresentacao.Aviso("Nenhum país encontrado.");
                    else
                        Apresentacao.TabelaPaginada(encontrados);
                    break;
                }

            case "5":
                {
                    Console.Write("Escreva o código do país (2 letras, ex: MZ): ");
                    string codigo = (Console.ReadLine() ?? "").Trim().ToUpper();

                    if (resultado.Data.TryGetValue(codigo, out Pais? pais))
                        Apresentacao.Ficha(codigo, pais);
                    else
                        Apresentacao.Aviso("Código não encontrado.");
                    break;
                }

            case "0":
                {
                    continuar = false;
                    Console.WriteLine("Até à próxima!");
                    break;
                }

            default:
                {
                    Apresentacao.Aviso("Opção inválida. Tente outra vez.");
                    break;
                }
        }
    }
}
catch (HttpRequestException ex)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine("Erro de ligação: " + ex.Message);
    Console.ResetColor();
}
catch (JsonException ex)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine("Erro ao ler o JSON: " + ex.Message);
    Console.ResetColor();
}

// ---------- Classes que representam o JSON ----------

public class RespostaPaises
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = "";

    [JsonPropertyName("total")]
    public int Total { get; set; }

    [JsonPropertyName("data")]
    public Dictionary<string, Pais> Data { get; set; } = new();
}

public class Pais
{
    [JsonPropertyName("country")]
    public string Nome { get; set; } = "";

    [JsonPropertyName("region")]
    public string Regiao { get; set; } = "";
}

// ---------- Classe responsável só pela apresentação ----------

public static class Apresentacao
{
    private const int LarguraNome = 46;
    private const int LarguraRegiao = 17;

    public static void Titulo(string texto)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("╔" + new string('═', texto.Length + 4) + "╗");
        Console.WriteLine("║  " + texto + "  ║");
        Console.WriteLine("╚" + new string('═', texto.Length + 4) + "╝");
        Console.ResetColor();
    }

    public static void Menu()
    {
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("┌──────────────── MENU ────────────────┐");
        Console.ResetColor();
        Console.WriteLine("  1 │ Tabela de todos os países");
        Console.WriteLine("  2 │ Gráfico de países por região");
        Console.WriteLine("  3 │ Tabela dos países de uma região");
        Console.WriteLine("  4 │ Pesquisar país pelo nome");
        Console.WriteLine("  5 │ Ficha de um país (pelo código)");
        Console.WriteLine("  0 │ Sair");
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("└──────────────────────────────────────┘");
        Console.ResetColor();
        Console.Write("Escolha uma opção: ");
    }

    public static void Aviso(string texto)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("⚠ " + texto);
        Console.ResetColor();
    }

    public static void Tabela(List<KeyValuePair<string, Pais>> itens)
    {
        Linha('┌', '┬', '┐');
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("│ " + "Cód.".PadRight(4) + " │ " + "País".PadRight(LarguraNome) + " │ " + "Região".PadRight(LarguraRegiao) + " │");
        Console.ResetColor();
        Linha('├', '┼', '┤');

        foreach (var item in itens)
        {
            Console.WriteLine("│ " + item.Key.PadRight(4)
                + " │ " + Cortar(item.Value.Nome, LarguraNome).PadRight(LarguraNome)
                + " │ " + Cortar(item.Value.Regiao, LarguraRegiao).PadRight(LarguraRegiao) + " │");
        }

        Linha('└', '┴', '┘');
    }

    public static void TabelaPaginada(List<KeyValuePair<string, Pais>> itens, int tamanhoPagina = 15)
    {
        for (int i = 0; i < itens.Count; i += tamanhoPagina)
        {
            var pagina = itens.Skip(i).Take(tamanhoPagina).ToList();
            Tabela(pagina);

            int fim = Math.Min(i + tamanhoPagina, itens.Count);
            Console.WriteLine($"A mostrar {i + 1}-{fim} de {itens.Count}");

            if (fim < itens.Count)
            {
                Console.Write("Enter = próxima página | Q = voltar ao menu: ");
                string? tecla = Console.ReadLine();
                if (string.Equals(tecla?.Trim(), "q", StringComparison.OrdinalIgnoreCase))
                    return;
                Console.WriteLine();
            }
        }
    }

    public static void GraficoRegioes(Dictionary<string, Pais> dados)
    {
        var grupos = dados.Values
            .GroupBy(p => p.Regiao)
            .Select(g => new { Regiao = g.Key, Total = g.Count() })
            .OrderByDescending(g => g.Total)
            .ToList();

        int maximo = grupos.Max(g => g.Total);

        Console.WriteLine("Países por região:");
        Console.WriteLine();

        foreach (var g in grupos)
        {
            int tamanhoBarra = (int)Math.Round(g.Total * 30.0 / maximo);

            Console.Write(g.Regiao.PadRight(18));
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write(new string('█', tamanhoBarra));
            Console.ResetColor();
            Console.WriteLine(" " + g.Total);
        }
    }

    public static void Ficha(string codigo, Pais pais)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("┌────────── FICHA DO PAÍS ──────────");
        Console.WriteLine("│ Código : " + codigo);
        Console.WriteLine("│ País   : " + pais.Nome);
        Console.WriteLine("│ Região : " + pais.Regiao);
        Console.WriteLine("└───────────────────────────────────");
        Console.ResetColor();
    }

    private static void Linha(char esquerda, char meio, char direita)
    {
        Console.WriteLine(esquerda + new string('─', 6) + meio
            + new string('─', LarguraNome + 2) + meio
            + new string('─', LarguraRegiao + 2) + direita);
    }

    private static string Cortar(string texto, int maximo)
    {
        if (texto.Length <= maximo) return texto;
        return texto.Substring(0, maximo - 1) + "…";
    }
}