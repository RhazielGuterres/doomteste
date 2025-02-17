using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

class Program
{
    private static DiscordSocketClient _client;
    private static Process _ffmpegProcess;

    static async Task Main(string[] args)
    {
        var config = new DiscordSocketConfig
        {
            GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent
        };

        _client = new DiscordSocketClient(config);
        _client.Log += Log;
        _client.MessageReceived += CommandHandler;

        string token = "tokendobot"; // Substitua pelo token correto
        await _client.LoginAsync(TokenType.Bot, token);
        await _client.StartAsync();

        Console.WriteLine("✅ Bot iniciado com sucesso!");
        await Task.Delay(-1);
    }

    private static async Task CommandHandler(SocketMessage message)
    {
        Console.WriteLine($"Mensagem recebida: {message.Content} de {message.Author.Username}");

        if (message.Author.IsBot) return;

        if (message.Content.StartsWith("!doom"))
        {
            await message.Channel.SendMessageAsync("🎮 **Capturando DOOM e convertendo para ASCII...**");
            StartDoomCapture();
            _ = Task.Run(() => ReadFfmpegOutput(message.Channel));
        }
    }

    private static void StartDoomCapture()
    {
        _ffmpegProcess = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = @"C:\ffmpeg\ffmpeg-master-latest-win64-gpl-shared\bin\ffmpeg.exe",
                Arguments = "-f gdigrab -framerate 10 -i title=\"The Ultimate DOOM - Chocolate Doom 3.1.0\" -vf \"format=gray,scale=80:40\" -f image2pipe -vcodec rawvideo -pix_fmt gray -",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };
        _ffmpegProcess.Start();
    }

    private static async Task ReadFfmpegOutput(ISocketMessageChannel channel)
    {
        byte[] buffer = new byte[60 * 30]; // Mantendo o tamanho ajustado
        Stream outputStream = _ffmpegProcess.StandardOutput.BaseStream;

        IUserMessage sentMessage = null; // Mensagem que será editada

        while (!_ffmpegProcess.HasExited)
        {
            int bytesRead = await outputStream.ReadAsync(buffer, 0, buffer.Length);
            if (bytesRead <= 0) continue;

            string asciiFrame = ConvertToAscii(buffer, 60, 30);

            // Se for a primeira vez, envia a mensagem inicial
            if (sentMessage == null)
            {
                sentMessage = await channel.SendMessageAsync($"```{asciiFrame}```");
            }
            else
            {
                // Atualiza a mensagem com o novo frame
                await sentMessage.ModifyAsync(msg => msg.Content = $"```{asciiFrame}```");
            }

            await Task.Delay(100); // Delay pequeno para evitar problemas de rate limit no Discord
        }
    }



    private static string ConvertToAscii(byte[] frameData, int width, int height)
    {
        // Definição de caracteres ASCII do mais escuro para o mais claro
        string asciiChars = "@%#*+=-:. ";

        StringBuilder asciiImage = new StringBuilder();

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int pixel = frameData[y * width + x];
                int index = pixel * (asciiChars.Length - 1) / 255; // Mapeia os valores para os caracteres
                asciiImage.Append(asciiChars[index]);
            }
            asciiImage.AppendLine(); // Quebra de linha para manter a estrutura da imagem
        }

        return asciiImage.ToString();
    }


    private static Task Log(LogMessage msg)
    {
        Console.WriteLine(msg.ToString());
        return Task.CompletedTask;
    }
}
