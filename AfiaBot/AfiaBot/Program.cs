using System;
using Telegram.Bot;

namespace AfiaBot
{
    internal static class Program
    {
        public static TelegramBotClient Bot { get; private set; }
        private const string Token = "289179199:AAFbRCxuvIoX_Cg0TpowgDLc5dlnLYuxaPA"; //need to delete

        private static void Main(string[] args)
        {
            Bot = new TelegramBotClient(Token);
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
