using System;
using System.Collections.Generic;
using System.Linq;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace AfiaBot
{
    //TODO: изменить механику вывода имен игроков
    internal class GameRoom
    {
        const string str_players = "Игроки";
        const string str_id = "ID";
        const string str_exit = "Выйти";
        const string str_newgame = "Новая игра!";
        const string str_conf = "Конфигурация";
        const string str_list = "Список";
        const string str_back = "Назад";
        const string str_showme = "Покажите мне мою роль!";
        const string str_remaining = "Оставшиеся роли";

        private static int freeId = 1000;

        private enum Role
        {
            Ведущий,
            Шлюха,
            Мафия,
            Якудза,
            Доктор,
            Маньяк,
            Комиссар,
            Мирный
        }

        static ReplyKeyboardMarkup markupDefault;
        static ReplyKeyboardMarkup markupAdmin;
        static ReplyKeyboardMarkup markupList;
        static ReplyKeyboardMarkup markupBack;
        static ReplyKeyboardMarkup markupShowMe;
        static ReplyKeyboardMarkup markupShowRemaining;

        static ReplyKeyboardHide markupHide;

        private bool waitingForConfig = false;
        private bool isStarted = false;

        public int ID { get; }
        private long Admin => members.Count > 0 ? members[0].Id : -1;
        private List<Chat> members = new List<Chat>();
        private int[] conf = new int[] { 1,1,2,2,1,0,1,3 };

        private Dictionary<long, Role> roles;
        private List<Role> deck;
        private long leader;

        static GameRoom()
        {
            var def = new KeyboardButton[2][];
            def[0] = new KeyboardButton[] { str_players, str_conf };
            def[1] = new KeyboardButton[] { str_exit, str_id };
            markupDefault = new ReplyKeyboardMarkup(def, true);

            var adm = new KeyboardButton[3][];
            adm[0] = new KeyboardButton[] { str_newgame };
            adm[1] = new KeyboardButton[] { str_conf };
            adm[2] = new KeyboardButton[] { str_exit, str_id, str_players };
            markupAdmin = new ReplyKeyboardMarkup(adm, true);

            markupList = new ReplyKeyboardMarkup(
                new KeyboardButton[] { str_list, str_back }, true
                );

            markupBack = new ReplyKeyboardMarkup(
                new KeyboardButton[] { str_back }, true
                );

            markupShowMe = new ReplyKeyboardMarkup(
                new KeyboardButton[] { str_showme }, true
                );

            markupShowRemaining = new ReplyKeyboardMarkup(
                new KeyboardButton[] { str_remaining }, true
                );

            markupHide = new ReplyKeyboardHide() { HideKeyboard = true };
        }

        public GameRoom(Chat chat)
        {
            lock ("getta muvfking id")
            {
                ID = freeId++;
            }
            members.Add(chat);
            Program.Bot.SendTextMessageAsync(chat.Id, "Вы создали комнату. Вы являетесь её администратором. " +
                "ID для подключения: " + ID + ". Вы всегда можете узнать ID с помощью кнопки.",
                false, false, 0, markupAdmin);
        }

        public void HandleMessage(Message msg)
        {
            var chatID = msg.Chat.Id;

            if (waitingForConfig && chatID == Admin)
            {
                if (msg.Text == str_back)
                {
                    Program.Bot.SendTextMessageAsync(chatID, "Возвращаемся...", false, false, 0, markupAdmin);
                    waitingForConfig = false;
                }
                else
                {
                    try
                    {
                        var strmas = msg.Text.Split(';');
                        if (strmas.Length != 7)
                        {
                            throw new Exception();
                        }

                        var mas = new int[8];
                        mas[0] = 1;

                        for (var i = 1; i < 8; i++)
                        {
                            mas[i] = Convert.ToInt32(strmas[i-1]);
                        }
                        this.conf = mas;

                        var list = "Конфигурация принята.\nВ вашей игре будет следующее количество ролей:\n";
                        for (var i = 0; i < 8; i++)
                        {
                            list += ((Role)i).ToString() + ": " + conf[i] + "\n";
                        }

                        Program.Bot.SendTextMessageAsync(chatID, list,
                            false, false, 0, markupAdmin);
                        waitingForConfig = false;
                    }
                    catch
                    {
                        Program.Bot.SendTextMessageAsync(chatID, "Неверная конфигурация. Попробуйте ещё раз.");
                    }
          
                        
                }
                return;
            }

            switch (msg.Text)
            {
                case str_newgame: if (chatID == Admin) NewGame(); break;
                case str_showme:
                    if (isStarted)
                    {
                        Program.Bot.SendTextMessageAsync(chatID, "*"+roles[chatID].ToString()+"*", false, true, 0,
                            chatID == Admin ? markupAdmin : markupDefault, Telegram.Bot.Types.Enums.ParseMode.Markdown);
                    }
                    break;
                case str_remaining:
                        if (isStarted && (chatID == leader))
                        {
                            var remaining = deck.Aggregate("", (current, role) => current + ("*" + role.ToString() + "*\n"));
                            Program.Bot.SendTextMessageAsync(chatID, remaining, false, true, 0,
                                chatID == Admin ? markupAdmin : markupDefault, Telegram.Bot.Types.Enums.ParseMode.Markdown);
                        }
                        break;
                case str_conf:
                    var list = "*Текущая конфигурация:*\n";
                    for (var k = 0; k < 8; k++)
                    {
                        list += ((Role)k).ToString() + ": " + conf[k] + "\n";
                    }
                    Program.Bot.SendTextMessageAsync(chatID, list, false, false, 0,
                        chatID == Admin ? markupBack : markupDefault, Telegram.Bot.Types.Enums.ParseMode.Markdown);

                    if (chatID == Admin)
                    {
                        Program.Bot.SendTextMessageAsync(chatID, "Введите игровую конфигурацию в виде чисел, разделенных знаком *\";\"*, " +
                            "для ролей в следующем порядке: *Шлюха, Мафия, Якудза, Доктор, Маньяк, Коммисар, Мирный.*\n" +
                            "Например: *1;2;2;1;0;1;3*.\nНе забывайте, что в игру будет добавлен ведущий.", false, false, 0, markupBack,
                            Telegram.Bot.Types.Enums.ParseMode.Markdown);
                        waitingForConfig = true;
                    }
                    break;
                case str_players:
                    Program.Bot.SendTextMessageAsync(chatID, "В комнате " + members.Count + " игроков (считая вас).",
                        false, false, 0, markupList);
                    break;
                case str_list:
                    var i = 1;
                    var userList = members.Aggregate("", (current, member) => current + (i++ + ". " + member.FirstName + "\n"));
                    Program.Bot.SendTextMessageAsync(chatID, userList, false, false, 0,
                        chatID == Admin ? markupAdmin : markupDefault);
                    break;
                case str_back:
                    Program.Bot.SendTextMessageAsync(chatID, "Возвращаемся...", false, false, 0,
                        chatID == Admin ? markupAdmin : markupDefault);
                    break;
                case str_id:
                    Program.Bot.SendTextMessageAsync(chatID, "ID комнаты: " + ID + ".");
                    break;
                case str_exit: LeaveRoom(chatID); break;
                default:
                    break;
            }
        }

        private void NewGame()
        {
            this.roles = new Dictionary<long, Role>();
            this.deck = new List<Role>();

            var rnd = new Random(DateTime.Now.Millisecond);

            for (var i = 1; i < conf.Length; i++)
            {
                for (var j = 0; j < conf[i]; j++)
                {
                    deck.Add((Role)i);
                }
            }

            var localList = new List<Chat>(members);
            this.leader = localList[rnd.Next(localList.Count)].Id;

            roles.Add(leader, (Role)0);
            localList.RemoveAll(x => x.Id == leader);

            foreach (var chatMember in localList)
            {
                var index = rnd.Next(deck.Count);
                roles.Add(chatMember.Id, deck[index]);
                deck.RemoveAt(index);
            }

            isStarted = true;

            foreach (var member in members)
            {
                Program.Bot.SendTextMessageAsync(member.Id, "Новая игра началась!", false, false, 0, 
                    member.Id == leader ? markupShowRemaining : markupShowMe);
            }

            Program.Bot.SendTextMessageAsync(leader, "Вы ведущий!", false, false, 0);
        }

        public void EnterRoom(Chat chat)
        {
            lock (this)
            {
                foreach (var member in members)
                {
                    Program.Bot.SendTextMessageAsync(member.Id, "В комнату зашёл игрок " + chat.FirstName + ".",
                        false, true);
                }
                members.Add(chat);
            }
            if (chat.Id == Admin)
            {
                Program.Bot.SendTextMessageAsync(chat.Id, "Поздравляем! Вы зашли в пустую комнату, а посему становитесь её администратом!",
                    false, false, 0, markupAdmin);
            }
            else
            {
                Program.Bot.SendTextMessageAsync(chat.Id, "Вы в комнате " + ID + ". В этой комнате " +
                    members.Count + " человек(а) (считая вас).\nПожалуйста, дождитесь, пока администратор начнет новую игру.", 
                    false, false, 0, markupDefault);
            }
        }

        private void LeaveRoom(long chatId)
        {
            var admin = chatId == Admin;
            lock (this)
            {
                members.RemoveAll(x => x.Id == chatId);
                foreach (var member in members)
                {
                    Program.Bot.SendTextMessageAsync(member.Id, "Игрок " + Program.Bot.GetChatAsync(chatId).Result.FirstName + " вышел из комнаты.",
                        false, true);
                }
            }
            Menu.LeaveRoom(chatId);
            if (admin && Admin != -1)
                Program.Bot.SendTextMessageAsync(Admin, "Волей Ктулху вы назначаетесь новым администратором комнаты! " +
                    "Примите наши искренние поздравления в связи с вашим новым назначением.", false, false, 0, markupAdmin);
        }
    }
}
