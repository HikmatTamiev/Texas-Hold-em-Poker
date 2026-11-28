using System.IO;
using System.Media;

namespace TexasHoldem;

public static class GameSounds
{
    private static readonly Random random = new Random();

    public static void Click()
    {
        Play("click.wav");
    }

    public static void Deal()
    {
        if (random.Next(2) == 0)
        {
            Play("deal.wav");
        }
        else
        {
            Play("deal2.wav");
        }
    }

    public static void Chips()
    {
        if (random.Next(2) == 0)
        {
            Play("chips.wav");
        }
        else
        {
            Play("chips2.wav");
        }
    }

    public static void Fold()
    {
        Play("fold.wav");
    }

    private static void Play(string fileName)
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Sounds", fileName);

        if (!File.Exists(path))
        {
            return;
        }

        SoundPlayer player = new SoundPlayer(path);
        player.Play();
    }
}
