using System.IO;
using System.Media;
using System.Windows.Media;

namespace TexasHoldem;

public static class GameSounds
{
    private static readonly Random random = new Random();
    private static MediaPlayer? ambiancePlayer;

    public static void StartAmbiance()
    {
        StopAmbiance();

        string path = Path.Combine(AppContext.BaseDirectory, "Sounds", "ambiance.mp3");
        if (!File.Exists(path))
        {
            return;
        }

        ambiancePlayer = new MediaPlayer();
        ambiancePlayer.Volume = 0.28;
        ambiancePlayer.MediaEnded += (_, _) =>
        {
            if (ambiancePlayer == null)
            {
                return;
            }

            ambiancePlayer.Position = TimeSpan.Zero;
            ambiancePlayer.Play();
        };
        ambiancePlayer.Open(new Uri(path));
        ambiancePlayer.Play();
    }

    public static void StopAmbiance()
    {
        if (ambiancePlayer == null)
        {
            return;
        }

        ambiancePlayer.Stop();
        ambiancePlayer.Close();
        ambiancePlayer = null;
    }

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
