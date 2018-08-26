using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using static System.Console;

namespace WadUnpack
{
    public class Program
    {
        public static void Main(string[] args)
        {
            SetWindowSize(70, 30);

            string wadfile = args[0];
            string basedir = "unpacked_" + Path.GetFileNameWithoutExtension(wadfile);
		    reader r = new reader(wadfile);
            Directory.CreateDirectory(basedir);

            WriteLine("File Size:         " + r.format_size());
            WriteLine("Directory Entries: " + r.format_entry_count());
            WriteLine("Directory Offset:  " + r.offset);

		    List<entry> dir = new List<entry>();

            for (int i = 0; i < r.entries; i++)
                dir.Add(new entry(r));

            int i = 7;

		    WriteLine("-----------------------------");
		    WriteLine($@"Unpacking into {basedir}\...");
            WriteLine("-----------------------------");
            foreach (entry e in dir)
            {
                if(i > 28)
                {
                    SetCursorPosition(0, 7);
                    i = 7;
                }
                int j = e.type;
                if (j != 67)
                    Console.WriteLine($"Cannot unpack an entry of type {j}.");
                else
                    unpack_texture(r, e, Path.Combine(basedir, e.name + ".png"));
                i++;
		    }
		    WriteLine("...Done.");
            for (; i < 28; i++)
                WriteLine();
            Read();
	    }

	    static void unpack_texture(reader r, entry d, string dest)
        {
            r.index = d.offset;
            r.read_string(16);
		    int w = r.read32();
		    int h = r.read32();
            int pixels = w * h;
            int offset1 = r.read32();
            r.index += 8;
            int offset4 = r.read32();

		    byte[] texture = new byte[pixels];
            r.index = d.offset + offset1;
		    for(int i = 0; i < pixels; i++)
                texture[i] = r.read8();

            r.index = d.offset + offset4 + pixels / 64 + 2;
		    Color[] clut = new Color[256];
		    for(int i = 0; i < 256; i++)
                clut[i] = r.readc();

		    Bitmap img = new Bitmap(w, h);

		    for(int x = 0; x < w; x++)
			    for(int y = 0; y < h; y++)
				    img.SetPixel(x, y, clut[texture[x + y * w]]);

		    WriteLine(dest + "         ");
            img.Save(dest, ImageFormat.Png);
	    }
    }

    class entry
    {
        public int offset;
        public int size;
        public int type;
        public string name;

        public entry(reader r)
        {
            offset = r.read32();
            size = r.read32();
            r.index += 4;
            type = r.read8();
            
            if (r.read8() != 0)
            {
                WriteLine("Compressed textures are not supported.");
                Read();
                Environment.Exit(1);
            }

            r.index++;
            r.index++;
            name = r.read_string(16).Replace("\0", "");
        }

        public override string ToString() => $"{offset}\t{name} ({size} bytes)";
    }

    class reader
    {
        public int index = 0;
        byte[] data;
        public int entries;
        public int offset;

        public reader(string file)
        {
            data = File.ReadAllBytes(file);

            string m = read_string(4);

            if (m == "WAD3" || m == "WAD2")
                WriteLine(m + " format");
            else
            {
                WriteLine("Not a WAD file.");
                Read();
                Environment.Exit(1);
            }

            entries = read32();
            offset = read32();
        }

        public string format_size()
        {
            long l = data.Length;
            return (l < 1000 ? l + "B" : l < 1000000 ? (l / 1000F).ToString("F") + "kB" : (l / 1000000F).ToString("F") + "MB");
        }

        public string format_entry_count()
        {
            return (entries < 1000 ? entries + "E" : (float)entries / 1000 + "kE");
        }

        public string read_string(int len)
        {
            string s = "";
            for (int i = 0; i < len; i++)
                s += (char)data[index++];
            return s;
        }

        public byte read8()
        {
            return data[index++];
        }

        public int read32()
        {
            return data[index++] | (data[index++] << 8) | (data[index++] << 16) | (data[index++] << 24);
        }

        public Color readc()
        {
            return Color.FromArgb(data[index++], data[index++], data[index++]);
        }
    }
}
