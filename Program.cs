using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

class ConsultaProduto
{
    public string CodigoBarras { get; set; }
    public string Descricao { get; set; }
    public string Preco { get; set; }
    public DateTime DataHora { get; set; }

    public ConsultaProduto(string codigoBarras, string descricao, string preco, DateTime dataHora)
    {
        CodigoBarras = codigoBarras;
        Descricao = descricao ?? "Não encontrado";
        Preco = preco ?? "N/A";
        DataHora = dataHora;
    }
}

class BuscaPrecoServer
{
    private static readonly string DATA_FILE = "produtos.txt";
    private static readonly int PORT = 6500;
    private static Dictionary<string, string> produtos = new Dictionary<string, string>();

    public BuscaPrecoServer()
    {
        CarregarProdutos();
    }

    public void Start()
    {
        var server = new TcpListener(IPAddress.Any, PORT);
        server.Start();
        Console.WriteLine($"Servidor iniciado na porta {PORT}...");

        try
        {
            while (true)
            {
                TcpClient client = server.AcceptTcpClient();
                Console.WriteLine($"Terminal conectado: {client.Client.RemoteEndPoint}");
                HandleClient(client);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"Erro ao iniciar o servidor: {e.Message}");
        }
        finally
        {
            server.Stop();
        }
    }

    private void HandleClient(TcpClient client)
    {
        try
        {
            NetworkStream stream = client.GetStream();
            byte[] okCommand = Encoding.ASCII.GetBytes("#ok");
            stream.Write(okCommand, 0, okCommand.Length);
            stream.Flush();

            byte[] buffer = new byte[255];
            while (client.Connected)
            {
                int bytesRead = stream.Read(buffer, 0, buffer.Length);
                string comando = Encoding.ASCII.GetString(buffer, 0, bytesRead).Trim();

                if (comando.StartsWith("#") && comando.Length > 1)
                {
                    string codigoBarras = comando.Substring(1);
                    Console.WriteLine($"Consulta recebida: {codigoBarras}");

                    string resposta;
                    string descricao = null;
                    string preco = null;
                    DateTime dataHora = DateTime.Now;

                    lock (produtos)
                    {
                        if (produtos.ContainsKey(codigoBarras))
                        {
                            string[] partes = produtos[codigoBarras].Substring(1).Split('|');
                            descricao = partes[0];
                            preco = partes[1].Trim();
                            resposta = $"#{descricao}|R$ {preco}";
                        }
                        else
                        {
                            resposta = "#nfound";
                        }
                    }

                    byte[] respostaBytes = Encoding.ASCII.GetBytes(resposta);
                    stream.Write(respostaBytes, 0, respostaBytes.Length);
                    stream.Flush();
                    Console.WriteLine($"Resposta enviada: {resposta}");
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"Erro na conexão com o terminal: {e.Message}");
        }
        finally
        {
            client.Close();
        }
    }

    private void CarregarProdutos()
    {
        try
        {
            Console.WriteLine($"Carregando produtos de: {DATA_FILE}");
            if (File.Exists(DATA_FILE))
            {
                string[] linhas = File.ReadAllLines(DATA_FILE);
                produtos.Clear();

                foreach (string linha in linhas)
                {
                    string[] partes = linha.Split('|');
                    if (partes.Length == 3)
                    {
                        string codigo = partes[0].Trim();
                        string nome = partes[1].Trim();
                        string preco = partes[2].Trim();
                        produtos[codigo] = $"#{nome}|{preco}";
                    }
                }
                Console.WriteLine($"Produtos carregados: {produtos.Count}");
            }
            else
            {
                Console.WriteLine($"Arquivo {DATA_FILE} não encontrado.");
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"Erro ao carregar produtos: {e.Message}");
        }
    }
}

class Program
{
    static void Main(string[] args)
    {
        var server = new BuscaPrecoServer();
        server.Start();
    }
}