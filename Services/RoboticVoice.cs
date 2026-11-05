namespace CustomSoundboard.Services;

/// <summary>
/// The robot styles that can be added to any spoken WAV file. They need no model files, so each one adds nothing to the app's size.
/// </summary>
public enum RoboticStyle
{
    Classic,
    Dalek,
    Retro,
    Metallic,
}

/// <summary>
/// Turns a spoken 16-bit PCM WAV file into a robot voice by changing its samples in place.
/// </summary>
public static class RoboticVoice
{
    public static void Apply(string wavPath, RoboticStyle style)
    {
        var bytes = File.ReadAllBytes(wavPath);
        var (dataStart, dataLength, sampleRate, channels) = ReadHeader(bytes);

        var count = dataLength / 2;
        var samples = new float[count];
        for (var i = 0; i < count; i++)
        {
            samples[i] = BitConverter.ToInt16(bytes, dataStart + i * 2) / 32768f;
        }

        switch (style)
        {
            case RoboticStyle.Classic:
                RingModulate(samples, sampleRate, channels, 60, 0.7f);
                break;
            case RoboticStyle.Dalek:
                RingModulate(samples, sampleRate, channels, 30, 1.0f);
                Crush(samples, sampleRate, channels, sampleRate, 7);
                break;
            case RoboticStyle.Retro:
                Crush(samples, sampleRate, channels, 6000, 5);
                break;
            case RoboticStyle.Metallic:
                Comb(samples, sampleRate, channels, 0.004f, 0.7f);
                break;
        }

        for (var i = 0; i < count; i++)
        {
            var value = (short)Math.Clamp((int)Math.Round(samples[i] * 32767f), short.MinValue, short.MaxValue);
            BitConverter.TryWriteBytes(bytes.AsSpan(dataStart + i * 2, 2), value);
        }

        File.WriteAllBytes(wavPath, bytes);
    }

    // Multiplies the voice by a low hum. depth is how much of the result is hum-modulated rather than the plain voice.
    private static void RingModulate(float[] samples, int sampleRate, int channels, float hertz, float depth)
    {
        for (var i = 0; i < samples.Length; i++)
        {
            var t = (double)(i / channels) / sampleRate;
            var hum = (float)Math.Sin(2 * Math.PI * hertz * t);
            samples[i] = samples[i] * (1 - depth) + samples[i] * hum * depth;
            samples[i] *= 1.4f;
        }
    }

    // Plays each sample for several samples in a row and rounds it to a few levels, for a retro machine sound.
    private static void Crush(float[] samples, int sampleRate, int channels, int targetRate, int bits)
    {
        var hold = Math.Max(1, sampleRate / Math.Max(1, targetRate));
        var levels = (float)(1 << (bits - 1));
        var held = new float[channels];
        for (var i = 0; i < samples.Length; i++)
        {
            var frame = i / channels;
            var channel = i % channels;
            if (frame % hold == 0)
            {
                held[channel] = MathF.Round(samples[i] * levels) / levels;
            }

            samples[i] = held[channel];
        }
    }

    // Adds a very short echo that rings, which makes the voice sound like it is inside a tin.
    private static void Comb(float[] samples, int sampleRate, int channels, float seconds, float feedback)
    {
        var delay = Math.Max(1, (int)(sampleRate * seconds)) * channels;
        for (var i = delay; i < samples.Length; i++)
        {
            samples[i] += samples[i - delay] * feedback;
        }

        for (var i = 0; i < samples.Length; i++)
        {
            samples[i] = Math.Clamp(samples[i] * 0.6f, -1f, 1f);
        }
    }

    private static (int DataStart, int DataLength, int SampleRate, int Channels) ReadHeader(byte[] bytes)
    {
        if (bytes.Length < 44 || bytes[0] != 'R' || bytes[1] != 'I' || bytes[2] != 'F' || bytes[3] != 'F')
        {
            throw new InvalidOperationException("The voice did not make a WAV file.");
        }

        var sampleRate = 0;
        var channels = 0;
        var bits = 0;
        var position = 12;
        while (position + 8 <= bytes.Length)
        {
            var id = System.Text.Encoding.ASCII.GetString(bytes, position, 4);
            var size = BitConverter.ToUInt32(bytes, position + 4);
            var body = position + 8;
            if (id == "fmt ")
            {
                var format = BitConverter.ToUInt16(bytes, body);
                channels = BitConverter.ToUInt16(bytes, body + 2);
                sampleRate = (int)BitConverter.ToUInt32(bytes, body + 4);
                bits = BitConverter.ToUInt16(bytes, body + 14);
                if (format != 1 && format != 0xFFFE)
                {
                    throw new InvalidOperationException("The robot styles need a 16-bit PCM WAV file.");
                }
            }
            else if (id == "data")
            {
                if (bits != 16 || channels == 0)
                {
                    throw new InvalidOperationException("The robot styles need a 16-bit PCM WAV file.");
                }

                var length = (int)Math.Min(size, (uint)(bytes.Length - body));
                return (body, length - length % 2, sampleRate, channels);
            }

            position = body + (int)size + (int)(size & 1);
        }

        throw new InvalidOperationException("The voice made a WAV file with no sound in it.");
    }
}
