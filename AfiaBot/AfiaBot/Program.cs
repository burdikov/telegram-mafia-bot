using System;
using Telegram.Bot;
using Telegram.Bot.Args;

namespace AfiaBot
{
    internal static class Program
    {
        private const string Token = "289179199:AAFbRCxuvIoX_Cg0TpowgDLc5dlnLYuxaPA"; //need to delete
        public static TelegramBotClient Bot { get; private set; }

        private static void Main(string[] args)
        {
            Bot = new TelegramBotClient(Token);
            Bot.OnMessage += Bot_OnMessage;
            Bot.StartReceiving();

            Console.WriteLine("Бот запущен и работает!");

            while (true)
            {
            }
        }

        private static void Bot_OnMessage(object sender, MessageEventArgs e)
        {
            Menu.HandleMessage(e.Message);
        }
    }
}