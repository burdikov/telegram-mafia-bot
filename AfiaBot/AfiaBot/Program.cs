using System;
using Telegram.Bot;

namespace AfiaBot
{
    class Program
    {
        public static TelegramBotClient Bot { get; private set; }
        private static string token = "289179199:AAFbRCxuvIoX_Cg0TpowgDLc5dlnLYuxaPA";

        static void Main(string[] args)
        {
            Bot = new TelegramBotClient(token);
            Bot.OnMessage += Bot_OnMessage;
            Bot.StartReceiving();
            
            Console.WriteLine("Бот запущен и работает!");

            while (true) { }
        }

        private static void Bot_OnMessage(object sender, Telegram.Bot.Args.MessageEventArgs e)
        {
            Menu.HandleMessage(e.Message);
        }
    }
}
