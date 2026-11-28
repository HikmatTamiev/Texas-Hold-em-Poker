using System.IO;
using System.Media;

namespace TexasHoldem;

public static class GameSounds
{
    public static void Click()
    {
        Play("click.wav");
    }

    public static void Deal()
    {
        Play("deal.wav");
    }

    public static void Chips()
    {
        Play("chips.wav");
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
