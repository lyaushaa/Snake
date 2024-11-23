using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Common;
using Newtonsoft.Json;


namespace Snake
{
     class Program
     {
            public static List<Leaders> Leaders = new List<Leaders>();
            public static List<ViewModelUserSettings> remoteIPAddress = new List<ViewModelUserSettings>();
            public static List<ViewModelGames> viewModelGames = new List<ViewModelGames>();
            public static int localPort = 5001;
            public static int MaxSpeed = 15;
     }
    private static void Send()
    {
        // Перебираем модели пользователей 
        foreach (ViewModelUserSettings User in remoteIPAddress)
        {
            UdpClient sender = new UdpClient();
            IPEndPoint endPoint = new IPEndPoint(
            IPAddress.Parse(User.IPAddress),
            int.Parse(User.Port));



            try
            {
                byte[] bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(viewModelGames.Find(x => x.IdSnake == User.IdSnake)));
                // Отправляем данные 
                sender.Send(bytes, bytes.Length, endPoint);
                // Выводим ответ в консоль 
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Отправил данные пользователю:{User.IPAddress}:{User.Port}");
            }

            catch (Exception ex)
            {
                // Если возникли какие-либо проблемы, выводим результат об ошибке в консоль 
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Возникло исключение: " + ex.ToString() + "/n" + ex.Message);
            }

            finally
            {
                // Если всё выполнилось превосходно, закрываем UdpClient
                sender.Close();
            }
        }
    }
}
