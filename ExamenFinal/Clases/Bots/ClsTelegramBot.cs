using ExamenFinal.Clases.Conexiones;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Mail;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace ExamenFinal.Clases.Bots
{
    class ClsTelegramBot
    {
        private readonly String Token;
        private static TelegramBotClient Bot;
        private static String correo;
        private static String resultado;
        private static String[] pedido;
        private static String[] datosNuevos;
        private static int contador = 0;
        private static int contador2 = 0;
        private static bool botIniciado;

        public ClsTelegramBot()
        {
            Token = "1830586883:AAFd2lNxvA0pYkdGYsqUnlkWITpncff1PoY";
        }

        public async Task iniciarBot()
        {
            Bot = new TelegramBotClient(Token);

            var me = await Bot.GetMeAsync();
            Console.Title = me.Username;
            
            Bot.OnMessage += BotCuandoRecibeMensajes;
            Bot.OnMessageEdited += BotCuandoRecibeMensajes;
            Bot.OnCallbackQuery += BotOnCallbackQueryRecibido;
            Bot.OnReceiveError += BotOnReceiveError;

            Bot.StartReceiving(Array.Empty<UpdateType>());
            Console.WriteLine($"Empezando a escuchar a @{me.Username}");

            Console.ReadLine();
            Bot.StopReceiving();
        }

        private static async void BotCuandoRecibeMensajes(object sender, MessageEventArgs messageEventArgumentos)
        {
            var ObjetoMensajeTelegram = messageEventArgumentos;
            var mensaje = ObjetoMensajeTelegram.Message;
            
            string mensajeEntrante = mensaje.Text;

            if (mensaje == null || mensaje.Type != MessageType.Text) return;
            
            if (mensajeEntrante == "/Menu")
            {
                botIniciado = true;

                var inlineKeyboard = new InlineKeyboardMarkup(new[]
                {
                    new []
                    {
                        InlineKeyboardButton.WithCallbackData("Realizar pedido", "realizar"),
                        InlineKeyboardButton.WithCallbackData("Consultar pedido", "consultar"),
                    },
                    new []
                    {
                        InlineKeyboardButton.WithCallbackData("Modificar datos", "modificar"),
                        InlineKeyboardButton.WithCallbackData("Borrar pedido", "borrar"),
                    }
                });
                await Bot.SendTextMessageAsync(
                    chatId: mensaje.Chat.Id,
                    text: "Hola, por favor elija una opción.",
                    replyMarkup: inlineKeyboard
                );
            }

            if (botIniciado == false)
            {
                await Bot.SendTextMessageAsync(
                    chatId: messageEventArgumentos.Message.Chat.Id,
                    text: "Utiliza /Menu para ver nuestras opciones.");
            }
        }

        private static async void BotOnCallbackQueryRecibido(object sender, CallbackQueryEventArgs callbackQueryEventArgs)
        {
            var callbackQuery = callbackQueryEventArgs.CallbackQuery;

            switch(callbackQuery.Data.ToString())
            {
                case "realizar":
                    await realizarPedido(callbackQuery);
                    break;

                case "consultar":
                    await consultarPedido(callbackQuery);
                    break;

                case "modificar":
                    await modificarPedido(callbackQuery);
                    break;

                case "borrar":
                    await borrarPedido(callbackQuery);
                    break;

                case "correo":
                    await enviarCorreo(callbackQuery);
                    break;
            }
        }

        static async Task realizarPedido(CallbackQuery callbackQuery)
        {
            contador = 0;
            pedido = new string[4];
            pedido[3] = "";

            await Bot.SendTextMessageAsync(
                chatId: callbackQuery.Message.Chat.Id,
                text: "A continuación tiene nuestra lista de libros disponibles: \n\n" + 
                        "1.Cazadores de sombras Ciudad de Hueso.\n"+
                        "2.Don Quijote de la mancha.\n" +
                        "3.María.\n" +
                        "4.Cazadores de sombras Ciudad de Cristal.\n" +
                        "5.Bajo la misma estrella.\n" +
                        "6.Poesías.\n" +
                        "7.Buscando a Alaska.\n" +
                        "8.Cazadores de sombras Ciudad de las almas perdidas.\n" +
                        "9.El señor Presidente.\n" +
                        "10.Mil veces hasta siempre.\n\n" +
                        "Por favor ingrese el número del libro que desea ordenar:"
            );

            Bot.OnMessage += BotCuandoRecibePedido;
            Bot.OnMessageEdited += BotCuandoRecibePedido;
        }

        static async Task consultarPedido(CallbackQuery callbackQuery)
        {
            resultado = "";
            ClsConexionSqlServer cn = new ClsConexionSqlServer();
            string sql = $"select * from tb_pedidos where IDCliente = {callbackQuery.Message.Chat.Id}";
            DataTable dt;
            dt = cn.consultarDB(sql);

            foreach (DataRow dr in dt.Rows)
            {
                string aux = dr["Nombre"].ToString() + "; " + dr["Libro"].ToString() + "; " + dr["Dirección"].ToString() + "; " + dr["Correo"].ToString();
                resultado += aux + "\n";
            }

            if (resultado != "")
            {
                correo = Convert.ToString(dt.Rows[0].Field<String>("Correo"));

                await Bot.SendTextMessageAsync(
                chatId: callbackQuery.Message.Chat.Id,
                text: "Tu pedido es el siguiente:\n" +
                    resultado);

                var inlineKeyboard = new InlineKeyboardMarkup(new[]
                {
                    new []
                    {
                        InlineKeyboardButton.WithCallbackData("Aceptar", "correo"),
                    }
                });
                await Bot.SendTextMessageAsync(
                    chatId: callbackQuery.Message.Chat.Id,
                    text: "Pulsa 'Aceptar' si deseas recibirlo por e-mail",
                    replyMarkup: inlineKeyboard
                );
            }
            else
            {
                await Bot.SendTextMessageAsync(
            chatId: callbackQuery.Message.Chat.Id,
            text: "No tienes ningún pedido.");
            }
        }

        static async Task modificarPedido(CallbackQuery callbackQuery)
        {
            contador2 = 0;

            await Bot.SendTextMessageAsync(
                chatId: callbackQuery.Message.Chat.Id,
                text: "Ingresa tu nueva dirección y correo electrónico separados por un espacio en blanco.");

            Bot.OnMessage += BotCuandoActualizaDatos;
            Bot.OnMessageEdited += BotCuandoActualizaDatos;
        }

        static async Task borrarPedido(CallbackQuery callbackQuery)
        {
            await Bot.SendTextMessageAsync(
                chatId: callbackQuery.Message.Chat.Id,
                text: "¿Estás seguro de que deseas borrar todos tus pedidos?\n" +
                "/Si\n" +
                "/No");

            Bot.OnMessage += BotCuandoBorraPedidos;
            Bot.OnMessageEdited += BotCuandoBorraPedidos;
        }

        static async Task enviarCorreo(CallbackQuery callbackQuery)
        {

            SmtpClient smtp = new SmtpClient("smtp.gmail.com", 587);
            smtp.Credentials = new NetworkCredential("danielacastillo.202045555@gmail.com", "Daniela12..");
            smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
            smtp.EnableSsl = true;
            smtp.UseDefaultCredentials = false;

            MailMessage mail = new MailMessage();
            mail.From = new MailAddress("danielacastillo.202045555@gmail.com", "PedidosBot");
            mail.To.Add(new MailAddress(correo));//a quien va dirigido
            mail.Subject = "SU PEDIDO: ";//asunto
            mail.IsBodyHtml = true;
            mail.Body = resultado;//lo que queremos que ponga en el cuerpo

            smtp.Send(mail);

            await Bot.SendTextMessageAsync(
                chatId: callbackQuery.Message.Chat.Id,
                text: "El pedido ha sido enviado a tu dirección de correo."
                );

        }
    
        private static async void BotCuandoRecibePedido(object sender, MessageEventArgs messageEventArgumentos)
        {
            if (contador < 4 && messageEventArgumentos.Message.Text != "/Menu")
            {
                string[] columna = { "nombre", "dirección", "e-mail" };
                var ObjetoMensajeTelegram = messageEventArgumentos;
                var mensaje = ObjetoMensajeTelegram.Message;

                string mensajeEntrante = mensaje.Text;

                if (mensaje == null || mensaje.Type != MessageType.Text) return;

                if (contador < 3)
                {
                    await Bot.SendTextMessageAsync(
                        chatId: mensaje.Chat.Id,
                        text: $"Por favor, ingrese su {columna[contador]}.");
                }

                pedido[contador] = mensajeEntrante;

                contador++;

                if (pedido[3] != "")
                {
                    ClsConexionSqlServer cn = new ClsConexionSqlServer();
                    string sql = $"insert into tb_pedidos values({messageEventArgumentos.Message.Chat.Id}, '{pedido[1]}', '{pedido[0]}', '{pedido[2]}', '{pedido[3]}')";

                    cn.ejecutarSql(sql);

                    await Bot.SendTextMessageAsync(
                            chatId: messageEventArgumentos.Message.Chat.Id,
                            text: "Tu nuevo pedido se ha realizado con éxito.");
                    Bot.StopReceiving();
                    ClsTelegramBot obj = new ClsTelegramBot();
                    await obj.iniciarBot();
                }
            }
        }

        private static async void BotCuandoActualizaDatos(object sender, MessageEventArgs messageEventArgumentos)
        {
            if (contador2 < 1)
            {
                var ObjetoMensajeTelegram = messageEventArgumentos;
                var mensaje = ObjetoMensajeTelegram.Message;
                ClsConexionSqlServer cn = new ClsConexionSqlServer();
                datosNuevos = new string[2];

                string mensajeEntrante = mensaje.Text;

                if (mensaje == null || mensaje.Type != MessageType.Text) return;

                datosNuevos = mensajeEntrante.Split(' ');

                string sql = $"update tb_pedidos set Dirección = '{datosNuevos[0]}', Correo = '{datosNuevos[1]}' where IDCliente = {messageEventArgumentos.Message.Chat.Id}";
                cn.ejecutarSql(sql);

                await Bot.SendTextMessageAsync(
                     chatId: messageEventArgumentos.Message.Chat.Id,
                     text: "Tus datos se han actualizado con éxito."
                    );

                contador2++;

                Bot.StopReceiving();
                ClsTelegramBot obj = new ClsTelegramBot();
                await obj.iniciarBot();
            }
        }

        private static async void BotCuandoBorraPedidos(object sender, MessageEventArgs messageEventArgumentos)
        {
            if (messageEventArgumentos.Message.Text == "/Si")
            {
                ClsConexionSqlServer cn = new ClsConexionSqlServer();
                string sql = $"delete from tb_pedidos where IDCliente = {messageEventArgumentos.Message.Chat.Id}";
                cn.ejecutarSql(sql);

                await Bot.SendTextMessageAsync(
                chatId: messageEventArgumentos.Message.Chat.Id,
                text: "Tus pedidos se han eliminado exitosamente.");
            }
        }

        private static void BotOnReceiveError(object sender, ReceiveErrorEventArgs receiveErrorEventArgs)
        {
            Console.WriteLine("UPS!!! Recibo un error!!!: {0} — {1}",
                receiveErrorEventArgs.ApiRequestException.ErrorCode,
                receiveErrorEventArgs.ApiRequestException.Message
            );
        }
    }
}
