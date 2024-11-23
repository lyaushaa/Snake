using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
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
        public static void Receiver()
        {
            // Создаем UdpClient для чтения входящих данных 
            UdpClient receivingUdpClient = new UdpClient(localPort);
            // Конечная сетевая точка
            IPEndPoint RemoteIpEndPoint = null;

            try
            {
                // Выводим сообщение
                Console.WriteLine("Команды сервера:");
                // Запускаем бесконечный цик для прослушки приходящих сообщеий 

                while (true)
                {
                    // Ожидание дейтаграммы
                    byte[] receiveBytes = receivingUdpClient.Receive(
                    ref RemoteIpEndPoint);
                    // Преобразуем и отображаем данные
                    string returnData = Encoding.UTF8.GetString(receiveBytes);
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("Получил команду:" + returnData.ToString());

                    // Начало игры 
                    if (returnData.ToString().Contains("/start"))
                    {
                        // Делим данные на командуи данные Json
                        string[] dataMessage = returnData.ToString().Split('|');
                        // Конвертируем данные в модель
                        ViewModelUserSettings viewModelUserSettings = JsonConvert.DeserializeObject<ViewModelUserSettings>(dataMessage[1]);
                        // Выводим запись в контроль
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"Подключился пользователь:{viewModelUserSettings.IPAddress}:{viewModelUserSettings.Port}");
                        // Добовляем данные в коллекцию для того, чтобы отправлять пользователю
                        remoteIPAddress.Add(viewModelUserSettings);
                        // добовляем змею
                        viewModelUserSettings.IdSnake = AddSnake();
                        // связываем змею и игрока
                        viewModelGames[viewModelUserSettings.IdSnake].IdSnake = viewModelUserSettings.IdSnake;
                    }
                    else
                    {
                        // Если команда не является стартом, значит:
                        // управление змеёй
                        string[] dataMessage = returnData.ToString().Split('|');
                        // Конвертируем данные в модель
                        ViewModelUserSettings viewModelUserSettings = JsonConvert.DeserializeObject<ViewModelUserSettings>(dataMessage[1]);
                        // Получаем ID игрока
                        int IdPlayer = -1;
                        // В случае если мёртвый игрок присылает команду
                        // Находим ID игрока, ища его в списке по ID адресу и Порту 
                        IdPlayer = remoteIPAddress.FindIndex(x => x.IPAddress == viewModelUserSettings.IPAddress && x.Port == viewModelUserSettings.Port);
                        // Если игрок найден 
                        if (IdPlayer != -1)
                        {
                            if (dataMessage[0] == "Up" &&
                                viewModelGames[IdPlayer].SnakesPlayers.direction != Snakes.Direction.Down)
                                // Змее игрока указываем команду вверх
                                viewModelGames[IdPlayer].SnakesPlayers.direction = Snakes.Direction.Up;
                            // Если команда вниз и есл зменя не ползёт вверх 
                            else if (dataMessage[0] == "Down" &&
                            viewModelGames[IdPlayer].SnakesPlayers.direction != Snakes.Direction.Up)
                                // Змее игрока указываем команду вверх
                                viewModelGames[IdPlayer].SnakesPlayers.direction = Snakes.Direction.Down;
                            // Если команда влево и змея не ползёт вправо 
                            else if (dataMessage[0] == "Left" &&
                            viewModelGames[IdPlayer].SnakesPlayers.direction != Snakes.Direction.Right)
                                // Змее игрока указываем команду влево
                                viewModelGames[IdPlayer].SnakesPlayers.direction = Snakes.Direction.Left;
                            // Если команда вправо и змея не ползёт влево 
                            else if (dataMessage[0] == "Right" &&
                            viewModelGames[IdPlayer].SnakesPlayers.direction != Snakes.Direction.Left)
                                // Змее игрока указываем команду вправо
                                viewModelGames[IdPlayer].SnakesPlayers.direction = Snakes.Direction.Right;

                        }

                    }
                }
            }
            catch (Exception ex)
            {

                // Если в ходе работы возникли какие-то ошибки выводим в консоль
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Boзниклo исключение: " + ex.ToString() + "\n " + ex.Message);

            }
        }
        public static int AddSnake()
        {
            // Создаём змею пользователю
            ViewModelGames viewModelGamesPlayer = new ViewModelGames();
            // Указываем стартовые координаты змеи
            viewModelGamesPlayer.SnakesPlayers = new Snakes()
            {
                // Точки змеи
                Points = new List<Snakes.Point>() {
                new Snakes.Point() { X = 30, Y = 10 },
                new Snakes.Point() { X = 20, Y = 10 },
                new Snakes.Point() { X = 10, Y = 10 },
               },

                // Направление змеи
                direction = Snakes.Direction.Start

            };

            // Создаём рандомную точку на карте
            viewModelGamesPlayer.Points = new Snakes.Point(new Random().Next(10, 783), new Random().Next(10, 410));
            // Добавляем змею в общий список всех змей
            viewModelGames.Add(viewModelGamesPlayer);
            // Возвращаем ID змеи чтобы связать игрока и змею
            return viewModelGames.FindIndex(x => x == viewModelGamesPlayer);

        }
        public static void Timer()
        {
            while (true)
            {
                Thread.Sleep(100);
                List<ViewModelGames> RemoteSnakes = viewModelGames.FindAll(x => x.SnakesPlayers.Game0ver);
                if (RemoteSnakes.Count > 0)
                    foreach (ViewModelGames DeadSnake in RemoteSnakes)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"0тключил пользоватлеля: {remoteIPAddress.Find(x => x.IdSnake == DeadSnake.IdSnake).IPAddress}" +
                          $":{remoteIPAddress.Find(x => x.IdSnake == DeadSnake.IdSnake).Port}");
                        remoteIPAddress.RemoveAll(x => x.IdSnake == DeadSnake.IdSnake);
                    }
                viewModelGames.RemoveAll(x => x.SnakesPlayers.Game0ver);

                foreach (ViewModelUserSettings User in remoteIPAddress)
                {

                    // Находим змею игрока
                    Snakes Snake = viewModelGames.Find(x => x.IdSnake == User.IdSnake).SnakesPlayers;
                    // Прогоняем точки змеии через цикл от конца в начало
                    for (int i = Snake.Points.Count - 1; i >= 0; i--)
                    {
                        // Если у нас не первая точка
                        if (i != 0)
                        {
                            // Перемещаем точку на место предыдущей
                            Snake.Points[i] = Snake.Points[i - 1];
                        }
                        else
                        {
                            // Получаем скорость змеи (Поскольку радиус точки 10, начальная скорость 10 пунктов) 
                            int Speed = 10 + (int)Math.Round(Snake.Points.Count / 20f);
                            // Если скорость змеии более максимальной скорости
                            if (Speed > MaxSpeed) Speed = MaxSpeed;
                            // Если направление змеи влево
                            if (Snake.direction == Snakes.Direction.Right)
                            {
                                // Двигаем змею влево
                                Snake.Points[i] = new Snakes.Point() { X = Snake.Points[i].X + Speed, Y = Snake.Points[i].Y };
                            }
                            // Если направление вниз
                            else if (Snake.direction == Snakes.Direction.Down)
                            {
                                // Двигаем вниз
                                Snake.Points[i] = new Snakes.Point() { X = Snake.Points[i].X, Y = Snake.Points[i].Y + Speed };
                            }
                            // Если направление на право
                            else if (Snake.direction == Snakes.Direction.Up)
                            {
                                // Двигаем вправо
                                Snake.Points[i] = new Snakes.Point() { X = Snake.Points[i].X, Y = Snake.Points[i].Y - Speed };
                            }
                            // Если направление влево
                            else if (Snake.direction == Snakes.Direction.Left)
                            {
                                // Двигаем влево
                                Snake.Points[i] = new Snakes.Point() { X = Snake.Points[i].X - Speed, Y = Snake.Points[i].Y };

                            }

                        }

                    }
                    // проверяем змею на столкновение с препядствием
                    // Если первая точка змеи вышла за координаты экрана по горизонтали
                    if (Snake.Points[0].X <= 0 || Snake.Points[0].X >= 793)
                    {
                        // Говорим что игра окончена
                        Snake.Game0ver = true;
                    }
                    else if (Snake.Points[0].Y <= 0 || Snake.Points[0].Y >= 420)
                    {
                        // Говорим что игра окончена
                        Snake.Game0ver = true;
                    }
                    // проверяем что мы не столкнулись сами с собой
                    if (Snake.direction != Snakes.Direction.Start)
                    {
                        // Прогоняем все точки кроме первой
                        for (int i = 1; i < Snake.Points.Count; i++)
                        {
                            // Если первая точка находится в координатах последующей по горизонтали   
                            if (Snake.Points[0].X >= Snake.Points[i].X - 1 && Snake.Points[0].X <= Snake.Points[i].X + 1)
                            {
                                // Если первая точка находится в координатах по вертикали
                                if (Snake.Points[0].Y >= Snake.Points[i].Y - 1 && Snake.Points[0].Y <= Snake.Points[i].Y + 1)
                                {
                                    // Говорим что игра окончена
                                    Snake.Game0ver = true;
                                    // останавливаем цикл
                                    break;

                                }

                            }

                        }

                    }
                    // Проверяем что если первая точка змеи игрока находится в координаиах яблока по горизонтали
                    if (Snake.Points[0].X >= viewModelGames.Find(x => x.IdSnake == User.IdSnake).Points.X - 15 &&
                       Snake.Points[0].X <= viewModelGames.Find(x => x.IdSnake == User.IdSnake).Points.X + 15)
                    {
                        // Проверяем что если первая точка змеи игрока находится в координаиах яблока по вертикали
                        if (Snake.Points[0].Y >= viewModelGames.Find(x => x.IdSnake == User.IdSnake).Points.Y - 15 &&
                         Snake.Points[0].Y <= viewModelGames.Find(x => x.IdSnake == User.IdSnake).Points.Y + 15)
                        {
                            // создаём новое яблоко
                            viewModelGames.Find(x => x.IdSnake == User.IdSnake).Points = new Snakes.Point(
                            new Random().Next(10, 783),
                            new Random().Next(10, 410));
                            // Добавляем змее новую точку на координатах последней
                            Snake.Points.Add(new Snakes.Point()
                            {
                                X = Snake.Points[Snake.Points.Count - 1].X,
                                Y = Snake.Points[Snake.Points.Count - 1].Y

                            });
                            // загружаем таблицу
                            LoadLeaders();
                            // добавляем нас в таблицу
                            Leaders.Add(new Leaders()
                            {
                                Name = User.Name,
                                Points = Snake.Points.Count - 3

                            });

                            // сортирируем таблицу по двум значениям сначала по кол-ву точек затем по наименованию
                            Leaders = Leaders.OrderByDescending(x => x.Points).ThenBy(x => x.Name).ToList();
                            // Ищем себя в списке и записываем в модель змеи
                            viewModelGames.Find(x => x.IdSnake == User.IdSnake).Top =
                            Leaders.FindIndex(x => x.Points == Snake.Points.Count - 3 && x.Name == User.Name) + 1;

                        }

                    }
                    // Если игра для змеи закончена
                    if (Snake.Game0ver)
                    {
                        // Загружаем таблицу
                        LoadLeaders();
                        // Добавляем нас в таблицу
                        Leaders.Add(new Leaders()
                        {
                            // Указываем никнейм 
                            Name = User.Name,
                            // Указываем кол-во яблок которое собрал пользователь
                            Points = Snake.Points.Count - 3

                        });
                        // Сохраняем результаты
                        SaveLeaders();

                    }

                }
                // Рассылаем пользователям ответ
                Send();

            }

        }
        public static void SaveLeaders()
        {
            // Преобразуем данные игроков в JSON
            string json = JsonConvert.SerializeObject(Leaders);
            // Записываем в файл
            StreamWriter SW = new StreamWriter("./leadens.txt");
            // Пишем строку
            SW.WriteLine(json);
            // Закрываем файл
            SW.Close();

        }

        public static void LoadLeaders()
        {
            // Проверяем что есть файл
            if (File.Exists("./leaders.txt"))
            {
                // Открваем файл
                StreamReader SR = new StreamReader("./leaders.txt");
                // читаем первую строку
                string json = SR.ReadLine();
                // Закрываем файл
                SR.Close();
                // Если есто что читать 
                if (!string.IsNullOrEmpty(json))
                    // Преобразуем троку в объект
                    Leaders = JsonConvert.DeserializeObject<List<Leaders>>(json);
                else
                    // Возвращаем пустой результат 
                    Leaders = new List<Leaders>();
            }
            else
                // Возвращаем пустой результат
                Leaders = new List<Leaders>();
        }
        static void Main(string[] args)
        {
            try
            {
                // Создаем поток для прослушивания сообщений от клиентов
                Thread tRec = new Thread(new ThreadStart(Receiver));
                // Запускаем поток прослушивания
                tRec.Start();
                // Создаём таймер для управления игрой 
                Thread tTime = new Thread(Timer);
                // Запускаем таймер для управления игрой
                tTime.Start();
            }
            catch (Exception ex)
            {
                // Если что-то пошло не так, выводим сообщение о том что возникла ошибка
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Возникло исключение: " + ex.ToString() + "\n " + ex.Message);

            }

        }
    }
}
