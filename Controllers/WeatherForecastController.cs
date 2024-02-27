using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace WebChatBox.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        static TcpListener server;
        static List<TcpClient> clients = new List<TcpClient>();
        
        [HttpGet]
        [Route("StartServerEngine")]
       public void StartServer()
        {
            server = new TcpListener(IPAddress.Any, 12345);
            server.Start();

            Console.WriteLine("Server is running. Waiting for connections...");

            while (true)
            {
                TcpClient client = server.AcceptTcpClient();
                clients.Add(client);

                Thread clientThread = new Thread(new ParameterizedThreadStart(HandleClientComm));
                clientThread.Start(client);

                // Start a new thread to handle messages from the server to this client
                Thread serverToClientThread = new Thread(new ParameterizedThreadStart(ServerToClientComm));
                serverToClientThread.Start(client);
            }
        }

        static void HandleClientComm(object clientObj)
        {
            TcpClient tcpClient = (TcpClient)clientObj;
            NetworkStream clientStream = tcpClient.GetStream();

            byte[] message = new byte[4096];
            int bytesRead;

            while (true)
            {
                bytesRead = 0;

                try
                {
                    bytesRead = clientStream.Read(message, 0, 4096);
                }
                catch
                {
                    break;
                }

                if (bytesRead == 0)
                    break;

                string receivedMessage = Encoding.UTF8.GetString(message, 0, bytesRead);
                Console.WriteLine($"Received message from client: {receivedMessage}");

                BroadcastMessage(receivedMessage, tcpClient);
            }

            clients.Remove(tcpClient);
            tcpClient.Close();
        }

        static void ServerToClientComm(object clientObj)
        {
            TcpClient tcpClient = (TcpClient)clientObj;
            NetworkStream clientStream = tcpClient.GetStream();

            while (true)
            {
                Console.Write("Enter message for client: ");
                string message = Console.ReadLine();
                byte[] data = Encoding.UTF8.GetBytes(message);

                clientStream.Write(data, 0, data.Length);
                clientStream.Flush();
            }
        }

        static void BroadcastMessage(string message, TcpClient senderClient)
        {
            foreach (TcpClient client in clients)
            {
                if (client != senderClient)
                {
                    NetworkStream clientStream = client.GetStream();
                    byte[] data = Encoding.UTF8.GetBytes(message);

                    clientStream.Write(data, 0, data.Length);
                    clientStream.Flush();
                }
            }

            Console.WriteLine($"Broadcasted message to all clients: {message}");
        }

        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly ILogger<WeatherForecastController> _logger;

        public WeatherForecastController(ILogger<WeatherForecastController> logger)
        {
            _logger = logger;
        }

        [HttpGet(Name = "GetWeatherForecast")]
        public IEnumerable<WeatherForecast> Get()
        {
            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateTime.Now.AddDays(index),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            })
            .ToArray();
        }
    }
}
